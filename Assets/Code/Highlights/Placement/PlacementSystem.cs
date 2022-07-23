using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;

namespace KaizerWald
{
    public readonly struct FormationLineData
    {
        public readonly bool IsLastLineComplete;
        public readonly int UnitsPerLine;
        public readonly int NumLine;
        public readonly int NumUnitsLastLine;
        public readonly float SpaceBetweenUnitX;
        
        //public readonly bool IsLastLineComplete => NumUnitsLastLine == UnitsPerLine;

        public FormationLineData(int numUnits, int unitsPerLine, float spaceBetweenUnitX)
        {
            UnitsPerLine = unitsPerLine;
            NumLine = (int)ceil(numUnits / (float)unitsPerLine);;
            
            int lastLineNumUnit = numUnits - ((numUnits / unitsPerLine) * unitsPerLine);
            NumUnitsLastLine = select(lastLineNumUnit,unitsPerLine,lastLineNumUnit == 0);
            
            SpaceBetweenUnitX = spaceBetweenUnitX;
            IsLastLineComplete = NumUnitsLastLine == UnitsPerLine;
        }

        public static float3 GetLastLineOffset(in FormationLineData formation, in float3 lineDirection)
        {
            if (formation.IsLastLineComplete) return float3.zero;
            float offset = (formation.UnitsPerLine - formation.NumUnitsLastLine) * 0.5f;
            float3 offsetPosition = lineDirection * (offset * formation.SpaceBetweenUnitX);
            return offsetPosition;
        }
    }
    
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlacementInputSystem))]
    public partial class PlacementSystem : SystemBase
    {
        private Entity placementManager;
        private EntityQuery placementQuery;
        private bool IsDrag => lengthsq(EndMouse - StartMouse) >= 128;
        private float3 StartMouse => GetSingleton<Data_StartPlacement>().Value;
        private float3 EndMouse => GetSingleton<Data_EndPlacement>().Value;
        
        protected override void OnCreate()
        {
            placementManager = GetSingletonEntity<Tag_PlacementManager>();
            placementQuery = EntityManager.CreateEntityQuery(typeof(Tag_Placement), typeof(Shared_RegimentEntity));
        }

        protected override void OnUpdate()
        {
            if(EndMouse.Equals(StartMouse)) return; //ATTENTION : Il faudra prendre en compte le cas où "Drag" -> "notDragAnymore"
            
            using NativeParallelHashSet<Entity> regimentSelected = new (2, Allocator.TempJob);
            new JGetSelectedRegiments { RegimentsSelected = regimentSelected }.Run();
            
            if (regimentSelected.IsEmpty || !GetSingleton<Filter_PlacementUpdate>().DidChange) return;

            using NativeArray<Entity> regimentSelected2 = regimentSelected.ToNativeArray(Allocator.TempJob);
            
            using NativeArray<FormationLineData> formationsData = FillNewFormation(regimentSelected2);
            float3 lineDirection    = GetComponent<Data_LineDirection>(placementManager).Value;
            float3 columnDirection  = GetComponent<Data_ColumnDirection>(placementManager).Value;
            float3 startPosition    = GetComponent<Data_StartPlacement>(placementManager).Value;
            quaternion lookRotation = GetComponent<Data_LookRotation>(placementManager).Value;
            
            for (int regimentIndex = 0; regimentIndex < regimentSelected2.Length; regimentIndex++)
            {
                placementQuery.SetSharedComponentFilter(new Shared_RegimentEntity(){Value = regimentSelected2[regimentIndex]});
                JSetPlacements job = new JSetPlacements
                {
                    RegimentIndex = regimentIndex,
                    LineDirection = lineDirection,
                    ColumnDirection = columnDirection,
                    StartPosition = startPosition,
                    LookRotation = lookRotation,
                    FormationsData = formationsData,
                };
                job.ScheduleParallel(placementQuery);
            }

            Dependency.Complete();
            JCacheNewFormation job2 = new JCacheNewFormation
            {
                RegimentSelected = regimentSelected2,
                FormationsData = formationsData
            };
            job2.Run();

            //formationsData.Dispose();

            SetSingleton(new Filter_PlacementUpdate() { DidChange = false });
        }

        [BurstCompile(CompileSynchronously = true)]
        [WithAll(typeof(Tag_PlacementManager))]
        private partial struct JCacheNewFormation : IJobEntity
        {
            public NativeArray<Entity> RegimentSelected;
            public NativeArray<FormationLineData> FormationsData;
            public void Execute(ref DynamicBuffer<Buffer_CachedUnitsPerLine>  unitsPerLine)
            {
                unitsPerLine.Clear();
                for (int i = 0; i < FormationsData.Length; i++)
                {
                    unitsPerLine.Add(new Buffer_CachedUnitsPerLine
                    {
                        regiment = RegimentSelected[i],
                        Value = FormationsData[i].UnitsPerLine
                    });
                }
            }
        }

        public int NumUnitsToAdd(NativeArray<Entity> regimentSelected, in float3 startPosition, in float3 endPosition)
        {
            float lineLength = length(endPosition - startPosition);
            float minRegimentsFormationSize = MinSizeFormation(regimentSelected);
            
            int numUnitToAdd = (int)(lineLength - minRegimentsFormationSize);
            return max(0, numUnitToAdd);
        }
        
        //ATTENTION: ON NE MET PAS ENCORE A JOUR LES INFORMATIONS! L'ACTION DOIT ÊTRE VALIDE (CLICK DROIT RELACHE)
        private NativeArray<FormationLineData> FillNewFormation(NativeArray<Entity> selectedRegiments)
        {
            //TODO: WE MUST CONSIDER CASE WHEN : selection reach its maximum then we shall not take them into account when getting numUnitToAdd
            int numSelection = selectedRegiments.Length;

            NativeArray<FormationLineData> formationLines = new(numSelection, Allocator.TempJob);

            int numUnitToAdd = NumUnitsToAdd(selectedRegiments, StartMouse, EndMouse);

            float fraction = modf(numUnitToAdd / (float)numSelection, out float integral);
            int numPerRegiment = (int)integral;

            int numUnEvenToAdd = select(0,numUnitToAdd - numPerRegiment * numSelection,fraction >= EPSILON);

            for (int i = 0; i < selectedRegiments.Length; i++)
            {
                Entity regiment = selectedRegiments[i];
                int numUnit = GetComponent<NumberUnits>(regiment).Value;
                int minLine = GetComponent<MinLine>(regiment).Value;
                int maxLine = GetComponent<MaxLine>(regiment).Value;
                
                int newNumUnitPerLine = min(maxLine, minLine + numPerRegiment);

                if (newNumUnitPerLine != maxLine && numUnEvenToAdd > 0)
                {
                    newNumUnitPerLine++;
                    numUnEvenToAdd--;
                }

                formationLines[i] = new FormationLineData(numUnit, newNumUnitPerLine,
                    GetComponent<Data_SpaceBetweenUnitX>(regiment).Value);
            }
            return formationLines;
        }
        
        public float MinSizeFormation(NativeArray<Entity> selectedRegiments)
        {
            float min = 0;
            float mediumSize = 0;
            for (int i = 0; i < selectedRegiments.Length; i++)
            {
                float spaceBetweenUnitX = GetComponent<Data_SpaceBetweenUnitX>(selectedRegiments[i]).Value;
                float minLine = GetComponent<MinLine>(selectedRegiments[i]).Value;
                float unitSizeX = GetComponent<Data_UnitSize>(selectedRegiments[i]).Value.x;

                min += spaceBetweenUnitX * minLine;
                
                //so we land at the edge of the very last Unit
                //Made in order to avoid issue when units are different in size between regiment
                mediumSize += unitSizeX * 0.5f; 
                
                if (i == 0) continue;
                //Add one space of the previous regiment
                float distanceBetweenUnit = GetComponent<Data_SpaceBetweenUnitX>(selectedRegiments[i - 1]).Value;
                //Add one space of the current regiment
                //float spaceBetweenRegiment = mad(spaceBetweenUnitX, 0.5f, distanceBetweenUnit);
                float spaceBetweenRegiment = spaceBetweenUnitX + distanceBetweenUnit * 0.5f;
                min += spaceBetweenRegiment;
            }

            mediumSize /= selectedRegiments.Length;
            return min - mediumSize;
        }
        
        [BurstCompile(CompileSynchronously = true)]
        [WithAll(typeof(Tag_Placement))]
        private partial struct JSetPlacements : IJobEntity
        {
            [ReadOnly] public int RegimentIndex;
            [ReadOnly] public float3 LineDirection;
            [ReadOnly] public float3 ColumnDirection;
            [ReadOnly] public float3 StartPosition;
            [ReadOnly] public quaternion LookRotation;
            
            [ReadOnly, NativeDisableParallelForRestriction] 
            public NativeArray<FormationLineData> FormationsData;
            public void Execute([EntityInQueryIndex] int entityInQueryIndex, ref Translation position, ref Rotation rotation)
            {
                FormationLineData formation = FormationsData[RegimentIndex];
                            
                float offsetRegiment = GetRegimentOffset(RegimentIndex);
                float3 offsetPosition = FormationLineData.GetLastLineOffset(formation, LineDirection);

                int y = entityInQueryIndex / formation.UnitsPerLine;
                int x = entityInQueryIndex - (y * formation.UnitsPerLine);

                //float3 linePosition = StartPosition + LineDirection * (formation.SpaceBetweenUnitX * x);
                float3 linePosition = mad(LineDirection, formation.SpaceBetweenUnitX * x, StartPosition);
                linePosition = mad(LineDirection, offsetRegiment, linePosition);// LineDirection * offsetRegiment;

                bool isLastRow = (y == formation.NumLine - 1) && (formation.NumUnitsLastLine != formation.UnitsPerLine);
                linePosition = select(float3.zero, offsetPosition, isLastRow) + linePosition;

                float3 columnPosition = ColumnDirection * (formation.SpaceBetweenUnitX * y);

                //Position Here!
                position.Value.xz = (linePosition + columnPosition).xz;
                rotation.Value = LookRotation;
            }
            private float GetRegimentOffset(int regimentIndex)
            {
                float offsetRegiment = 0;
                if (regimentIndex == 0) return offsetRegiment;
                for (int i = 0; i < regimentIndex; i++)
                {
                    offsetRegiment += FormationsData[i].SpaceBetweenUnitX * FormationsData[i].UnitsPerLine;
                    offsetRegiment += FormationsData[regimentIndex - 1].SpaceBetweenUnitX + FormationsData[regimentIndex].SpaceBetweenUnitX;
                }
                return offsetRegiment;
            }
        }

    }
}