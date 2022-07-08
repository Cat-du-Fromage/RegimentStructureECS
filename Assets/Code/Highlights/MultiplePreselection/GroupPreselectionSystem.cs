using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
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
        private EntityQuery query;
        
        private Camera playerCamera;
        private Entity cameraInput;
        private PlayerControls controls;
        
        private int pixelWidth;
        private int pixelHeight;
        
        private bool IsDragSelection => lengthsq(EndMousePosition - StartMousePosition) >= 128;
        private float2 StartMousePosition => GetSingleton<Data_StartMousePosition>().Value;
        private float2 EndMousePosition => GetSingleton<Data_EndMousePosition>().Value;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Tag_Camera>();
        }

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

            pixelWidth = playerCamera.pixelWidth;
            pixelWidth = playerCamera.pixelHeight;
        }
        
        protected override void OnUpdate()
        {
            AABB selectionBounds = GetViewportBounds(StartMousePosition, EndMousePosition, playerCamera.nearClipPlane, playerCamera.farClipPlane);
            
            return;
        }
        
        [BurstCompile(CompileSynchronously = true)]
        [WithAll(typeof(Tag_Unit))]
        private partial struct JGetPreselection : IJobEntity
        {
            [ReadOnly] public AABB BoundsAABB;
            [ReadOnly] public float4x4 WorldToCameraMatrix;
            [ReadOnly] public float4x4 ProjectionMatrix;
            private void Execute(in LocalToWorld ltw)
            {
                float3 unitPositionInRect = ltw.Position.WorldToViewportPoint(WorldToCameraMatrix, ProjectionMatrix);
                bool test = BoundsAABB.Contains(unitPositionInRect);
                
                
            }
        }
        
        private AABB GetViewportBounds(in float2 startPoint, in float2 endPoint, float nearClipPlane, float farClipPlane)
        {
            float3 start = new (startPoint.ScreenToViewportPoint(pixelWidth, pixelHeight),0);
            float3 end = new (endPoint.ScreenToViewportPoint(pixelWidth, pixelHeight),0);
            float3 min = math.min(start, end);
            float3 max = math.max(start, end);
            
            min.z = nearClipPlane;
            max.z = farClipPlane;

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds.ToAABB();
        }
/*
        private float2 ScreenToViewport(float2 point, int pixelWidth, int pixelHeight)
        {
            float x = point.x / pixelWidth; //given by camera.pixelWidth
            float y = point.y / pixelHeight; //given by camera.pixelHeight
            return new float2(x,y);
        }
*/
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
                    bool isDrag = IsDragSelection;
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