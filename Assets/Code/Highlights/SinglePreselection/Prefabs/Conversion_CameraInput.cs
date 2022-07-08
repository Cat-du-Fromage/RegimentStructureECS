using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace KaizerWald
{
    [DisallowMultipleComponent]
    public class Conversion_CameraInput : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private int UnitLayerMask;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            EntityArchetype cameraInputArchetype = dstManager.CreateArchetype
            (
                typeof(Authoring_PlayerCamera),
                typeof(Authoring_PlayerControls),
                typeof(Tag_Camera),
                typeof(Data_ClickDrag),
                typeof(Data_StartMousePosition),
                typeof(Data_EndMousePosition),
                typeof(Data_UnitCollisionFilter)
            );
            dstManager.SetArchetype(entity, cameraInputArchetype);
            dstManager.SetName(entity, "Singleton_CameraInput");
            dstManager.SetComponentData(entity, new Authoring_PlayerCamera() { Value = playerCamera = playerCamera == null ? Camera.main : playerCamera });
            dstManager.SetComponentData(entity, new Authoring_PlayerControls() { Value = new PlayerControls() });

            dstManager.SetComponentData(entity, new Data_UnitCollisionFilter() { Value = collisionFilter });
        }
        
        private CollisionFilter collisionFilter => new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1u << UnitLayerMask, GroupIndex = 0 };
    }
}