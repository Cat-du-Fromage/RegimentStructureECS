using Unity.Entities;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(CameraRaySystem))]
    public partial class PreselectionSystem : SystemBase
    {
        private Entity CameraRaySingleton;
        private BeginInitializationEntityCommandBufferSystem beginInitEcb;

        protected override void OnCreate()
        {
            Enabled = false;
            beginInitEcb = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnStartRunning()
        {
            CameraRaySingleton = GetSingletonEntity<SingletonCameraRayHit>();
            RequireSingletonForUpdate<SingletonCameraRayHit>();
        }

        protected override void OnUpdate()
        {
            if (GetComponent<IsClickDragPerformed>(CameraRaySingleton).Value) return;
            
            bool didChange = GetComponentDataFromEntity<SingletonCameraRayHit>(true).DidChange(CameraRaySingleton, LastSystemVersion);
            if (!didChange) return; //true means : we hover a NEW regiment or NO regiment at all!
            
            EntityCommandBuffer.ParallelWriter ecb = beginInitEcb.CreateCommandBuffer().AsParallelWriter();
            DisablePreviousPreselection(ecb); // We first unPreselect the previous regiment
            
            if (IsUnitHover(out Entity unit)) //Preselect regiment if we hover a unit
            {
                EnablePreselection(ecb, unit);
            }
            beginInitEcb.AddJobHandleForProducer(Dependency);
        }

        private bool IsUnitHover(out Entity unit)
        {
            SingletonCameraRayHit preselected = GetSingleton<SingletonCameraRayHit>();
            unit = preselected.UnitHit;
            return unit != Entity.Null;
        }
        
        //==============================================================================================================
        // ENABLE
        //==============================================================================================================
        private void EnablePreselection(EntityCommandBuffer.ParallelWriter ecb, Entity unit)
        {
            RegimentSharedData regiment = EntityManager.GetSharedComponentData<RegimentSharedData>(unit);

            Entities
                .WithName("Preselection_Enable_Regiment")
                .WithBurst()
                .WithAll<Tag_Unit>()
                .WithSharedComponentFilter(regiment)
                .ForEach((Entity unitEntity, int entityInQueryIndex) => 
                {
                    ecb.AddComponent<TIsPreselected>(entityInQueryIndex, unitEntity);
                }).ScheduleParallel();
            Entities
                .WithName("Preselection_Enable_Highlight")
                .WithBurst()
                .WithAll<TPreselection, Disabled>()
                .WithSharedComponentFilter(regiment)
                .ForEach((Entity preselectionEntity, int entityInQueryIndex) => 
                {
                    ecb.RemoveComponent<Disabled>(entityInQueryIndex, preselectionEntity);
                }).ScheduleParallel();
        }
        
        //==============================================================================================================
        // DISABLE
        //==============================================================================================================

        private void DisablePreviousPreselection(EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities
                .WithName("Preselection_Disable_Regiment")
                .WithBurst()
                .WithAll<Tag_Unit, TIsPreselected>()
                .ForEach((Entity unitEntity, int entityInQueryIndex) => 
                {
                    ecb.RemoveComponent<TIsPreselected>(entityInQueryIndex, unitEntity);
                }).ScheduleParallel();
            Entities
                .WithName("Preselection_Disable_Highlight")
                .WithBurst()
                .WithAll<TPreselection>()
                .WithNone<Disabled>()
                .ForEach((Entity preselectionEntity, int entityInQueryIndex) => 
                {
                    ecb.AddComponent<Disabled>(entityInQueryIndex, preselectionEntity);
                }).ScheduleParallel();
        }
    }
}