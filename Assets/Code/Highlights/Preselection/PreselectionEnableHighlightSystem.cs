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
                All = new ComponentType[] { typeof(Tag_Preselection), typeof(Shared_RegimentEntity) },
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
            
            JGetRegimentPreselected job = new (){ RegimentUpdated = regimentUpdated };
            job.Run();
            
            foreach (KeyValue<Entity, bool> update in regimentUpdated)
            {
                preselectionHighlightQuery.SetSharedComponentFilter(new Shared_RegimentEntity(){Value = update.Key});
                EntityManager.SelectAddOrRemove<DisableRendering>(preselectionHighlightQuery, !update.Value);
                preselectionHighlightQuery.ResetFilter();
            }
            regimentUpdated.Dispose();
        }
        
        //ATTENTION "WithChangeFilter": Change TOUT LE CHUNK pas uniquement les entités avec les comp changés!
        [BurstCompile(CompileSynchronously = true)]
        [WithAll(typeof(Tag_Regiment))]
        [WithChangeFilter(typeof(Flag_Preselection))]
        private partial struct JGetRegimentPreselected : IJobEntity
        {
            [WriteOnly] public NativeParallelHashMap<Entity, bool> RegimentUpdated;

            private void Execute(Entity regimentEntity, ref Filter_Preselection filter, in Flag_Preselection flag)
            {
                if (!filter.DidChange) return;
                RegimentUpdated.Add(regimentEntity, flag.IsActive);
                filter.DidChange = false;
            }
        }
    }
}