using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
/*
namespace KaizerWald
{
    public partial class SpawnerSystem : SystemBase
    {
        private EntityManager em;
        private EntityQuery query;

        protected override void OnCreate()
        {
            Enabled = false;
            
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
            query = em.CreateEntityQuery(typeof(Tag_Uninitialize));
            
            RequireForUpdate(query);
        }

        protected override void OnUpdate()
        {
            Debug.Log("Update Spawner");
            
            ComponentTypes preselectionData = new (typeof(Flag_Preselection), typeof(Filter_Preselection));
            ComponentTypes selectionData = new (typeof(Flag_Selection), typeof(Filter_Selection));

            Entities
                .WithStructuralChanges()
                .WithAll<Tag_Uninitialize>()
                .WithStoreEntityQueryInField(ref query)
                .ForEach((Entity regimentEntity, int entityInQueryIndex, in RegimentData regimentData) =>
                {
                    em.AddComponent<Tag_Regiment>(regimentEntity);
                    em.AddComponent<LocalToWorld>(regimentEntity);
                    em.AddComponents(regimentEntity, preselectionData);
                    em.AddComponents(regimentEntity, selectionData);

                    NativeArray<Entity> units = CreateUnits(regimentData);
                    Shared_RegimentEntity sharedSharedRegiment = new Shared_RegimentEntity() { Value = regimentEntity };

                    for (int i = 0; i < units.Length; i++)
                    {
                        DynamicBuffer<LinkedEntityGroup> bufferUp = EntityManager.GetBuffer<LinkedEntityGroup>(regimentEntity);
                        bufferUp.Add(new LinkedEntityGroup() { Value = units[i] });
                        //DynamicBuffer<Child> bufferUp = EntityManager.GetBuffer<Child>(regimentEntity);
                        //bufferUp.Add(new Child() { Value = units[i] });
                        
                        em.AddComponent<Tag_Unit>(units[i]);
                        em.AddComponents(units[i], preselectionData);
                        
                        //em.AddComponentData(units[i], new Parent() {Value = regimentEntity} );
                        //em.AddComponent<LocalToParent>(units[i]);
                        
                        em.AddComponentData(units[i], new Data_Regiment(){Value = regimentEntity});
                        em.AddSharedComponentData(units[i], sharedSharedRegiment);
                    }
                    
                    float3 regimentPosition = entityInQueryIndex * (new float3(1) * 12);
                    float unitSizeX = em.GetComponentData<LocalToWorld>(units[0]).Value.c3.w;
                    
                    for (int i = 0; i < units.Length; i++)
                    {
                        Translation position = new Translation() { Value = GetPositionInRegiment(regimentPosition, i, unitSizeX) };
                        em.SetComponentData(units[i], position);
                    }
                    SetUpHighlights(units, sharedSharedRegiment);
                    //SetUpPlacements(units, sharedSharedRegiment, regimentData);
                    
                    units.Dispose();
                }).Run();

            em.RemoveComponent<Tag_Uninitialize>(query);
        }

        private NativeArray<Entity> CreateUnits(in RegimentData regimentData)
        {
            NativeArray<Entity> units = new (regimentData.NumUnits, Allocator.Temp);
            em.Instantiate(regimentData.UnitPrefab, units);
            return units;
        }

        private void SetUpHighlights(NativeArray<Entity> units, Shared_RegimentEntity sharedSharedRegiment)
        {
            for (int i = 0; i < units.Length; i++)
            {
                DynamicBuffer<LinkedEntityGroup> bufferUp = em.GetBuffer<LinkedEntityGroup>(units[i]);
                Entity preselectionHighlight = bufferUp[1].Value;
                Entity selectionHighlight = bufferUp[2].Value;
                
                //Preselection
                em.AddComponent<Tag_Preselection>(preselectionHighlight);
                em.AddSharedComponentData(preselectionHighlight, sharedSharedRegiment);

                //Selection
                em.AddComponent<Tag_Selection>(selectionHighlight);
                em.AddSharedComponentData(selectionHighlight, sharedSharedRegiment);
            }
        }

        private void SetUpPlacements(NativeArray<Entity> units, Shared_RegimentEntity sharedSharedRegiment, in RegimentData regimentData)
        {
            NativeArray<Entity> placements = new (units.Length, Allocator.Temp);
            em.Instantiate(regimentData.UnitPrefab, placements);
            
            for (int i = 0; i < units.Length; i++)
            {
                float3 position = GetComponent<Translation>(units[i]).Value;
                SetComponent(placements[i], new Translation(){Value = position});
            }
        }
        
        private float3 GetPositionInRegiment(float3 regimentPosition, int index, float unitSizeX)
        {
            int row = 10;
                
            //Coord according to index
            int z = index / row;
            int x = index - (z * row);
                
            //Offset to place regiment in the center of the mass
            float offsetX = regimentPosition.x - GetXOffset();

            return new float3(x * unitSizeX + offsetX, 1f, -(z * unitSizeX));
            
            float GetXOffset()
            {
                float unitHalfOffset = unitSizeX / 2f;
                float halfRow = row / 2f;
                return (halfRow * unitSizeX - unitHalfOffset);
            }
        }
    }
}
*/