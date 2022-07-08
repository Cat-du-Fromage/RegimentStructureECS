using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(CameraRaySystem))]
    [UpdateAfter(typeof(PreselectionSystem))]
    public partial class SelectionSystem : SystemBase
    {
        private const int SelectionIndex = 2;
        
        private ButtonControl leftMouseClick;
        private KeyControl leftShift;
        
        private BeginInitializationEntityCommandBufferSystem beginInitEcb;
        
        protected override void OnCreate()
        {
            beginInitEcb = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnStartRunning()
        {
            leftMouseClick = Mouse.current.leftButton;
            leftShift = Keyboard.current.leftShiftKey;
        }

        protected override void OnUpdate()
        {
            if (!leftMouseClick.wasReleasedThisFrame) return;
            EntityCommandBuffer.ParallelWriter ecb = beginInitEcb.CreateCommandBuffer().AsParallelWriter();
            if (!leftShift.isPressed)
            {
                DisableSelection(ecb);
            }
            EnableSelection(ecb);
            beginInitEcb.AddJobHandleForProducer(Dependency);
        }
        
        //==============================================================================================================
        // ENABLE
        //==============================================================================================================
        private void EnableSelection(EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities
                .WithName("Selection_Enable_Regiment")
                .WithBurst()
                .WithAll<Tag_Unit, TIsPreselected>()
                .WithNone<TIsSelected>()
                .ForEach((Entity unitEntity, int entityInQueryIndex, in DynamicBuffer<LinkedEntityGroup> children) => 
                {
                    ecb.AddComponent<TIsSelected>(entityInQueryIndex, unitEntity);
                    ecb.RemoveComponent<Disabled>(entityInQueryIndex, children[SelectionIndex].Value);
                }).ScheduleParallel();
        }
        
        //==============================================================================================================
        // DISABLE
        //==============================================================================================================
        private void DisableSelection(EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities
                .WithName("Selection_Disable_Regiment")
                .WithBurst()
                .WithAll<Tag_Unit, TIsSelected>()
                .WithNone<TIsPreselected>()
                .ForEach((Entity unitEntity, int entityInQueryIndex, in DynamicBuffer<LinkedEntityGroup> children) => 
                {
                    ecb.RemoveComponent<TIsSelected>(entityInQueryIndex, unitEntity);
                    ecb.AddComponent<Disabled>(entityInQueryIndex, children[SelectionIndex].Value);
                }).ScheduleParallel();
        }
    }
}