using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace KaizerWald
{
    [BurstCompile(CompileSynchronously = true)]
    [WithAll(typeof(Tag_Regiment), typeof(Flag_Selection))]
    public partial struct JGetSelectedRegiments : IJobEntity
    {
        [WriteOnly] public NativeParallelHashSet<Entity> RegimentsSelected;
        public void Execute(Entity regiment, in Flag_Selection selection)
        {
            if (!selection.IsActive) return;
            RegimentsSelected.Add(regiment);
        }
    }
}