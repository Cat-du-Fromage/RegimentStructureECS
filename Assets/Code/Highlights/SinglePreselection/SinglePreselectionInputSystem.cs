using System;
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

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SinglePreselectionInputSystem : SystemBase, PlayerControls.ISinglePreselectionActions
    {
        private const float Radius = 0.3f;
        private const float DistanceCast = 100f;
        
        private readonly float screenWidth = Screen.width;
        private readonly float screenHeight = Screen.height;
        
        private Camera playerCamera;
        private PlayerControls controls;

        private BuildPhysicsWorld buildPhysicsWorld;
        
        private Entity previousRegimentHit;
        private NativeReference<Entity> regimentHit;

        protected override void OnCreate()
        {
            buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
            regimentHit = new NativeReference<Entity>(Allocator.Persistent);
        }

        protected override void OnStartRunning()
        {
            Entity cameraEntity = GetSingletonEntity<Tag_Camera>();
            playerCamera = EntityManager.GetComponentData<Authoring_PlayerCamera>(cameraEntity).Value;
            controls = EntityManager.GetComponentData<Authoring_PlayerControls>(cameraEntity).Value;
            
            if (!controls.SinglePreselection.enabled)
            {
                controls.SinglePreselection.Enable();
                controls.SinglePreselection.SetCallbacks(this);
            }
            RequireSingletonForUpdate<Tag_Camera>();
        }

        protected override void OnUpdate()
        {
            if (GetSingleton<Data_ClickDrag>().Value) return;
            if (previousRegimentHit == regimentHit.Value) return;
            if (regimentHit.Value != Entity.Null)
            {
                SetComponent(regimentHit.Value, new Flag_Preselection(){IsActive = true});
                SetComponent(regimentHit.Value, new Fitler_Preselection(){DidChange = true});
                //EntityManager.SetComponentData(regimentHit.Value, new Flag_Preselection(){IsActive = true});
            }
            if (previousRegimentHit != Entity.Null)
            {
                SetComponent(previousRegimentHit, new Flag_Preselection(){IsActive = false});
                SetComponent(previousRegimentHit, new Fitler_Preselection(){DidChange = true});
                //EntityManager.SetComponentData(previousRegimentHit, new Flag_Preselection(){IsActive = false});
            }
            previousRegimentHit = regimentHit.Value;
        }

        protected override void OnDestroy()
        {
            if (regimentHit.IsCreated) regimentHit.Dispose();
            controls.SinglePreselection.Disable();
        }

        protected override void OnStopRunning()
        {
            if (regimentHit.IsCreated) regimentHit.Dispose();
        }

        public void OnMouseMove(InputAction.CallbackContext context)
        {
            if (GetSingleton<Data_ClickDrag>().Value) return;
            
            float2 mousePosition = context.ReadValue<Vector2>();
            regimentHit.Value = ScheduleSinglePreselection(mousePosition);
        }
        
        private Entity ScheduleSinglePreselection(in float2 mousePosition)
        {
            CollisionWorld world = buildPhysicsWorld.PhysicsWorld.CollisionWorld;
            float3 origin = playerCamera.transform.position;
            float3 direction = playerCamera.ScreenToWorldDirection(mousePosition, screenWidth, screenHeight);
            CollisionFilter unitFilter = GetSingleton<Data_UnitCollisionFilter>().Value;
            
            JobHandle jobHandle = world.SingleSphereCast(origin, Radius, direction, regimentHit, DistanceCast, unitFilter, Dependency);
            jobHandle.Complete();
            
            if (regimentHit.Value == Entity.Null) return Entity.Null;
            Entity regiment = EntityManager.GetSharedComponentData<RegimentSharedData>(regimentHit.Value).Regiment;
            return previousRegimentHit == regiment ? previousRegimentHit : regiment;
        }
        
    }
}