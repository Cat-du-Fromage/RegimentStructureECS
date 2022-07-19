using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using KWUtils;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using static KWUtils.KWmath;
using static Unity.Mathematics.math;
using static Unity.Mathematics.float3;
using static Unity.Mathematics.quaternion;

namespace KaizerWald
{
    public readonly struct DynamicFormation
    {
        public readonly int NumUnits;
        public readonly int UnitsPerLine;
        public readonly int NumLine;
        public readonly int NumUnitsLastLine;

        public DynamicFormation(int numUnits, int unitsPerLine)
        {
            NumUnits = numUnits;
            UnitsPerLine = unitsPerLine;
            
            NumLine = (int)ceil(numUnits / (float)unitsPerLine);
            
            // NumUnitsLastLine
            int numCompleteLine = (int)floor(numUnits / (float)unitsPerLine); //REWORK LATER
            int lastLineNumUnit = numUnits - (numCompleteLine * unitsPerLine);
            NumUnitsLastLine = select(lastLineNumUnit,unitsPerLine,lastLineNumUnit == 0);
        }
        
        public DynamicFormation(int numUnits, int unitsPerLine, int numLine, int numUnitsLastLine)
        {
            NumUnits = numUnits;
            UnitsPerLine = unitsPerLine;
            NumLine = numLine;
            NumUnitsLastLine = numUnitsLastLine;
        }
        
        public readonly int LastLineStartIndex => NumUnits - NumUnitsLastLine;
        public readonly int LastIndex => NumUnits - 1;
        public readonly bool IsEven => NumUnitsLastLine == UnitsPerLine;
    }
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class RearrangementSystem : SystemBase
    {
        private EntityQuery unitKilledQuery;
        
        protected override void OnCreate()
        {
            EntityQueryDesc description = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(Data_IndexInRegiment), typeof(Data_Regiment) },
                None = new ComponentType[] { typeof(Tag_Unit) }
            };
            unitKilledQuery = EntityManager.CreateEntityQuery(description);
            RequireForUpdate(unitKilledQuery);
        }

        protected override void OnUpdate()
        {
            using NativeList<Entity> unitsToMove = new(2, Allocator.TempJob);
            //using NativeParallelHashMap<Entity, float3> unitsToMove = new(2, Allocator.TempJob);

            //Query dead units
            using NativeArray<Entity> deadUnits = unitKilledQuery.ToEntityArray(Allocator.TempJob);
            using NativeParallelMultiHashMap<Entity, Entity> pairRegimentUnit = GetPairRegimentDeadUnit(deadUnits);

            using NativeArray<Entity> uniqueKeyRegiment = pairRegimentUnit.GetKeyArray(Allocator.TempJob);

            
            for (int i = 0; i < uniqueKeyRegiment.Length; i++) //LOOP REGIMENT CAREFULL!
            {
                Entity regiment = uniqueKeyRegiment[i];
                
                //Needed for GetIndexAlgorithm
                DynamicBuffer<Buffer_Units> unitsBuffer = GetBuffer<Buffer_Units>(regiment);
                using NativeArray<Entity> regimentUnits = unitsBuffer.Reinterpret<Entity>().ToNativeArray(Allocator.TempJob);
                
                //TODO: MAKE A NativeSortedList
                using NativeList<int> deadIndicesInRegiment = GetDeadIndicesForRegiment(pairRegimentUnit, regiment);
                
                while (deadIndicesInRegiment.Length != 0)
                {
                    DynamicFormation formation = GetFormationRegiment(regiment);

                    int deadIndex = deadIndicesInRegiment[0];
                    int swapIndex = GetIndexAround(regimentUnits, deadIndex, formation);
                    Debug.Log($"deadIndex: {deadIndex}; swapIndex: {swapIndex}");

                    if (swapIndex == -1)
                    {
                        deadIndicesInRegiment.RemoveAt(0);
                        break;
                    }

                    SwapIndexInRegiment(unitsBuffer[deadIndex], unitsBuffer[swapIndex]);

                    (unitsBuffer[deadIndex], unitsBuffer[swapIndex]) =
                        (unitsBuffer[swapIndex], unitsBuffer[deadIndex]);
                    unitsToMove.Add(unitsBuffer[deadIndex]);

                    deadIndicesInRegiment.RemoveAt(0);
                    deadIndicesInRegiment.Add(swapIndex);
                    deadIndicesInRegiment.SortJob().Schedule().Complete();
                }
            }
            
            EntityManager.AddComponent<Tag_MoveRearrange>(unitsToMove.AsArray());
            
            EntityManager.RemoveComponent(unitKilledQuery, new ComponentTypes(typeof(Data_Regiment), typeof(Data_IndexInRegiment)));
            EntityManager.DestroyEntity(unitKilledQuery);
        }
        /*
        public void ReplaceStaticPlacements(Entity regiment)
        {
            FormationData tempsFormation = regiment.Formation;
            if (tempsFormation.NumLine == 1 || tempsFormation.IsLastLineComplete) return;

            float unitSpace = regiment.RegimentClass.SpaceSizeBetweenUnit;
            Vector3 offset = tempsFormation.GetLastLineOffset(regiment.RegimentClass.SpaceSizeBetweenUnit);
            
            int startIndex = (tempsFormation.NumCompleteLine - 1) * tempsFormation.UnitsPerLine;
            int firstHighlightIndex = startIndex + tempsFormation.UnitsPerLine;

            Vector3 startPosition = Records[regiment.RegimentID][startIndex].HighlightTransform.position;
            startPosition += (Vector3)tempsFormation.ColumnDirection * unitSpace + offset;
            
            for (int i = 0; i < tempsFormation.NumUnitsLastLine; i++)
            {
                Vector3 linePosition = startPosition + (Vector3)tempsFormation.LineDirection * (unitSpace * i);
                Records[regiment.RegimentID][firstHighlightIndex + i].HighlightTransform.position = linePosition;
            }
        }
*/
        public void SwapIndexInRegiment(Entity deadUnit, Entity swapedUnit)
        {
            Data_IndexInRegiment indexDead = GetComponent<Data_IndexInRegiment>(deadUnit);
            Data_IndexInRegiment indexSwap = GetComponent<Data_IndexInRegiment>(swapedUnit);
            
            SetComponent(swapedUnit, indexDead);
            SetComponent(deadUnit, indexSwap);
        }

        public DynamicFormation GetFormationRegiment(Entity regiment)
        {
            int numUnits = GetComponent<Data_NumUnits>(regiment).Value;
            int unitsPerLine = GetComponent<Data_UnitsPerLine>(regiment).Value;
            return new DynamicFormation(numUnits, unitsPerLine);
        }
        private NativeList<int> GetDeadIndicesForRegiment(NativeParallelMultiHashMap<Entity, Entity> pairRegimentUnit, Entity regiment)
        {
            NativeList<int> deadIndicesInRegiment = new (pairRegimentUnit.CountValuesForKey(regiment), Allocator.TempJob);
                
            NativeParallelMultiHashMap<Entity, Entity>.Enumerator unitsInKey = pairRegimentUnit.GetValuesForKey(regiment);
            foreach (Entity unity in unitsInKey)
            {
                int indexInRegiment = GetComponent<Data_IndexInRegiment>(unity).Value;
                Debug.Log(indexInRegiment);
                deadIndicesInRegiment.Add(indexInRegiment);
            }

            return deadIndicesInRegiment;
        }
        
        private NativeParallelMultiHashMap<Entity, Entity> GetPairRegimentDeadUnit(NativeArray<Entity> deadUnits)
        {
            NativeParallelMultiHashMap<Entity, Entity> pairRegimentUnit = new(deadUnits.Length, Allocator.TempJob);
            foreach (Entity deadUnit in deadUnits)
            {
                Entity regiment = GetComponent<Data_Regiment>(deadUnit).Value;
                pairRegimentUnit.Add(regiment, deadUnit);
            }
            return pairRegimentUnit;
        }

        
/*
        public void ManualRearrange()
        {
            NativeArray<int> deadIndices = unitKilledQuery.ToComponentDataArray<Data_IndexInRegiment>(Allocator.TempJob).Reinterpret<int>();
            NativeArray<Entity> regiments = unitKilledQuery.ToComponentDataArray<Data_Regiment>(Allocator.TempJob).Reinterpret<Entity>();
            
            int deadIndex = deadIndices[0];
            Entity regiment = regiments[0];

            NativeArray<Entity> units = GetBuffer<Buffer_Units>(regiment).Reinterpret<Entity>().ToNativeArray(Allocator.TempJob);
            int swapIndex = GetIndexAround(units ,deadIndex, Formation);

            if (swapIndex == -1)
            {
                DeadUnits.Remove(deadIndex);
                return;
            }
            
            //TODO: need to replace all units in last Line

            ReplaceStaticPlacements();
            Vector3 positionToGo = HighlightCoordinator.StaticPlaceRegister.Records[RegimentID][deadIndex].HighlightTransform.position;
            
            //Index in regiment n'est pas clair, il faut voir quand le changer!
            (Units[deadIndex].IndexInRegiment, Units[swapIndex].IndexInRegiment) = (Units[swapIndex].IndexInRegiment, Units[deadIndex].IndexInRegiment);
            
            rearrangementSequence.Reorganize(Units[swapIndex], positionToGo);
            
            (Units[deadIndex], Units[swapIndex]) = (Units[swapIndex], Units[deadIndex]);
            (UnitsTransform[deadIndex], UnitsTransform[swapIndex]) = (UnitsTransform[swapIndex], UnitsTransform[deadIndex]);
            
            DeadUnits.Remove(deadIndex);
            DeadUnits.Add(swapIndex);
        }
*/
        [BurstCompile(CompileSynchronously = true)]
        public partial struct JRearrange : IJobEntityBatch
        {
            public ComponentTypeHandle<Data_Regiment> RegimentTypeHandle;
            public ComponentTypeHandle<Translation> IndexInRegimentTypeHandle;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                NativeArray<Entity> regiments = batchInChunk.GetNativeArray(RegimentTypeHandle).Reinterpret<Entity>();
                NativeArray<int> indexInRegiment = batchInChunk.GetNativeArray(IndexInRegimentTypeHandle).Reinterpret<int>();
            }
        }
        
        public int GetIndexAround(NativeArray<Entity> units, int index, in DynamicFormation formation)
        {
            (int coordX, int coordY) = index.GetXY(formation.UnitsPerLine);
            
            int totalLine = formation.NumLine;
            int lastLineIndex = totalLine - 1;

            if (coordY == formation.NumLine - 1)
            {
                return RearrangeInline(units, coordX, coordY, formation.UnitsPerLine, formation.LastIndex);
            }

            for (int line = 1; line < totalLine-coordY; line++)
            {
                int lineIndexChecked = coordY + line;
                
                int lineWidth = lineIndexChecked == lastLineIndex ? formation.NumUnitsLastLine : formation.UnitsPerLine;
                int lastLineIndexChecked = lineWidth - 1;

                bool2 leftRightClose = new (coordX == 0, coordX == lastLineIndexChecked);

                //We first check if there is something to check behind
                if (!IsNextRowValid(units, coordY, formation)) continue;

                int indexUnit = -1;
                
                for (int i = 0; i <= lineWidth; i++) // 0 because we check unit right behind
                {
                    //Check Unit Behind
                    if (i == 0)
                    {
                        indexUnit = mad(coordY + line, formation.UnitsPerLine, coordX);

                        //Check if we pick on the last line: then adjust considering if the line is complete or not
                        if (coordY + line == formation.NumLine - 1)
                        {
                            int unitBehindIndex = GetUnitBehindInUnEvenLastLine(indexUnit, formation);
                            indexUnit = select(unitBehindIndex, min(indexUnit, formation.NumUnits - 1), formation.IsEven);
                        }

                        if (IsUnitValid(units[indexUnit]))
                        {
                            return indexUnit;
                        }
                        
                        leftRightClose = IsLeftRightClose(indexUnit, formation.NumUnits, lineWidth);
                        continue;
                    }
                    
                    
                    //Check Left/Negative Index
                    if (!leftRightClose.x)
                    {
                        int x = min(coordX, lineWidth) - i;
                        indexUnit = new int2(x, coordY + line).GetIndex(formation.UnitsPerLine);

                        if (units[indexUnit] != Entity.Null) return indexUnit;
                        leftRightClose.x = min(coordX, lineWidth) - i == 0;
                    }

                    //Check Right/Positiv Index
                    if (!leftRightClose.y)
                    {
                        indexUnit = new int2(coordX + i, coordY + line).GetIndex(formation.UnitsPerLine);
                        
                        if(units[indexUnit] != Entity.Null) return indexUnit;
                        leftRightClose.y = coordX + i == lastLineIndexChecked;
                    }
                    
                    
                    //if (IsRightValid(formation.UnitsPerLine)) return indexUnit;
                    //if (IsLeftValid(formation.UnitsPerLine)) return indexUnit;
                    //No more unit to check in this line
                    if (all(leftRightClose)) break;
                    
                    bool IsRightValid(int unitsPerLine)
                    {
                        if (leftRightClose.x) return false;
                        int x = min(coordX, lineWidth) - i;
                        indexUnit = mad(coordY + line, unitsPerLine, x);
                        leftRightClose.x = min(coordX, lineWidth) - i == 0;
                        return IsUnitValid(units[indexUnit]);
                    }

                    bool IsLeftValid(int unitsPerLine)
                    {
                        if (leftRightClose.y) return false;
                        indexUnit = new int2(coordX + i, coordY + line).GetIndex(unitsPerLine);
                        leftRightClose.y = coordX + i == lastLineIndexChecked;
                        return IsUnitValid(units[indexUnit]);
                    }
                }
            }
            return -1;
        }
        
        private bool IsUnitValid(Entity unit) => unit != Entity.Null && !unitKilledQuery.Matches(unit);
        
        private bool IsNextRowValid(NativeArray<Entity> units, int yLine, in DynamicFormation formation)
        {
            int nextYLine = yLine + 1;
            int totalLine = formation.NumLine;
            
            if (nextYLine > totalLine - 1) return false;
            int numUnitOnLine = nextYLine == totalLine - 1 ? formation.NumUnitsLastLine : formation.UnitsPerLine;
            
            NativeSlice<Entity> lineToCheck = new (units,nextYLine * formation.UnitsPerLine, numUnitOnLine);
            int numInvalid = 0;
            foreach (Entity unit in lineToCheck)
            {
                numInvalid += select(0,1,!IsUnitValid(unit));
            }
            return numInvalid != lineToCheck.Length;
        }
        
        private int GetUnitBehindInUnEvenLastLine(int index, in DynamicFormation formation)
        {
            int offset = (int)ceil((formation.UnitsPerLine - formation.NumUnitsLastLine) / 2f);
            int indexUnitBehind = index - offset;

            int minIndex = formation.UnitsPerLine * (formation.NumLine - 1);
            indexUnitBehind = max(minIndex, indexUnitBehind);
            indexUnitBehind = min(formation.NumUnits - 1, indexUnitBehind);
            
            return indexUnitBehind;
        }
        
        private int RearrangeInline(NativeArray<Entity> units, int coordX, int coordY, int unitPerLine, int lastIndexFormation)
        {
            int increment = 1;
            int fullIndex = mad(coordY, unitPerLine, coordX);
            while (fullIndex < lastIndexFormation)
            {
                int xToCheck = coordX + increment;
                fullIndex = mad(coordY, unitPerLine, xToCheck);
                if(IsUnitValid(units[fullIndex])) return fullIndex;
                increment++;
            }
            return -1;
        }

        private bool2 IsLeftRightClose(int index, int numUnits, int lineWidth)
        {
            return new bool2
            {
                x = index == numUnits - lineWidth,
                y = index == numUnits - 1
            };
        }
    }
}