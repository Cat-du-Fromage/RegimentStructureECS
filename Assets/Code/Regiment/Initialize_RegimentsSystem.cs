using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

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
            .ForEach((Entity regiment, in Data_RegimentClass regimentClass, in Data_PrefabsRegiment prefabs, in Translation position) =>
            {
                CreateUnits(regiment, regimentClass, prefabs, position.Value);
                CreatePlacements(regiment, regimentClass, prefabs, position.Value);
                
                DynamicBuffer<Buffer_Destinations> destinations = EntityManager.AddBuffer<Buffer_Destinations>(regiment);
                destinations.EnsureCapacity(regimentClass.BaseNumUnits);
            }).Run();
            EntityManager.RemoveComponent<Tag_Uninitialize>(unInitializeRegiment);
            Enabled = false;
        }

        private void CreateUnits(Entity regiment, in Data_RegimentClass regimentClass,in Data_PrefabsRegiment prefabs, float3 pos)
        {
            using NativeArray<Entity> units = new (regimentClass.BaseNumUnits, Allocator.Temp);
            EntityManager.Instantiate(prefabs.UnitPrefab, units);
            EntityManager.RemoveComponent<LinkedEntityGroup>(units);
            
            for (int i = 0; i < units.Length; i++)
            {
                EntityManager.SetName(units[i], $"Unit_Regiment_{regiment.Index}_{i}");
                EntityManager.SetEnabled(units[i], true);
                
                float3 position = GetPositionInRegiment(pos, i, 1, GetComponent<Data_MinLine>(regiment).Value);
                EntityManager.SetComponentData(units[i], new Translation(){Value = position});
                
                //TODO : FIND A WAY TO DO THIS IN => "Initialize_UnitsSystem"
                EntityManager.AddSharedComponentData(units[i], new Shared_RegimentEntity(){Value = regiment});
            }
        }

        private void CreatePlacements(Entity regiment, in Data_RegimentClass regimentClass,in Data_PrefabsRegiment prefabs, float3 pos)
        {
            using NativeArray<Entity> placements = new (regimentClass.BaseNumUnits, Allocator.Temp);
            EntityManager.Instantiate(prefabs.PrefabPlacement, placements);
            for (int i = 0; i < placements.Length; i++)
            {
                float3 position = GetPositionInRegiment(pos, i, 1, GetComponent<Data_MinLine>(regiment).Value);
                EntityManager.SetComponentData(placements[i], new Translation(){Value = new float3(position.x,0.05f,position.z)});
                EntityManager.AddSharedComponentData(placements[i], new Shared_RegimentEntity(){Value = regiment});
            }
        }
        
        private float3 GetPositionInRegiment(float3 regimentPosition, int index, float unitSizeX, int minLine)
        {
            int row = minLine;
                
            //Coord according to index
            int z = index / row;
            int x = index - (z * row);
                
            //Offset to place regiment in the center of the mass
            float offsetX = regimentPosition.x - GetXOffset();

            return new float3(x * unitSizeX + offsetX, 0f, -(z * unitSizeX));
            
            float GetXOffset()
            {
                float unitHalfOffset = unitSizeX / 2f;
                float halfRow = row / 2f;
                return (halfRow * unitSizeX - unitHalfOffset);
            }
        }
    }
}