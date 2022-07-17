using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWald
{
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
                DynamicBuffer<TempData_RegimentOrders> data = GetBuffer<TempData_RegimentOrders>(factories[i]);

                //float xOffset = 0;
                foreach (TempData_RegimentOrders t in data)
                {
                    NativeArray<Entity> regiments = new (data[i].Number, Allocator.Temp);
                    EntityManager.Instantiate(data[i].RegimentPrefab, regiments);
                    
                    RemoveFromRegiments(regiments);
                    
                    AddToRegiments(regiments);
                    
                    SetRegimentPosition(regiments);
                }
            }
            EntityManager.DestroyEntity(factoryQuery);
            Enabled = false;
        }

        private void SetRegimentPosition(NativeArray<Entity> regiments)
        {
            float xOffset = 0;
            for (int regimentIndex = 0; regimentIndex < regiments.Length; regimentIndex++)
            {
                float3 position = new float3(xOffset, 0, 0);
                Data_RegimentClass regClass = GetComponent<Data_RegimentClass>(regiments[regimentIndex]);
                SetComponent(regiments[regimentIndex], new Translation(){Value = position});
                xOffset += (regClass.MinRow + 4) * regClass.SpaceBetweenUnitsX;
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
        }

        protected override void OnUpdate()
        {
            return;
        }

        protected override void OnStopRunning()
        {
            Debug.Log("End Factory System");
        }
    }
}