using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using static Unity.Mathematics.float4x4;

namespace KaizerWald
{
    public readonly struct ScreenData
    {
        public readonly float ScreenWidth;
        public readonly float ScreenHeight;
        public readonly float2 ScreenPosition;

        public ScreenData(float2 screenPosition)
        {
            ScreenPosition = screenPosition;
            ScreenWidth = Screen.width;
            ScreenHeight = Screen.height;
        }
    }

    public readonly struct CameraScreenToWorldData
    {
        public readonly float4x4 CameraToWorld;
        public readonly float4x4 CameraProjectionInverse;

        public CameraScreenToWorldData(Camera camera)
        {
            CameraToWorld = camera.cameraToWorldMatrix;
            CameraProjectionInverse = inverse(camera.projectionMatrix);
        }
    }
    
    public static class ScreenToWorldUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="screenPosition">new float2 (mousePos.x, mousePos.y);</param>
        /// <param name="screenWidth">Screen.width</param>
        /// <param name="screenHeight">Screen.height;</param>
        /// <param name="cameraToWorld">camera.cameraToWorldMatrix;</param>
        /// <param name="cameraProjectionInverse">math.inverse (camera.projectionMatrix);</param>
        /// <param name="cameraOrigin">camera.transform.position;</param>
        /// <returns></returns>
        public static float3 ScreenToWorldRay (
            float2 screenPosition,
            float screenWidth,
            float screenHeight,
            float4x4 cameraToWorld,
            float4x4 cameraProjectionInverse,
            float3 cameraOrigin)
        {
            screenPosition = new float2(screenPosition.x, screenHeight - screenPosition.y);
     
            float4 clipSpace = new float4(((screenPosition.x * 2.0f) / screenWidth) - 1.0f, (1.0f - (2.0f * screenPosition.y) / screenHeight), 0.0f, 1.0f);
     
            float4 viewSpace = mul(cameraProjectionInverse, clipSpace);
            viewSpace /= viewSpace.w;
     
            float4 worldSpace = mul(cameraToWorld, viewSpace);
     
            float3 worldDirection = normalize(worldSpace.xyz - cameraOrigin);
     
            return worldDirection;
        }
                
        private static float3 ScreenToWorldRay(this float3 cameraPosition, ScreenData screenData, CameraScreenToWorldData cameraData)
        {
            float2 screenPosition = new (screenData.ScreenPosition.x, screenData.ScreenHeight - screenData.ScreenPosition.y);
            
            float4 clipSpace = new (((screenPosition.x * 2.0f) / screenData.ScreenWidth) - 1.0f, (1.0f - (2.0f * screenPosition.y) / screenData.ScreenHeight), 0.0f, 1.0f);
     
            float4 viewSpace = mul(cameraData.CameraProjectionInverse, clipSpace);
            viewSpace /= viewSpace.w;
     
            float4 worldSpace = mul(cameraData.CameraToWorld, viewSpace);
     
            float3 worldDirection = normalize(worldSpace.xyz - cameraPosition);
     
            return worldDirection;
        }
        
        public static Ray ScreenPointToRay(this float3 cameraPosition, ScreenData screenData, CameraScreenToWorldData cameraData)
        {
            return new Ray(cameraPosition, cameraPosition.ScreenToWorldRay(screenData, cameraData));
        }
        
        public static float3 ScreenToWorldDirection(this Camera camera, float2 screenPosition, float screenWidth, float screenHeight)
        {
            float2 tempScreenPosition = new (screenPosition.x, screenHeight - screenPosition.y);
            
            float4 clipSpace = new (((tempScreenPosition.x * 2f) / screenWidth) - 1f, (1f - (2f * tempScreenPosition.y) / screenHeight), 0, 1f);
     
            float4 viewSpace = mul(inverse(camera.projectionMatrix), clipSpace);
            viewSpace /= viewSpace.w;
     
            float4 worldSpace = mul(camera.cameraToWorldMatrix, viewSpace);
     
            float3 worldDirection = normalize(worldSpace.xyz - (float3)camera.transform.position);
     
            return worldDirection;
        }
        
        public static float3 ScreenToWorldDirection(this float2 screenPosition,in float3 cameraPosition,in float4x4 projectionMatrix,in float4x4 cameraToWorldMatrix,float screenWidth, float screenHeight)
        {
            float2 tempScreenPosition = new (screenPosition.x, screenHeight - screenPosition.y);
            
            float4 clipSpace = new (((tempScreenPosition.x * 2f) / screenWidth) - 1f, (1f - (2f * tempScreenPosition.y) / screenHeight), 0, 1f);
     
            float4 viewSpace = mul(inverse(projectionMatrix), clipSpace);
            viewSpace /= viewSpace.w;
     
            float4 worldSpace = mul(cameraToWorldMatrix, viewSpace);
     
            float3 worldDirection = normalize(worldSpace.xyz - cameraPosition);
     
            return worldDirection;
        }
        
        public static Ray ScreenPointToRay(this Camera camera, float2 screenPosition, float screenWidth, float screenHeight)
        {
            return new Ray(camera.transform.position, camera.ScreenToWorldDirection(screenPosition, screenWidth, screenHeight));
        }
        
    }
}
