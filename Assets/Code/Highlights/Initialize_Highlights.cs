using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(Initialize_UnitsSystem))]
    public partial class Initialize_Highlights : SystemBase
    {
        private EntityQuery unInitializeHighlights;
        
        protected override void OnCreate()
        {
            EntityQueryDesc description = new EntityQueryDesc
            {
                Any = new ComponentType[] { typeof(Tag_Preselection), typeof(Tag_Selection) },
                All = new ComponentType[] { typeof(Tag_Uninitialize) },
            };
            
            unInitializeHighlights = EntityManager.CreateEntityQuery(description);
            RequireForUpdate(unInitializeHighlights);
        }

        protected override void OnUpdate()
        {
            //ATTENTION : Seem WithStoreEntityQueryInField not work as intended! (capture a bone which only has parent)
            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithAll<Tag_Uninitialize>()
            .WithAny<Tag_Preselection, Tag_Selection>()
            .WithStoreEntityQueryInField(ref unInitializeHighlights)
            .ForEach((Entity highlight, in Parent unit) =>
            {
                Shared_RegimentEntity sharedRegiment = EntityManager.GetSharedComponentData<Shared_RegimentEntity>(unit.Value);
                EntityManager.AddSharedComponentData(highlight, sharedRegiment);
            }).Run();
            EntityManager.RemoveComponent<Tag_Uninitialize>(unInitializeHighlights);
            Enabled = false;
        }
    }
}