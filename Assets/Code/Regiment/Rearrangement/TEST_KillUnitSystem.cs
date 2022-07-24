using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using KWUtils;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using static KWUtils.KWmath;
using static Unity.Mathematics.math;
using static Unity.Mathematics.float3;
using static Unity.Mathematics.quaternion;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class TEST_KillUnitSystem : SystemBase
    {
        private EntityQuery regimentQ;
        private BeginInitializationEntityCommandBufferSystem beginInitSystem;
        
        private readonly float screenWidth = Screen.width;
        private readonly float screenHeight = Screen.height;
        
        private Camera playerCamera;
        private BuildPhysicsWorld buildPhysicsWorld;
        
        private NativeReference<Entity> unitHit;

        private Mouse mouse;
        protected override void OnCreate()
        {
            beginInitSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            unitHit = new NativeReference<Entity>(Allocator.Persistent);
            regimentQ = GetEntityQuery(typeof(Tag_Regiment));
        }
        
        protected override void OnStartRunning()
        {
            Entity cameraEntity = GetSingletonEntity<Tag_Camera>();
            playerCamera = EntityManager.GetComponentData<Authoring_PlayerCamera>(cameraEntity).Value;
            mouse = Mouse.current;
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }

        protected override void OnUpdate()
        {
            Kill3Units();
            KillUnitTest();
        }

        private void Kill3Units()
        {
            if (regimentQ.IsEmpty) return;
            if (!Keyboard.current.kKey.wasPressedThisFrame) return;
            using NativeArray<Entity> regiments = regimentQ.ToEntityArray(Allocator.Temp);
            Entity regiment = regiments[0];
            Debug.Log($"kill: {regiment.Index}");
            int numUnits = GetComponent<NumberUnits>(regiment).Value;
            if (numUnits < 3) return;

            NativeArray<Entity> unitsToDestroy = new NativeArray<Entity>(3, Allocator.Temp);
            DynamicBuffer<Buffer_Units> units = GetBuffer<Buffer_Units>(regiment);
            for (int i = 0; i < 3; i++)
            {
                unitsToDestroy[i] = units[i].Value;
            }
            
            EntityCommandBuffer ecb = beginInitSystem.CreateCommandBuffer();
            ecb.DestroyEntity(unitsToDestroy);
            beginInitSystem.AddJobHandleForProducer(Dependency);
        }

        private void KillUnitTest()
        {
            //ATTENTION SI ON BOUGE LA SOURIS PENDANT LE KILL, ERROR LIE A LA PRESELECTION
            if (!Keyboard.current.leftShiftKey.isPressed || !mouse.leftButton.wasReleasedThisFrame) return;
            if (ScheduleSingleRaycast(mouse.position.ReadValue()) == Entity.Null) return;
            EntityCommandBuffer ecb = beginInitSystem.CreateCommandBuffer();
            
            //Entity regiment = GetComponent<Data_Regiment>(unitHit.Value).Value;
            //int numUnit = GetComponent<NumberUnits>(regiment).Value;
            //SetComponent(regiment, new NumberUnits(){Value = numUnit-1});
            
            ecb.DestroyEntity(unitHit.Value);
            beginInitSystem.AddJobHandleForProducer(Dependency);
        }
        protected override void OnStopRunning()
        {
            if (unitHit.IsCreated) unitHit.Dispose();
        }
        protected override void OnDestroy()
        {
            if (unitHit.IsCreated) unitHit.Dispose();
        }
        
        private Entity ScheduleSingleRaycast(in float2 mousePosition)
        {
            CollisionWorld world = buildPhysicsWorld.PhysicsWorld.CollisionWorld;
            float3 origin = playerCamera.transform.position;
            float3 direction = playerCamera.ScreenToWorldDirection(mousePosition, screenWidth, screenHeight);

            CollisionFilter unitFilter = GetSingleton<Data_UnitCollisionFilter>().Value;
            
            world.SingleSphereCast(origin, 0.3f, direction, unitHit, 100f, unitFilter, Dependency).Complete();
            return unitHit.Value;
        }
    }
}