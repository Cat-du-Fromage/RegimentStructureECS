using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(Initialize_RegimentsSystem))]
    public partial class Initialize_UnitsSystem : SystemBase
    {
        private EntityQuery unInitializeHighlights;

        protected override void OnCreate()
        {
            unInitializeHighlights = EntityManager.CreateEntityQuery(typeof(Tag_Uninitialize), typeof(Tag_Unit));
            RequireForUpdate(unInitializeHighlights);
        }

        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithAll<Tag_Uninitialize,Tag_Unit>()
            .WithStoreEntityQueryInField(ref unInitializeHighlights)
            .ForEach((Entity unit, ref Data_Regiment regiment) =>
            {
                regiment.Value = EntityManager.GetSharedComponentData<Shared_RegimentEntity>(unit).Value;
            }).Run();
            EntityManager.RemoveComponent<Tag_Uninitialize>(unInitializeHighlights);
            Enabled = false;
        }
    }
}