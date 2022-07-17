using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using static Unity.Mathematics.math;

namespace KaizerWald
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(RegimentMouvementSystem))]
    [UpdateBefore(typeof(SingleClipPlayerSystem))]
    public partial class RegimentStateMachineSystem : SystemBase
    {
        private EntityQuery regimentsWithOrderQuery;
        private EntityQuery unitsWithOrderQuery;
        protected override void OnCreate()
        {
            regimentsWithOrderQuery = EntityManager.CreateEntityQuery(typeof(Tag_Regiment), typeof(State_NewOrder));
            unitsWithOrderQuery = EntityManager.CreateEntityQuery(typeof(Tag_Unit), typeof(Shared_RegimentEntity));
            RequireForUpdate(regimentsWithOrderQuery);
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> regiments = regimentsWithOrderQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity regiment in regiments)
            {
                unitsWithOrderQuery.SetSharedComponentFilter(new Shared_RegimentEntity(){Value = regiment});
                FusilierClips animationIndex = GetComponent<Data_RegimentAnimationPlayed>(regiment).Value;
                JSetUnitsAnimation job = new JSetUnitsAnimation()
                {
                    Animation = (int)animationIndex
                };
                job.ScheduleParallel(unitsWithOrderQuery);
            }
            EntityManager.RemoveComponent<State_NewOrder>(regimentsWithOrderQuery);
        }
    }
    
    [BurstCompile(CompileSynchronously = true)]
    [WithAll(typeof(Tag_Unit))]
    public partial struct JSetUnitsAnimation : IJobEntity
    {
        [ReadOnly] public int Animation;
        
        public void Execute(ref Data_AnimationPlayed animationToPlay)
        {
            animationToPlay.Value = (FusilierClips)Animation;
        }
    }
}