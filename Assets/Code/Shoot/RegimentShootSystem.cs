using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = UnityEngine.RaycastHit;

namespace KaizerWald
{
    public partial class RegimentShootSystem : SystemBase
    {
        private EntityQuery regimentIdleQuery;
        
        private JobHandle targetHandle;
        private NativeArray<RaycastHit> results;
        private NativeArray<SpherecastCommand> commands;
        
        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            return;
        }

        private bool TestLayer()
        {
            bool result = false;

            CollisionFilter filter = new CollisionFilter
            {
                BelongsTo = (1<<1 | 1<<2) & ~(1<<1 | 1<<3),
                CollidesWith = 0,
                GroupIndex = 0
            };

            return result;
        }

        private void GetRegimentTarget()
        {
            NativeArray<Entity> regimentsIdle = regimentIdleQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < regimentsIdle.Length; i++)
            {
                Entity regiment = regimentsIdle[i];
                int numUnitPerLine = GetComponent<Data_UnitsPerLine>(regiment).Value;

                JSphereCast job = new JSphereCast
                {
                    radius = 0.2f,
                    distance = 100f,
                    origin = default,
                    direction = default,
                    filter = default,
                    world = default,
                    results = default
                };

                for (int unitIndex = 0; unitIndex < numUnitPerLine; unitIndex++)
                {
                    
                }
            }
        }

        /*
        private void GetTarget()
        {
            regimentIdleQuery.ToEntityArray(Allocator.Temp);
            
            float3 offset = new float3(0, 0.5f, 0);

            List<int> searcher = new List<int>(regiment.CurrentLineFormation);
            
            for (int i = 0; i < regiment.CurrentLineFormation; i++)
            {
                if (regiment.Units[i].IsDead) continue;
                searcher.Add(i);
            }
                
            results = new (searcher.Count, Allocator.TempJob);
            commands = new (searcher.Count, Allocator.TempJob);

            for (int i = 0; i < searcher.Count; i++)
            {
                int unitIndex = searcher[i];
                float3 direction = regiment.UnitsTransform[unitIndex].forward;
                float3 origin = regiment.UnitsTransform[unitIndex].position + offset + direction;

                commands[i] = new SpherecastCommand(origin, 2f,direction, 20f,HitMask);
            }
            targetHandle = SpherecastCommand.ScheduleBatch(commands, results, regiment.CurrentLineFormation);
        }
        */
    }
}
