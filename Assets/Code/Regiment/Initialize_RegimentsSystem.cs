using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class Initialize_RegimentsSystem : SystemBase
    {
        private EntityQuery unInitializeRegiment;

        protected override void OnCreate()
        {
            unInitializeRegiment = GetEntityQuery(typeof(Tag_Regiment),typeof(Tag_Uninitialize),typeof(Data_PrefabsRegiment));
            RequireForUpdate(unInitializeRegiment);
        }

        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithStoreEntityQueryInField(ref unInitializeRegiment)
            .ForEach((Entity regiment ,in Data_RegimentClass regimentClass, in Data_PrefabsRegiment prefabs, in Translation position, in Data_SpawnAxeDirection axis) =>
            {
                GetBuffer<Buffer_Units>(regiment).EnsureCapacity(regimentClass.BaseNumUnits);
                CreateUnits(regiment, regimentClass, prefabs, position.Value, axis);
                CreatePlacements(regiment, regimentClass, prefabs, position.Value, axis);
                
                DynamicBuffer<Buffer_Destinations> destinations = EntityManager.AddBuffer<Buffer_Destinations>(regiment);
                destinations.EnsureCapacity(regimentClass.BaseNumUnits);
            }).Run();
            EntityManager.RemoveComponent<Tag_Uninitialize>(unInitializeRegiment);
            Enabled = false;
        }

        private void CreateUnits(Entity regiment, in Data_RegimentClass regimentClass, in Data_PrefabsRegiment prefabs, float3 pos, Data_SpawnAxeDirection axis)
        {
            using NativeArray<Entity> units = new (regimentClass.BaseNumUnits, Allocator.Temp);
            EntityManager.Instantiate(prefabs.UnitPrefab, units);
            int signZ = axis.Z;
            int signX = axis.X;
            Rotation rot = GetComponent<Rotation>(regiment);
            GetBuffer<Buffer_Units>(regiment).AddRange(units.Reinterpret<Buffer_Units>());
            for (int i = 0; i < units.Length; i++)
            {
                Entity unit = units[i];
                PhysicsUtils.AddToCollisionFilter(EntityManager, unit, HasComponent<Tag_Player>(regiment) ? 3 : 4);

                EntityManager.SetName(unit, $"Unit_Regiment{regiment.Index}_{unit.Index}_RegId{i}");
                EntityManager.SetEnabled(unit, true);
                //EntityManager.SetComponentData(unit, new Data_IndexInRegiment(){Value = i});
                
                float3 position = GetPositionInRegiment(pos, i, 1, GetComponent<Data_UnitsPerLine>(regiment).Value, signZ, signX);
                EntityManager.SetComponentData(unit, new Translation(){Value = position});
                EntityManager.SetComponentData(unit, rot);
                //TODO : FIND A WAY TO DO THIS IN => "Initialize_UnitsSystem"
                EntityManager.AddSharedComponentData(unit, new Shared_RegimentEntity(){Value = regiment});
                
                //SYSTEM STATE COMPONENT
                EntityManager.AddComponentData(unit, new Data_IndexInRegiment() { Value = i });
                EntityManager.AddComponentData(unit, new Data_Regiment() { Value = regiment });
            }
        }

        private void CreatePlacements(Entity regiment, in Data_RegimentClass regimentClass, in Data_PrefabsRegiment prefabs, float3 pos, Data_SpawnAxeDirection axis)
        {
            using NativeArray<Entity> placements = new (regimentClass.BaseNumUnits, Allocator.Temp);
            EntityManager.Instantiate(prefabs.PrefabPlacement, placements);
            
            int signZ = axis.Z;
            int signX = axis.X;
            
            Rotation rot = GetComponent<Rotation>(regiment);
            for (int i = 0; i < placements.Length; i++)
            {
                Entity placement = placements[i];
                EntityManager.SetName(placement, $"Placement_R{regiment.Index}_{placement.Index}_IdInReg{i}");
                float3 position = GetPositionInRegiment(pos, i, 1, GetComponent<Data_UnitsPerLine>(regiment).Value, signZ, signX);
                EntityManager.SetComponentData(placements[i], new Translation(){Value = new float3(position.x,0.05f,position.z)});
                EntityManager.SetComponentData(placements[i], rot);
                EntityManager.AddSharedComponentData(placements[i], new Shared_RegimentEntity(){Value = regiment});
            }
        }
        
        private float3 GetPositionInRegiment(float3 regimentPosition, int index, float unitSizeX, int unitPerLine, int signZ, int signX)
        {
            int row = unitPerLine;
                
            //Coord according to index
            int z = index / row;
            int x = index - (z * row);
                
            //Offset to place regiment in the center of the mass
            float offsetX = regimentPosition.x - GetXOffset();

            return new float3(signX*(x * unitSizeX + offsetX), 0f, ((z * unitSizeX)) + regimentPosition.z);
            
            float GetXOffset()
            {
                float unitHalfOffset = unitSizeX / 2f;
                float halfRow = row / 2f;
                return (halfRow * unitSizeX - unitHalfOffset);
            }
        }
    }
}