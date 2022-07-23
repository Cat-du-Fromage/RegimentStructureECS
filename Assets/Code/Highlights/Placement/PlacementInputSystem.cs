using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine.InputSystem;

using static Unity.Mathematics.math;
using RaycastHit = Unity.Physics.RaycastHit;
using quaternion = Unity.Mathematics.quaternion;

namespace KaizerWald
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PlacementInputSystem : SystemBase, PlayerControls.IDynamicPlacementActions
    {
        private Entity placementManager;
        
        private EntityQuery regimentsQuery;
        private EntityQuery unitsSelectedQuery;
        //Dynamic Placement
        private EntityQuery enabledPlacementsQuery;
        private EntityQuery disabledPlacementsQuery;
        //Static Placement
        //private EntityQuery staticPlacementsQuery;

        private readonly float screenWidth = Screen.width;
        private readonly float screenHeight = Screen.height;
        
        private Camera playerCamera;
        private Entity cameraInput;

        private Mouse mouse;
        private PlayerControls controls;
        
        private BuildPhysicsWorld buildPhysicsWorld;

        private List<Shared_RegimentEntity> sharedRegiments;

        private bool IsDrag => distancesq(StartMousePosition,EndMousePosition) >= 2;
        //private bool IsDrag => lengthsq(EndMousePosition - StartMousePosition) >= 32;
        private float3 StartMousePosition => GetSingleton<Data_StartPlacement>().Value;
        private float3 EndMousePosition => GetSingleton<Data_EndPlacement>().Value;

        private bool IsTerrainHit => GetSingleton<Check_TerrainHit>().IsValid;
    
        protected override void OnCreate()
        {
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            RequireSingletonForUpdate<Tag_Camera>();
            
            EntityQueryDesc enabledQuery = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(Tag_Placement), typeof(Shared_RegimentEntity), typeof(Translation) },
                None = new ComponentType[] { typeof(DisableRendering) },
                Options = EntityQueryOptions.IncludeDisabled
            };
            enabledPlacementsQuery = EntityManager.CreateEntityQuery(enabledQuery);
            
            EntityQueryDesc disabledQuery = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(Tag_Placement), typeof(Shared_RegimentEntity), typeof(DisableRendering), typeof(Translation) },
                Options = EntityQueryOptions.IncludeDisabled
            };
            disabledPlacementsQuery = EntityManager.CreateEntityQuery(disabledQuery);

            unitsSelectedQuery = EntityManager.CreateEntityQuery(typeof(Tag_Unit), typeof(Shared_RegimentEntity), typeof(Translation));
            regimentsQuery = EntityManager.CreateEntityQuery(typeof(Tag_Regiment));
            
            //staticPlacementsQuery = EntityManager.CreateEntityQuery(typeof(Tag_StaticPlacement), typeof(Shared_RegimentEntity));
            CreateInputManager();
        }

        private void CreateInputManager()
        {
            EntityArchetype placementInputArchetype = EntityManager.CreateArchetype
            (
                typeof(Tag_PlacementManager),
                typeof(Data_StartPlacement),
                typeof(Data_EndPlacement),
                typeof(Check_TerrainHit),
                typeof(Data_LineDirection),
                typeof(Data_ColumnDirection),
                typeof(Data_LookRotation),
                typeof(Filter_PlacementUpdate),
                typeof(Buffer_CachedUnitsPerLine)
                //typeof(Data_TerrainCollisionFilter) //WE CANT SET the filter on the editor this way...
            );
            placementManager = EntityManager.CreateEntity(placementInputArchetype);
            EntityManager.SetName(placementManager, "Placement Manager");
        }
        
        protected override void OnStartRunning()
        {
            cameraInput = GetSingletonEntity<Tag_Camera>();
            playerCamera = EntityManager.GetComponentData<Authoring_PlayerCamera>(cameraInput).Value;
            controls = EntityManager.GetComponentData<Authoring_PlayerControls>(cameraInput).Value;

            if (!controls.DynamicPlacement.enabled)
            {
                controls.DynamicPlacement.Enable();
                controls.DynamicPlacement.SetCallbacks(this);
            }
            
            mouse = Mouse.current;
            
            GetBuffer<Buffer_CachedUnitsPerLine>(placementManager).EnsureCapacity(regimentsQuery.CalculateEntityCount());
        }
        
        protected override void OnUpdate()
        {
            /*
            if (Keyboard.current.rKey.wasReleasedThisFrame)
            {
                NativeArray<Entity> units = GetEntityQuery(typeof(ParticleSystem)).ToEntityArray(Allocator.Temp);
                for (int i = 0; i < units.Length; i++)
                {
                   EntityManager.GetComponentObject<ParticleSystem>(units[i]).Play();
                }
            }
            */
            return;
        }

        public void OnRightMouseClickAndMove(InputAction.CallbackContext context)
        {
            if (mouse.leftButton.isPressed) return;
            float2 mousePosition = context.ReadValue<Vector2>();
            bool isRayCastHit = TerrainRaycast(mousePosition, out RaycastHit hit, 100f);
            float3 hitPosition = hit.Position;
            switch (context.phase)
            {
                case InputActionPhase.Started when isRayCastHit:
                    OnStart(hitPosition);
                    return;
                case InputActionPhase.Performed when IsTerrainHit && isRayCastHit:
                    OnPerformed(hitPosition);
                    return;
                case InputActionPhase.Canceled when IsTerrainHit:
                    //Debug.Log($"Pass: {StartMousePosition}; end: {EndMousePosition}; distsq: {distancesq(EndMousePosition,StartMousePosition)}");
                    RegimentCallback_DestinationsBuffers(); //Send Message
                    OnCancel();
                    return;
            }
        }

        private void OnStart(in float3 hitPosition)
        {
            SetSingleton(new Check_TerrainHit() { IsValid = true });
            SetSingleton(new Data_StartPlacement() { Value = hitPosition });
        }

        private void OnPerformed(in float3 hitPosition)
        {
            SetSingleton(new Data_EndPlacement() { Value = hitPosition });
            SetDirectionalsComponents();
            
            if (EndMousePosition.Equals(StartMousePosition)) return;
            
            EnableSelectedPlacements();
            SetSingleton(new Filter_PlacementUpdate() { DidChange = true });
        }

        private void OnCancel()
        {
            SetNewFormations();
            SetSingleton(new Check_TerrainHit() { IsValid = false });
            DisableAllPlacements();
        }

        private void SetNewFormations()
        {
            DynamicBuffer<Buffer_CachedUnitsPerLine> buffer = GetBuffer<Buffer_CachedUnitsPerLine>(placementManager);
            for (int i = 0; i < buffer.Length; i++)
            {
                Entity regiment = buffer[i].regiment;
                SetComponent(regiment, new UnitsPerLine(){Value = buffer[i].Value});
            }
        }

        /// <summary>
        /// Callback directed to MovementSystem
        /// </summary>
        private void RegimentCallback_DestinationsBuffers()
        {
            if (!IsDrag) return;
            using NativeParallelHashSet<Entity> regimentSelected = new (2, Allocator.TempJob);
            new JGetSelectedRegiments { RegimentsSelected = regimentSelected }.Run();
            
            if (regimentSelected.IsEmpty) return;
            
            using NativeArray<Entity> regiments = regimentSelected.ToNativeArray(Allocator.Temp);
            foreach (Entity regiment in regiments)
            {
                enabledPlacementsQuery.SetSharedComponentFilter(new Shared_RegimentEntity(){Value = regiment});
                NativeArray<Translation> t = enabledPlacementsQuery.ToComponentDataArray<Translation>(Allocator.Temp);
                GetBuffer<Buffer_Destinations>(regiment).CopyFrom(t.Reinterpret<Buffer_Destinations>());
                SetComponent(regiment, GetComponent<Data_LookRotation>(placementManager));
            }
            enabledPlacementsQuery.ResetFilter();
            EntityManager.AddComponent<Tag_Move>(regiments);
            EntityManager.AddComponent<State_NewOrder>(regiments);
        }
        
        private void EnableSelectedPlacements()
        {
            using NativeParallelHashSet<Entity> regimentSelected = new (2, Allocator.TempJob);
            new JGetSelectedRegiments { RegimentsSelected = regimentSelected }.Run();
            foreach (Entity regiment in regimentSelected)
            {
                disabledPlacementsQuery.SetSharedComponentFilter(new Shared_RegimentEntity(){Value = regiment});
                EntityManager.RemoveComponent<DisableRendering>(disabledPlacementsQuery);
                SetComponent(regiment, new Data_RegimentAnimationPlayed(){Value = FusilierClips.Walk});
            }
            disabledPlacementsQuery.ResetFilter();
        }

        private void DisableAllPlacements()
        {
            EntityManager.AddComponent<DisableRendering>(enabledPlacementsQuery);
        }

        public void OnLeftMouse(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            SetSingleton(new Check_TerrainHit() { IsValid = false });
            SetSingleton(new Filter_PlacementUpdate() { DidChange = false });
            DisableAllPlacements();
        }

        public void OnSpaceKey(InputAction.CallbackContext context)
        {
            if (context.performed) return;
            if (context.started)
            {
                NativeArray<Entity> regiments = regimentsQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity regiment in regiments)
                {
                    Shared_RegimentEntity filter = new () { Value = regiment };
                    disabledPlacementsQuery.SetSharedComponentFilter(filter);
                    EntityManager.RemoveComponent<DisableRendering>(disabledPlacementsQuery);
                    if (HasComponent<Tag_Move>(regiment)) continue;
                    enabledPlacementsQuery.SetSharedComponentFilter(filter);
                    unitsSelectedQuery.SetSharedComponentFilter(filter);
                    NativeArray<Translation> positions = unitsSelectedQuery.ToComponentDataArray<Translation>(Allocator.Temp);
                    enabledPlacementsQuery.CopyFromComponentDataArray(positions);
                }
                disabledPlacementsQuery.ResetFilter();
                enabledPlacementsQuery.ResetFilter();
                unitsSelectedQuery.ResetFilter();
            }
            else
            {
                DisableAllPlacements();
            }
        }

        private void SetDirectionalsComponents()
        {
            float3 newLineDirection = normalizesafe(EndMousePosition - StartMousePosition);
            float3 newColumnDirection = normalizesafe(cross(newLineDirection, down()));
            quaternion lookRotation = quaternion.LookRotationSafe(-newColumnDirection, up());
            SetComponent(placementManager, new Data_LineDirection(){Value = newLineDirection});
            SetComponent(placementManager, new Data_ColumnDirection(){Value = newColumnDirection});
            SetComponent(placementManager, new Data_LookRotation(){Value = lookRotation});
        }
        
        //==============================================================================================================
        //Mouses Positions
        private bool TerrainRaycast(in float2 mousePosition, out RaycastHit hit, float distance)
        {
            CollisionWorld world = buildPhysicsWorld.PhysicsWorld.CollisionWorld;
            float3 origin = playerCamera.transform.position;
            float3 direction = playerCamera.ScreenToWorldDirection(mousePosition, screenWidth, screenHeight);
            
            CollisionFilter terrainFilter = GetSingleton<Data_TerrainCollisionFilter>().Value;

            hit = world.Raycast(origin, direction, distance,terrainFilter);
            return hit.Entity != Entity.Null;
        }
        //==============================================================================================================
    }
}