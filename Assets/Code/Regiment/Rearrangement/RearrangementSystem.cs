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
            //Enabled = false;
        }
        /*
        [BurstCompile(CompileSynchronously = true)]
        public partial struct JRearrange : IJobEntityBatch
        {

            public BufferTypeHandle<Buffer_Units> BufferUnitsTypeHandle;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                BufferAccessor<Buffer_Units> units = batchInChunk.GetBufferAccessor(BufferUnitsTypeHandle);

                for (int i = 0; i < batchInChunk.Count; i++)
                {
                    //batchInChunk.
                }

                //NativeArray<Entity> regiments = batchInChunk.GetNativeArray(RegimentTypeHandle).Reinterpret<Entity>();
                //NativeArray<int> indexInRegiment = batchInChunk.GetNativeArray(IndexInRegimentTypeHandle).Reinterpret<int>();
            }
            
            private NativeList<int> GetDeadIndicesForRegiment(NativeParallelMultiHashMap<Entity, Entity> pairRegimentUnit, Entity regiment)
            {
                NativeList<int> deadIndicesInRegiment = new (pairRegimentUnit.CountValuesForKey(regiment), Allocator.TempJob);
                NativeParallelMultiHashMap<Entity, Entity>.Enumerator unitsInKey = pairRegimentUnit.GetValuesForKey(regiment);
                foreach (Entity unity in unitsInKey)
                {
                    int indexInRegiment = GetComponent<Data_IndexInRegiment>(unity).Value;
                    deadIndicesInRegiment.Add(indexInRegiment);
                }
                return deadIndicesInRegiment;
            }
        }
        */
        
        
        protected override void OnUpdate()
        {
            using NativeList<Entity> unitsToMove = new(2, Allocator.TempJob);

            //Query dead units
            using NativeArray<Entity> deadUnits = unitKilledQuery.ToEntityArray(Allocator.TempJob);
            using NativeParallelMultiHashMap<Entity, Entity> pairRegimentUnit = GetPairRegimentDeadUnit(deadUnits);
            
            using NativeArray<Entity> uniqueKeyRegiment = pairRegimentUnit.GetKeyArray(Allocator.TempJob);
            int numUniqueKey = uniqueKeyRegiment.Unique();
            
            for (int i = 0; i < numUniqueKey; i++) //LOOP REGIMENT CAREFULL!
            {
                Entity regiment = uniqueKeyRegiment[i];
                
                //Needed for GetIndexAlgorithm
                DynamicBuffer<Buffer_Units> unitsBuffer = GetBuffer<Buffer_Units>(regiment);
                NativeArray<Entity> regimentUnits = unitsBuffer.Reinterpret<Entity>().ToNativeArray(Allocator.Temp);

                using NativeList<int> deadIndicesInRegiment = GetDeadIndicesForRegiment(pairRegimentUnit, regiment);
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
            }
            EntityManager.AddComponent<Tag_MoveRearrange>(unitsToMove.AsArray());
            
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

        
        public int GetIndexAround(NativeArray<Entity> units, int index, in DynamicFormation formation)
        {
            //(int coordX, int coordY) = index.GetXY(formation.UnitsPerLine);
            int coordInRegimentY = index / formation.UnitsPerLine;
            int coordInRegimentX = index - (coordInRegimentY * formation.UnitsPerLine);
            
            int lastLineIndex = formation.NumLine - 1;

            if (coordInRegimentY == lastLineIndex)
            {
                return RearrangeInline(units, coordInRegimentX, coordInRegimentY, formation.UnitsPerLine, formation.LastIndex);
            }
            //for (int line = coordInRegimentY + 1; line < formation.NumLine; line++)
            for (int line = 1; line < formation.NumLine - coordInRegimentY; line++)
            {
                //We first check if there is something to check behind
                if (!IsNextRowValid(units, coordInRegimentY, formation)) continue;
                
                int lineIndexChecked = coordInRegimentY + line;
                
                int lineWidth = lineIndexChecked == lastLineIndex ? formation.NumUnitsLastLine : formation.UnitsPerLine;
                int lastLineIndexChecked = lineWidth - 1;

                bool2 leftRightClose = new (coordInRegimentX == 0, coordInRegimentX == lastLineIndexChecked);

                //We first check if there is something to check behind
                //if (!IsNextRowValid(units, coordY, formation)) continue;

                int indexUnit = -1;
                
                for (int i = 0; i <= lineWidth; i++) // 0 because we check unit right behind
                {
                    //Check Unit Behind : MOVE THIS OUT and start at 1 insstead
                    if (i == 0)
                    {
                        indexUnit = mad(coordInRegimentY + line, formation.UnitsPerLine, coordInRegimentX);

                        //Check if we pick on the last line: then adjust considering if the line is complete or not
                        if (coordInRegimentY + line == formation.NumLine - 1)
                        {
                            int unitBehindIndex = GetUnitBehindInUnEvenLastLine(indexUnit, formation);
                            indexUnit = select(unitBehindIndex, min(indexUnit, formation.NumUnits - 1), formation.IsEven);
                        }

                        if (IsUnitValid(units[indexUnit])) return indexUnit;

                        leftRightClose = IsLeftRightClose(indexUnit, formation.NumUnits, lineWidth);
                        continue;
                    }
                    
                    
                    //Check Left/Negative Index
                    if (!leftRightClose.x)
                    {
                        int x = min(coordInRegimentX, lineWidth) - i;
                        indexUnit = new int2(x, coordInRegimentY + line).GetIndex(formation.UnitsPerLine);

                        if (indexUnit < units.Length && IsUnitValid(units[indexUnit])) return indexUnit;
                        leftRightClose.x = min(coordInRegimentX, lineWidth) - i == 0;
                    }

                    //Check Right/Positiv Index
                    if (!leftRightClose.y)
                    {
                        indexUnit = new int2(coordInRegimentX + i, coordInRegimentY + line).GetIndex(formation.UnitsPerLine);
                        
                        if(indexUnit < units.Length && IsUnitValid(units[indexUnit])) return indexUnit;
                        leftRightClose.y = coordInRegimentX + i == lastLineIndexChecked;
                    }
                    
                    /*
                    if (IsRightValid(formation.UnitsPerLine)) return indexUnit;
                    if (IsLeftValid(formation.UnitsPerLine)) return indexUnit;
                    */
                    //No more unit to check in this line
                    if (all(leftRightClose)) break;
                    
                    bool IsRightValid(int unitsPerLine)
                    {
                        if (leftRightClose.x) return false;
                        int x = min(coordInRegimentX, lineWidth) - i;
                        indexUnit = mad(coordInRegimentY + line, unitsPerLine, x);
                        leftRightClose.x = min(coordInRegimentX, lineWidth) - i == 0;
                        return IsUnitValid(units[indexUnit]);
                    }

                    bool IsLeftValid(int unitsPerLine)
                    {
                        if (leftRightClose.y) return false;
                        indexUnit = new int2(coordInRegimentX + i, coordInRegimentY + line).GetIndex(unitsPerLine);
                        leftRightClose.y = coordInRegimentX + i == lastLineIndexChecked;
                        return IsUnitValid(units[indexUnit]);
                    }
                }
            }
            return -1;
        }
        
        private bool IsUnitValid(Entity unit)
        {
            return unit != Entity.Null && !unitKilledQuery.Matches(unit);
        }

        private bool IsNextRowValid(NativeArray<Entity> units, int yLine, in DynamicFormation formation)
        {
            int nextYLineIndex = yLine + 1;
            int totalLine = formation.NumLine;
            
            if (nextYLineIndex > totalLine - 1) return false;
            int numUnitOnLine = nextYLineIndex == totalLine - 1 ? formation.NumUnitsLastLine : formation.UnitsPerLine;
            
            NativeSlice<Entity> lineToCheck = new (units,nextYLineIndex * formation.UnitsPerLine, numUnitOnLine);
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
            //int increment = 1;
            int fullIndex = mad(coordY, unitPerLine, coordX);

            int maxIteration = units.Length - fullIndex;
            for (int i = 1; i < maxIteration; i++)
            {
                int xToCheck = coordX + i;
                fullIndex = mad(coordY, unitPerLine, xToCheck);
                if(IsUnitValid(units[fullIndex])) return fullIndex;
            }
            /*
            while (fullIndex < lastIndexFormation)
            {
                int xToCheck = coordX + increment;
                fullIndex = mad(coordY, unitPerLine, xToCheck);
                if(IsUnitValid(units[fullIndex])) return fullIndex;
                increment++;
            }
            */
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