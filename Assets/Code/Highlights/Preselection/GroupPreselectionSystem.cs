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
    /// <summary>
    /// MUST NOT RUN IF: RIGHT CLICK ENABLE
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GroupPreselectionSystem : SystemBase, PlayerControls.IGroupPreselectionActions
    {
        private EntityQuery unitQuery;
        private EntityQuery regimentQuery;
        
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
            regimentQuery = GetEntityQuery(ComponentType.ReadOnly<Tag_Regiment>());
            unitQuery = GetEntityQuery(ComponentType.ReadOnly<Tag_Unit>());
            
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
            pixelHeight = playerCamera.pixelHeight;
        }
        
        protected override void OnUpdate()
        {
            if (!GetSingleton<Data_ClickDrag>().Value)
            {
                if (!Mouse.current.leftButton.wasReleasedThisFrame) return;
                DisableAllPreselection();
            }
            
            int numUnit = unitQuery.CalculateEntityCount();
            NativeParallelHashSet<Entity> regimentsPreselected = new (numUnit, Allocator.TempJob);
            AABB selectionBounds = GetScreenToViewportAABB(StartMousePosition, EndMousePosition, playerCamera.nearClipPlane, playerCamera.farClipPlane);

            JGetPreselection job = new JGetPreselection
            {
                BoundsAABB = selectionBounds,
                WorldToCameraMatrix = playerCamera.worldToCameraMatrix,
                ProjectionMatrix = playerCamera.projectionMatrix,
                RegimentsPreselected = regimentsPreselected.AsParallelWriter()
            };
            JobHandle jobHandle = job.ScheduleParallel(unitQuery, Dependency);
            jobHandle.Complete();
            
            MakeRegimentPreselectionSort(regimentsPreselected);
            regimentsPreselected.Dispose();
        }

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

        private void DisableAllPreselection()
        {
            Entities
            .WithName("Preselection_OnLeftMouse_Released")
            .WithoutBurst()
            .WithAll<Tag_Regiment>()
            .ForEach((Entity regiment, ref Flag_Preselection flag) =>
            {
                if (!flag.IsActive) return;
                flag.IsActive = false;
                SetComponent(regiment, new Filter_Preselection(){DidChange = true});
            }).Run();
        }
        
        private void MakeRegimentPreselectionSort(NativeParallelHashSet<Entity> regimentsPreselected)
        {
            Entities
            .WithName("Preselection_Sort_Regiment")
            .WithoutBurst()
            .WithAll<Tag_Regiment>()
            .ForEach((Entity regiment, ref Flag_Preselection flag) => 
            {
                if (regimentsPreselected.Contains(regiment))
                {
                    if (flag.IsActive) return;
                    flag.IsActive = true;
                }
                else
                {
                    if (!flag.IsActive) return;
                    flag.IsActive = false;
                }
                SetComponent(regiment, new Filter_Preselection(){DidChange = true});
            }).Run();
        }
        
        private AABB GetScreenToViewportAABB(in float2 startPoint, in float2 endPoint, float nearClipPlane, float farClipPlane)
        {
            float2 start = startPoint.ScreenToViewportPoint(pixelWidth, pixelHeight);
            float2 end = endPoint.ScreenToViewportPoint(pixelWidth, pixelHeight);
            
            float3 minBound = new (min(start, end), nearClipPlane);
            float3 maxBound = new (max(start, end), farClipPlane);

            Bounds bounds = new Bounds();
            bounds.SetMinMax(minBound, maxBound);
            return bounds.ToAABB();
        }

        [BurstCompile(CompileSynchronously = true)]
        [WithAll(typeof(Tag_Unit))]
        private partial struct JGetPreselection : IJobEntity
        {
            [ReadOnly] public AABB BoundsAABB;
            [ReadOnly] public float4x4 WorldToCameraMatrix;
            [ReadOnly] public float4x4 ProjectionMatrix;

            [WriteOnly, NativeDisableParallelForRestriction] public NativeParallelHashSet<Entity>.ParallelWriter RegimentsPreselected;
            private void Execute(in Data_Regiment dataRegiment, in Translation position)
            {
                float3 unitPositionInRect = position.Value.WorldToViewportPoint(WorldToCameraMatrix, ProjectionMatrix);
                if (!BoundsAABB.Contains(unitPositionInRect)) return;
                RegimentsPreselected.Add(dataRegiment.Value);
            }
        }
    }
}