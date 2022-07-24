using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KaizerWald
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ShootSystem : SystemBase
    {
        private EntityQuery regimentQuery;
        private EntityQuery unitsQuery;

        protected override void OnCreate()
        {
            regimentQuery = EntityManager.CreateEntityQuery(typeof(Tag_Regiment));
            unitsQuery = EntityManager.CreateEntityQuery(typeof(Tag_Unit), typeof(Shared_RegimentEntity));
        }
        
        protected override void OnUpdate()
        {
            PlayMuzzleFlash();
        }
        
        private void PlayMuzzleFlash()
        {
            if (!Keyboard.current.rKey.wasReleasedThisFrame) return;
            NativeArray<Entity> regiments = regimentQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < regiments.Length; i++)
            {
                Entity regiment = regiments[i];
                DynamicBuffer<Buffer_Units> units = GetBuffer<Buffer_Units>(regiment);
                int unitsPerLine = GetComponent<UnitsPerLine>(regiment).Value;
               
                for (int j = 0; j < unitsPerLine; j++)
                {
                    Entity muzzle = GetBuffer<LinkedEntityGroup>(units[j].Value)[4].Value;
                    DynamicBuffer<Child> subParticles = GetBuffer<Child>(muzzle);
                    for (int k = 0; k < subParticles.Length; k++)
                    {
                        EntityManager.GetComponentObject<ParticleSystem>(subParticles[k].Value).Play();
                    }
                }
            }
        }
    }
}