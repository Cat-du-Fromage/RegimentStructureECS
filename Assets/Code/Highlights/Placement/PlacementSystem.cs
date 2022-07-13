using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace KaizerWald
{
    public partial class PlacementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            return;
            Entities.ForEach((ref Translation translation, in Rotation rotation) => 
            {
            
            }).Schedule();
        }
    }
}