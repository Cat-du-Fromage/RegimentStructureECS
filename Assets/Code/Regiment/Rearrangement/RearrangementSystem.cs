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
            NativeList<Entity> unitsToMove = new(2, Allocator.Temp);
            //NativeArray<Entity> unitsToMove = CollectionHelper.CreateNativeArray<Entity, RewindableAllocator>(2, ref World.UpdateAllocator);
            //Query dead units
            NativeArray<Entity> deadUnits = unitKilledQuery.ToEntityArray(Allocator.Temp);
            NativeParallelMultiHashMap<Entity, Entity> pairRegimentUnit = GetPairRegimentDeadUnit(deadUnits);
            
            NativeArray<Entity> uniqueKeyRegiment = pairRegimentUnit.GetKeyArray(Allocator.Temp);
            int numUniqueKey = uniqueKeyRegiment.Unique();

            for (int i = 0; i < numUniqueKey; i++) //LOOP REGIMENT CAREFULL!
            {
                Entity regiment = uniqueKeyRegiment[i];
                Debug.Log($"rearrange: regiment{regiment.Index}");
                //Needed for GetIndexAlgorithm
                DynamicBuffer<Buffer_Units> unitsBuffer = GetBuffer<Buffer_Units>(regiment);
                NativeArray<Entity> regimentUnits = unitsBuffer.Reinterpret<Entity>().ToNativeArray(Allocator.Temp);

                //Use filter regiment on units query
                
                NativeList<int> deadIndicesInRegiment = GetDeadIndicesForRegiment(pairRegimentUnit, regiment);
                deadIndicesInRegiment.Sort();
                int numBaseDead = deadIndicesInRegiment.Length;

                while (deadIndicesInRegiment.Length != 0)
                {
                    DynamicFormation formation = GetFormationRegiment(regiment);

                    int deadIndex = deadIndicesInRegiment[0];
                    int swapIndex = RearrangmentUtils.GetIndexAround
                    (
                        unitKilledQuery,
                        regimentUnits,
                        deadIndex,
                        formation.NumLine,
                        formation.UnitsPerLine, 
                        formation.NumUnitsLastLine
                    );
                    
                    if (swapIndex == -1)
                    {
                        deadIndicesInRegiment.RemoveAt(0);
                        break;
                    }
                    
                    SwapIndexInRegiment(regimentUnits[deadIndex], regimentUnits[swapIndex]);
                    unitsToMove.Add(regimentUnits[swapIndex]);
                    //(unitsBuffer[deadIndex], unitsBuffer[swapIndex]) = (unitsBuffer[swapIndex], unitsBuffer[deadIndex]);
                    (regimentUnits[deadIndex], regimentUnits[swapIndex]) = (regimentUnits[swapIndex], regimentUnits[deadIndex]);
                    
                    deadIndicesInRegiment.RemoveAt(0);
                    deadIndicesInRegiment.Add(swapIndex);
                    deadIndicesInRegiment.Sort();
                }

                int numUnit = GetComponent<NumberUnits>(regiment).Value;
                SetComponent(regiment, new NumberUnits(){Value = numUnit-numBaseDead});
                unitsBuffer.CopyFrom(regimentUnits.Reinterpret<Buffer_Units>());
                unitsBuffer.RemoveRange((unitsBuffer.Length) - numBaseDead, numBaseDead);
                
                Debug.Log($"regiment: {regiment.Index}; Anim: {GetComponent<Data_RegimentAnimationPlayed>(regiment).Value}" );
                if (EntityManager.GetComponentData<Data_RegimentAnimationPlayed>(regiment).Value == FusilierClips.Idle)
                {
                    EntityManager.AddComponent<Tag_MoveRearrange>(unitsToMove.AsArray());
                }
                unitsToMove.Clear();
            }
            
            //EntityManager.AddComponent<Tag_MoveRearrange>(unitsToMove.AsArray());
            
            EntityManager.RemoveComponent(unitKilledQuery, new ComponentTypes(typeof(Data_Regiment), typeof(Data_IndexInRegiment)));
            EntityManager.DestroyEntity(unitKilledQuery);
        }
        
        public void SwapIndexInRegiment(Entity deadUnit, Entity swapedUnit)
        {
            Data_IndexInRegiment indexDead = GetComponent<Data_IndexInRegiment>(deadUnit);
            Data_IndexInRegiment indexSwap = GetComponent<Data_IndexInRegiment>(swapedUnit);
            
            SetComponent(swapedUnit, indexDead);
            SetComponent(deadUnit, indexSwap);
        }

        public DynamicFormation GetFormationRegiment(Entity regiment)
        {
            int numUnits = GetComponent<NumberUnits>(regiment).Value;
            int unitsPerLine = GetComponent<UnitsPerLine>(regiment).Value;
            return new DynamicFormation(numUnits, unitsPerLine);
        }
        
        private NativeList<int> GetDeadIndicesForRegiment(NativeParallelMultiHashMap<Entity, Entity> pairRegimentUnit, Entity regiment)
        {
            NativeList<int> deadIndicesInRegiment = new (pairRegimentUnit.CountValuesForKey(regiment), Allocator.Temp);
            NativeParallelMultiHashMap<Entity, Entity>.Enumerator unitsInKey = pairRegimentUnit.GetValuesForKey(regiment);
            foreach (Entity unity in unitsInKey)
            {
                int indexInRegiment = GetComponent<Data_IndexInRegiment>(unity).Value;
                deadIndicesInRegiment.Add(indexInRegiment);
            }
            return deadIndicesInRegiment;
        }
        
        private NativeParallelMultiHashMap<Entity, Entity> GetPairRegimentDeadUnit(NativeArray<Entity> deadUnits)
        {
            NativeParallelMultiHashMap<Entity, Entity> pairRegimentUnit = new(deadUnits.Length, Allocator.Temp);
            foreach (Entity deadUnit in deadUnits)
            {
                Entity regiment = GetComponent<Data_Regiment>(deadUnit).Value;
                pairRegimentUnit.Add(regiment, deadUnit);
            }
            return pairRegimentUnit;
        }
    }
}