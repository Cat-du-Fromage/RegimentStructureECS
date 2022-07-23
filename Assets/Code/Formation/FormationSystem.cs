using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

using static Unity.Mathematics.math;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class FormationSystem : SystemBase
    {
        private EntityQuery query;
        
        protected override void OnCreate()
        {
            query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<NumberUnits>(),
                },
                Any = new ComponentType[]
                {
                    typeof(MinLine),
                    typeof(MaxLine),
                    typeof(NumberLine),
                    typeof(UnitsPerLine),
                    typeof(UnitsLastLine),
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
        }

        protected override void OnUpdate()
        {
            ComponentTypeHandle<NumberUnits> numberUnitsType     = GetComponentTypeHandle<NumberUnits>(true);
            
            ComponentTypeHandle<MinLine> minLineType             = GetComponentTypeHandle<MinLine>(false);
            ComponentTypeHandle<MaxLine> maxLineType             = GetComponentTypeHandle<MaxLine>(false);
            ComponentTypeHandle<NumberLine> numberLineType       = GetComponentTypeHandle<NumberLine>(false);
            ComponentTypeHandle<UnitsPerLine> unitsPerLineType   = GetComponentTypeHandle<UnitsPerLine>(false);
            ComponentTypeHandle<UnitsLastLine> unitsLastLineType = GetComponentTypeHandle<UnitsLastLine>(false);

            JFormationSystem formationSystemJob = new JFormationSystem
            {
                NumberUnitsTypeHandle = numberUnitsType,
                MinLineTypeHandle = minLineType,
                MaxLineTypeHandle = maxLineType,
                NumberLineTypeHandle = numberLineType,
                UnitsPerLineTypeHandle = unitsPerLineType,
                UnitsLastLineTypeHandle = unitsLastLineType,
                LastSystemVersion = this.LastSystemVersion
            };
            Dependency = formationSystemJob.ScheduleParallel(query, Dependency);
        }

        [BurstCompile]
        private struct JFormationSystem : IJobEntityBatch
        {
            public uint LastSystemVersion;
            [ReadOnly] public ComponentTypeHandle<NumberUnits> NumberUnitsTypeHandle;
            
            public ComponentTypeHandle<MinLine> MinLineTypeHandle;
            public ComponentTypeHandle<MaxLine> MaxLineTypeHandle;
            public ComponentTypeHandle<NumberLine> NumberLineTypeHandle;
            public ComponentTypeHandle<UnitsPerLine> UnitsPerLineTypeHandle;
            public ComponentTypeHandle<UnitsLastLine> UnitsLastLineTypeHandle;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                bool changed = batchInChunk.DidOrderChange(LastSystemVersion) ||
                               batchInChunk.DidChange(NumberUnitsTypeHandle, LastSystemVersion) || 
                               batchInChunk.DidChange(UnitsPerLineTypeHandle, LastSystemVersion);
                if (!changed) return;
                
                NativeArray<NumberUnits> chunkNumberUnits = batchInChunk.GetNativeArray(NumberUnitsTypeHandle);
                
                NativeArray<MinLine> chunkMinLine             = batchInChunk.GetNativeArray(MinLineTypeHandle);
                NativeArray<MaxLine> chunkMaxLine             = batchInChunk.GetNativeArray(MaxLineTypeHandle);
                NativeArray<NumberLine> chunkNumberLine       = batchInChunk.GetNativeArray(NumberLineTypeHandle);
                NativeArray<UnitsPerLine> chunkUnitsPerLine   = batchInChunk.GetNativeArray(UnitsPerLineTypeHandle);
                NativeArray<UnitsLastLine> chunkUnitsLastLine = batchInChunk.GetNativeArray(UnitsLastLineTypeHandle);
                
                bool hasMinLine       = batchInChunk.Has(MinLineTypeHandle);
                bool hasMaxLine       = batchInChunk.Has(MaxLineTypeHandle);
                bool hasNumberLine    = batchInChunk.Has(NumberLineTypeHandle);
                bool hasUnitsPerLine  = batchInChunk.Has(UnitsPerLineTypeHandle);
                bool hasUnitsLastLine = batchInChunk.Has(UnitsLastLineTypeHandle);
                int count = batchInChunk.Count;
                
                for (int i = 0; i < count; i++)
                {
                    int numUnits = chunkNumberUnits[i].Value;
                    int unitsPerLine = max(chunkUnitsPerLine[i].Value,1);
                    
                    if (hasMinLine)
                    {
                        int minLine = chunkMinLine[i].Value;
                        chunkMinLine[i] = new MinLine() { Value = select(minLine, numUnits, minLine > numUnits) };
                    }

                    if (hasMaxLine)
                    {
                        int maxLine = chunkMaxLine[i].Value;
                        chunkMaxLine[i] = new MaxLine() { Value = select(maxLine, numUnits, maxLine > numUnits) };
                    }
                    
                    if (hasUnitsPerLine)
                    {
                        chunkUnitsPerLine[i] = new UnitsPerLine() { Value = select(unitsPerLine,numUnits, unitsPerLine > numUnits) };
                        unitsPerLine = max(chunkUnitsPerLine[i].Value,1);
                    }
                    
                    if (hasNumberLine)
                    {
                        chunkNumberLine[i] = new NumberLine(){ Value = (int)ceil(numUnits / (float)unitsPerLine) };
                    }

                    if (hasUnitsLastLine)
                    {
                        int lastLineNumUnit = numUnits - (numUnits / unitsPerLine) * unitsPerLine;
                        chunkUnitsLastLine[i] = new UnitsLastLine() { Value =  select(lastLineNumUnit,unitsPerLine,lastLineNumUnit == 0)};
                    }
                    
                    /*
                    int lastLineNumUnit2 = numUnits - (numUnits / unitsPerLine) * unitsPerLine;
                    if (lastLineNumUnit2 != unitsPerLine)
                    {
                        float offset = (unitsPerLine - lastLineNumUnit2) * 0.5f;
                        float3 offsetPosition = (newLineDirection * regimentClass.SpaceSizeBetweenUnit) * offset;
                    }
                    */
                }
            }
        }
    }
}