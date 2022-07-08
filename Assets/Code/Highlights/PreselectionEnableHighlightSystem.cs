using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SinglePreselectionInputSystem))]
    public partial class PreselectionEnableHighlightSystem : SystemBase
    {
        private EntityQuery preselectionHighlightQuery;

        protected override void OnCreate()
        {
            EntityQueryDesc description = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(TPreselection), typeof(RegimentSharedData) },
                Options = EntityQueryOptions.IncludeDisabled
            };
            preselectionHighlightQuery = GetEntityQuery(description);
        }

        protected override void OnUpdate()
        {
            UpdateHighlights();
        }

        private void UpdateHighlights()
        {
            NativeParallelHashMap<Entity, bool> regimentUpdated = new (2, Allocator.TempJob);
            
            new JGetRegimentChanged() { RegimentUpdated = regimentUpdated }.Run();
            
            foreach (KeyValue<Entity, bool> update in regimentUpdated)
            {
                preselectionHighlightQuery.SetSharedComponentFilter(new RegimentSharedData(){Regiment = update.Key});
                EntityManager.SelectAddOrRemove<DisableRendering>(preselectionHighlightQuery, !update.Value);
                preselectionHighlightQuery.ResetFilter();
            }
            regimentUpdated.Dispose();
        }

        private void Test2()
        {
            NativeList<bool> regimentToUpdateState = new (2, Allocator.TempJob);
            NativeList<Entity> regimentToUpdate = new (2, Allocator.TempJob);

            new JGetRegimentChanged2()
            {
                RegimentToUpdateState = regimentToUpdateState,
                RegimentToUpdate = regimentToUpdate
            }.Run();

            for (int i = 0; i < regimentToUpdate.Length; i++)
            {
                preselectionHighlightQuery.SetSharedComponentFilter(new RegimentSharedData(){Regiment = regimentToUpdate[i]});
                if (regimentToUpdateState[i])
                {
                    EntityManager.RemoveComponent<DisableRendering>(preselectionHighlightQuery);
                }
                else
                {
                    //CAREFULL: with Disable, we need to make an ecb 
                    //otherwise, the renderer seems to not know the entity shouldn't be rendered
                    EntityManager.AddComponent<DisableRendering>(preselectionHighlightQuery);
                }
                preselectionHighlightQuery.ResetFilter();
            }

            regimentToUpdateState.Dispose();
            regimentToUpdate.Dispose();
            
        }
        
        [BurstCompile]
        [WithAll(typeof(Tag_Regiment))]
        [WithChangeFilter(typeof(Flag_Preselection))]
        private partial struct JGetRegimentChanged : IJobEntity
        {
            [WriteOnly] public NativeParallelHashMap<Entity, bool> RegimentUpdated;
            
            public void Execute(Entity regimentEntity, ref Fitler_Preselection filter, in Flag_Preselection flag)
            {
                if (!filter.DidChange) return;
                RegimentUpdated.Add(regimentEntity, flag.IsActive);
                filter.DidChange = false;
            }
        }
        
        [BurstCompile]
        [WithAll(typeof(Tag_Regiment))]
        [WithChangeFilter(typeof(Flag_Preselection))]
        private partial struct JGetRegimentChanged2 : IJobEntity
        {
            [WriteOnly] public NativeList<bool> RegimentToUpdateState;
            [WriteOnly] public NativeList<Entity> RegimentToUpdate;

            public void Execute(Entity regimentEntity, ref Fitler_Preselection filter, in Flag_Preselection flag)
            {
                if (!filter.DidChange) return;
                RegimentToUpdate.Add(regimentEntity);
                RegimentToUpdateState.Add(flag.IsActive);
                filter.DidChange = false;
            }
        }

        private void Test1()
        {
            Entities
                .WithName("Test")
                .WithoutBurst()
                .WithStructuralChanges()
                .WithAll<Tag_Regiment>()
                .WithChangeFilter<Flag_Preselection>()
                .ForEach((Entity regimentEntity, ref Fitler_Preselection filter, in Flag_Preselection flag, in DynamicBuffer<LinkedEntityGroup> units) =>
                {
                    if (!filter.DidChange) return;
                    
                    //PLUS Lent (0.32ms, 0.40ms, (marginale)0.49ms)
                    NativeArray<Entity> tempBuffer = units.ToNativeArray(Allocator.Temp).Reinterpret<Entity>();
                    if(flag.IsActive)
                        EntityManager.AddComponent<TIsPreselected>(tempBuffer);
                    else
                        EntityManager.RemoveComponent<TIsPreselected>(tempBuffer);
                    
                    /*
                    //PLUS RAPIDE ~(0.14, 0.17ms, 0.26ms)
                    for (int i = 0; i < units.Length; i++)
                    {
                        EntityManager.SetComponentData(units[i].Value, flag);
                    }
                    */
                    filter.DidChange = false;
                    
                }).Run();
        }

        [WithAll(typeof(Tag_Regiment))]
        [WithChangeFilter(typeof(Flag_Preselection))]
        private partial struct JUpdateUnits : IJobEntity
        {
            //[ReadOnly] public bool NewState;
            //private NativeList<Entity> regimentChanged;
            
            public void Execute(Entity entity, in Flag_Preselection flag)
            {
                Debug.Log($"{entity}");
            }
        }
        
    }
}