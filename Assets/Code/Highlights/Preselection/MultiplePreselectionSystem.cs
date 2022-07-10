using System.Collections.Generic;
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
    [UpdateAfter(typeof(CameraRaySystem))]
    public partial class MultiplePreselectionSystem : SystemBase
    {
        private Entity CameraRaySingleton;
        private EntityQuery regimentQuery;
        
        private EntityQuery preselectedUnitQuery;
        private EntityQuery unPreselectedUnitQuery;
        
        Camera playerCamera;
        
        protected override void OnCreate()
        {
            Enabled = false;
            regimentQuery = GetEntityQuery(ComponentType.ReadOnly<Tag_Regiment>());
            
            preselectedUnitQuery = GetEntityQuery(ComponentType.ReadOnly<Tag_Unit>(), ComponentType.ReadOnly<TIsPreselected>());
            
            EntityQueryDesc unPreselectQueryDesc = new EntityQueryDesc()
            {
                None = new ComponentType[] { typeof(TIsPreselected) },
                All = new [] { ComponentType.ReadOnly<Tag_Unit>() }
            };
            unPreselectedUnitQuery = GetEntityQuery(unPreselectQueryDesc);
        }

        protected override void OnStartRunning()
        {
            CameraRaySingleton = GetSingletonEntity<SingletonCameraRayHit>();
            RequireSingletonForUpdate<SingletonCameraRayHit>();
            playerCamera = Camera.main;
        }
        
        protected override void OnUpdate()
        {
            return;
            if (!GetComponent<IsClickDragPerformed>(CameraRaySingleton).Value) return;

            AABB selectionRectangle = GetComponent<SelectionBounds>(CameraRaySingleton).Value.ToAABB();

            int numRegiments = regimentQuery.CalculateEntityCount() * 2;
            NativeParallelHashSet<Entity> regiments = new (numRegiments, Allocator.TempJob);
            
            JGetPreselectedRegiment getPreselectedRegiment = new JGetPreselectedRegiment
            {
                SelectionRectangle = selectionRectangle,
                WorldToCameraMatrix = playerCamera.worldToCameraMatrix,
                ProjectionMatrix = playerCamera.projectionMatrix,
                Regiments = regiments.AsParallelWriter(),
            };
            getPreselectedRegiment.ScheduleParallel(unPreselectedUnitQuery, Dependency).Complete();

            Debug.Log(regiments.Count());
            regiments.Dispose();
        }
        
        [BurstCompile(CompileSynchronously = true)]
        private partial struct JGetPreselectedRegiment : IJobEntity
        {
            [ReadOnly] public AABB SelectionRectangle;
            [ReadOnly] public float4x4 WorldToCameraMatrix;
            [ReadOnly] public float4x4 ProjectionMatrix;
            [WriteOnly] public NativeParallelHashSet<Entity>.ParallelWriter Regiments;
            
            public void Execute(in LocalToWorld position, in RegimentBelong regiment)
            {
                float3 unitPositionInRect = position.Position.WorldToViewportPoint(WorldToCameraMatrix, ProjectionMatrix);
                
                if (SelectionRectangle.Contains(unitPositionInRect))
                {
                    Regiments.Add(regiment.Regiment);
                }
            }
        }
        
    }
}