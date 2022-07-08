using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GroupPreselectionSystem : SystemBase, PlayerControls.IGroupPreselectionActions
    {
        private Camera playerCamera;
        private Entity cameraInput;
        private PlayerControls controls;

        protected override void OnStartRunning()
        {
            cameraInput = GetSingletonEntity<Tag_Camera>();
            playerCamera = EntityManager.GetComponentData<Authoring_PlayerCamera>(cameraInput).Value;
            controls = EntityManager.GetComponentData<Authoring_PlayerControls>(cameraInput).Value;
            if (!controls.GroupPreselection.enabled)
            {
                controls.GroupPreselection.Enable();
                controls.GroupPreselection.SetCallbacks(this);
            }
            
            RequireSingletonForUpdate<Tag_Camera>();
        }
        
        protected override void OnUpdate()
        {
            return;
        }

        private bool IsDragSelection() =>
            lengthsq(GetSingleton<Data_EndMousePosition>().Value - GetSingleton<Data_StartMousePosition>().Value) >=
            128;

        public void OnLeftMouseClickAndMove(InputAction.CallbackContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Started:
                    float2 mousePosition = context.ReadValue<Vector2>();
                    SetSingleton(new Data_StartMousePosition() { Value = mousePosition });
                    SetSingleton(new Data_ClickDrag() { Value = false });
                    return;
                case InputActionPhase.Performed:
                    SetSingleton(new Data_EndMousePosition(){Value = context.ReadValue<Vector2>()});
                    bool isDrag = IsDragSelection();
                    SetSingleton(new Data_ClickDrag() { Value = isDrag });
                    return;
                case InputActionPhase.Canceled:
                    SetSingleton(new Data_ClickDrag() { Value = false });
                    SetSingleton(new Data_StartMousePosition() { Value = float2.zero });
                    SetSingleton(new Data_EndMousePosition() {Value = float2.zero });
                    return;
                default:
                    return;
            }
        }
    }
}