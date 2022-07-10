using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SelectionSystem))]
    public partial class SelectionHighlightSystem : SystemBase
    {
        private EntityQuery regimentUpdatedQuery;
        private EntityQuery selectionHighlightQuery;
        private ButtonControl leftMouseClick;
        
        protected override void OnCreate()
        {
            EntityQueryDesc description = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(Tag_Selection), typeof(Shared_RegimentEntity) },
                Options = EntityQueryOptions.IncludeDisabled
            };
            selectionHighlightQuery = GetEntityQuery(description);
            
            regimentUpdatedQuery = GetEntityQuery(ComponentType.ReadOnly<Tag_Regiment>(), typeof(Flag_Selection));
            regimentUpdatedQuery.SetChangedVersionFilter(typeof(Flag_Selection));
        }

        protected override void OnStartRunning()
        {
            leftMouseClick = Mouse.current.leftButton;
        }

        protected override void OnUpdate()
        {
            if (!leftMouseClick.wasReleasedThisFrame) return;
            NativeParallelHashMap<Entity, bool> regimentUpdated = new (2, Allocator.TempJob);
            JGetRegimentSelected job = new (){ RegimentUpdated = regimentUpdated };
            job.Run(regimentUpdatedQuery);
            
            foreach (KeyValue<Entity, bool> update in regimentUpdated)
            {
                selectionHighlightQuery.SetSharedComponentFilter(new Shared_RegimentEntity(){Value = update.Key});
                EntityManager.SelectAddOrRemove<DisableRendering>(selectionHighlightQuery, !update.Value);
                selectionHighlightQuery.ResetFilter();
            }
            regimentUpdated.Dispose();
        }
        
        [BurstCompile(CompileSynchronously = true)]
        [WithAll(typeof(Tag_Regiment))]
        [WithChangeFilter(typeof(Flag_Selection))]
        private partial struct JGetRegimentSelected : IJobEntity
        {
            [WriteOnly] public NativeParallelHashMap<Entity, bool> RegimentUpdated;

            private void Execute(Entity regimentEntity, ref Filter_Selection filter, in Flag_Selection flag)
            {
                if (!filter.DidChange) return;
                RegimentUpdated.Add(regimentEntity, flag.IsActive);
                filter.DidChange = false;
            }
        }
    }
}