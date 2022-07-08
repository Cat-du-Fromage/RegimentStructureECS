using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

using static Unity.Mathematics.math;
using static Unity.Mathematics.float2;
using RaycastHit = Unity.Physics.RaycastHit;
using static KaizerWald.CameraUtils;
using static KaizerWald.RaycastUtils;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class CameraRaySystem : SystemBase
    {
        private const int UnitLayerMask = 1;
        
        private const float Radius = 0.3f;
        private const float DistanceCast = 100f;
        
        private readonly float screenWidth = Screen.width;
        private readonly float screenHeight = Screen.height;

        private Entity singleton;
        
        private Camera playerCamera;
        private Transform cameraTransform;
        
        private Mouse mouse;
        float2 previousPosition;
        
        private BuildPhysicsWorld buildPhysicsWorld;
        private CollisionFilter unitFilter;

        protected override void OnCreate()
        {

            EntityArchetype singletonArchetype = EntityManager.CreateArchetype
            (
                typeof(SingletonCameraRayHit),
                typeof(IsClickDragPerformed),
                typeof(StartLeftMousePosition),
                typeof(EndLeftMousePosition),
                typeof(SelectionBounds)
            );
            singleton = EntityManager.CreateEntity(singletonArchetype);
            
            EntityManager.SetName(singleton, "CameraRayEntity");
            
            buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();

            unitFilter = new CollisionFilter
            {
                BelongsTo = ~0u, //belongs to all layers
                CollidesWith = 1u << UnitLayerMask, //collides with all layers
                GroupIndex = 0,
            };
        }

        protected override void OnStartRunning()
        {
            playerCamera = Camera.main;
            cameraTransform = playerCamera.transform;
            
            mouse = Mouse.current;
            previousPosition = mouse.position.ReadValue();

            RequireSingletonForUpdate<SingletonCameraRayHit>();
        }

        protected override void OnUpdate()
        {
            float2 currentMousePosition = mouse.position.ReadValue();
            
            float2 start = OnLeftMousePressed(currentMousePosition);
            bool isDrag = IsDragSelection(currentMousePosition, out float2 end);
            OnLeftMouseReleased();
            
            if(!isDrag)
                SingleRayCheck(currentMousePosition);
            else
                SetBounds(currentMousePosition,float3(start,0), float3(end,0));

            //if (previousPosition.Equals(currentMousePosition)) return;
            //previousPosition = currentMousePosition;
        }
        
        private void SetBounds(in float2 mousePosition, in Vector3 start, in Vector3 end)
        {
            if (EarlyExit(mousePosition)) return;
            Bounds selectionBounds = GetViewportBounds(playerCamera, start, end);
            SetComponent(singleton, new SelectionBounds(){Value = selectionBounds});
        }

        private float2 OnLeftMousePressed(in float2 currentMousePosition)
        {
            if (!mouse.leftButton.wasPressedThisFrame) return zero;
            SetComponent(singleton, new IsClickDragPerformed() {Value = false});
            SetComponent(singleton, new StartLeftMousePosition() {Value = currentMousePosition});
            SetComponent(singleton, new EndLeftMousePosition() {Value = currentMousePosition});
            return currentMousePosition;
        }

        private void OnLeftMouseReleased()
        {
            if(!mouse.leftButton.wasReleasedThisFrame) return;
            SetComponent(singleton, new IsClickDragPerformed() {Value = false});
            SetComponent(singleton, new StartLeftMousePosition() {Value = zero});
            SetComponent(singleton, new EndLeftMousePosition() {Value = zero});
            SetComponent(singleton, new SelectionBounds(){Value = new Bounds()});
        }

        private bool IsDragSelection(in float2 currentMousePosition, out float2 end)
        {
            end = zero;
            if (!mouse.leftButton.isPressed) return false;
            float2 start = GetComponent<StartLeftMousePosition>(singleton).Value;
            bool previousIsDragSelection = GetComponent<IsClickDragPerformed>(singleton).Value;
            float lengthDrag = lengthsq(currentMousePosition - start);

            bool isDragSelection = lengthDrag >= 128;

            if (isDragSelection)
            {
                SetComponent(singleton, new EndLeftMousePosition() {Value = currentMousePosition});
                if(!previousIsDragSelection)
                    SetComponent(singleton, new IsClickDragPerformed() { Value = true });
            }
            else if(previousIsDragSelection) 
                SetComponent(singleton, new IsClickDragPerformed() { Value = false });

            end = currentMousePosition;
            return isDragSelection;
        }

        
        
        private void SingleRayCheck(in float2 mousePosition)
        {
            Entity unit = GetUnitHit(mousePosition);
            Entity regiment = GetRegimentHit(unit);
            if(GetSingleton<SingletonCameraRayHit>().RegimentHit == regiment) return;
            SetSingleton(new SingletonCameraRayHit(){UnitHit = unit, RegimentHit = regiment});
        }
        
        /// <summary>
        /// Get Unit from camera raycast
        /// </summary>
        /// <param name="mousePosition">current mouse position (Mouse.current.position.ReadValue())</param>
        /// <returns>unit entity hit by raycast</returns>
        private Entity GetUnitHit(in float2 mousePosition)
        {
            float3 origin = cameraTransform.position;
            float3 direction = playerCamera.ScreenToWorldDirection(mousePosition, screenWidth, screenHeight);
            return buildPhysicsWorld.PhysicsWorld.CollisionWorld.SphereCastAllClosest(origin, direction, Radius, DistanceCast, unitFilter);
        }

        /// <summary>
        /// Get regiment from unity shared component
        /// </summary>
        /// <param name="unit"></param>
        /// <returns>regiment unit belongs to</returns>
        private Entity GetRegimentHit(in Entity unit)
        {
            return unit == Entity.Null ? Entity.Null : EntityManager.GetSharedComponentData<RegimentSharedData>(unit).Regiment;
        }
        
        private bool EarlyExit(in float2 currentMousePosition)
        {
            //Vector2 currentMousePosition = mouse.position.ReadValue();
            if (previousPosition.Equals(currentMousePosition)) return true;
            previousPosition = currentMousePosition;
            return false;
        }
        
        private Bounds GetViewportBounds(Camera camera, Vector3 startPoint, Vector3 endPoint)
        {
            Vector3 start = camera.ScreenToViewportPoint(startPoint);
            Vector3 end = camera.ScreenToViewportPoint(endPoint);
            Vector3 min = Vector3.Min(start, end);
            Vector3 max = Vector3.Max(start, end);
            min.z = camera.nearClipPlane;
            max.z = camera.farClipPlane;

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }
    }
}