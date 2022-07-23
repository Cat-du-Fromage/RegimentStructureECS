using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWald
{
    public struct Data_SpawnAxeDirection : IComponentData
    {
        public int X;
        public int Z;
    }
    public struct Data_RegimentAnimationPlayed : IComponentData
    {
        public FusilierClips Value;
    }
    
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class RegimentFactorySystem : SystemBase
    {
        private EntityQuery factoryQuery;
        
        protected override void OnCreate()
        {
            factoryQuery = GetEntityQuery(typeof(TempData_RegimentOrders));
            RequireForUpdate(factoryQuery);
        }

        protected override void OnStartRunning()
        {
            NativeArray<Entity> factories = factoryQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < factories.Length; i++)
            {
                quaternion rotationFactory = GetComponent<LocalToWorld>(factories[i]).Rotation;
                Data_SpawnAxeDirection axesDir = new Data_SpawnAxeDirection()
                {
                    X = (int)GetComponent<LocalToWorld>(factories[i]).Right.x,
                    Z = (int)GetComponent<LocalToWorld>(factories[i]).Forward.z
                };
                
                //Debug.Log($"x: {axesDir.X}; z: {axesDir.Z}");
                DynamicBuffer<TempData_RegimentOrders> data = GetBuffer<TempData_RegimentOrders>(factories[i]);
                
                foreach (TempData_RegimentOrders t in data)
                {
                    NativeArray<Entity> regiments = new (t.Number, Allocator.Temp);
                    EntityManager.Instantiate(t.RegimentPrefab, regiments);

                    if (t.IsPlacer)
                    {
                        EntityManager.AddComponent<Tag_Player>(regiments);
                    }
                    else
                    {
                        EntityManager.AddComponent<Tag_Enemy>(regiments);
                    }
                    
                    RemoveFromRegiments(regiments);
                    
                    AddToRegiments(regiments);
                    
                    SetRegimentPosition(regiments, t.IsPlacer, t.SpawnStartPosition, rotationFactory, axesDir);
                }
                
            }
            //EntityManager.DestroyEntity(factoryQuery);
            Enabled = false;
        }
        
        protected override void OnUpdate()
        {
            return;
        }

        protected override void OnStopRunning()
        {
            Debug.Log("End Factory System");
        }

        private void SetRegimentPosition(NativeArray<Entity> regiments, bool isPlayer, float3 basePosition, quaternion rotation, Data_SpawnAxeDirection axesDir)
        {
            float xOffset = 0;
            for (int regimentIndex = 0; regimentIndex < regiments.Length; regimentIndex++)
            {
                float3 position = new float3(xOffset, 0, basePosition.z);
                Data_RegimentClass regClass = GetComponent<Data_RegimentClass>(regiments[regimentIndex]);
                
                //DIFFERENCE PLAYER - ENEMY
                int unitPerLine = isPlayer ? regClass.MinRow : regClass.MaxRow;
                SetComponent(regiments[regimentIndex], new Data_UnitsPerLine(){Value = unitPerLine});
                
                SetComponent(regiments[regimentIndex], new Translation(){Value = position});
                xOffset += (unitPerLine + 4) * regClass.SpaceBetweenUnitsX;
                SetComponent(regiments[regimentIndex], new Rotation(){Value = rotation});

                EntityManager.AddComponentData(regiments[regimentIndex], axesDir);
            }
        }

        private void RemoveFromRegiments(NativeArray<Entity> regiments)
        {
            EntityManager.RemoveComponent<LinkedEntityGroup>(regiments);
        }

        private void AddToRegiments(NativeArray<Entity> regiments)
        {
            EntityManager.AddComponent<Tag_Uninitialize>(regiments);
            //PRESELECTION
            EntityManager.AddComponent<Flag_Preselection>(regiments);
            EntityManager.AddComponent<Filter_Preselection>(regiments);
            //SELECTION
            EntityManager.AddComponent<Flag_Selection>(regiments);
            EntityManager.AddComponent<Filter_Selection>(regiments);
            //ANIMATION
            EntityManager.AddComponent<Data_RegimentAnimationPlayed>(regiments);
            EntityManager.AddComponent<Data_LookRotation>(regiments);
            
            
            for (int i = 0; i < regiments.Length; i++)
            {
                EntityManager.AddBuffer<Buffer_Units>(regiments[i]);
                //EntityManager.AddComponents(regiments[i],FormationUtility.GetFormationComponents());
            }
        }

        
    }
}