using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using static Unity.Mathematics.float4x4;

namespace KaizerWald
{
    public readonly struct CameraProject
    {
        public readonly float4x4 WorldToCameraMatrix;
        public readonly float4x4 ProjectionMatrix;

        public CameraProject(Camera camera)
        {
            WorldToCameraMatrix = camera.worldToCameraMatrix;
            ProjectionMatrix = camera.projectionMatrix;
        }
    }
    
    public readonly struct CameraDataProject
    {
        public readonly float3 Forward;
        public readonly float4x4 LocalToWorldMatrix;
        public readonly float4x4 WorldToCameraMatrix;
        public readonly float4x4 ProjectionMatrix;

        public CameraDataProject(Camera camera)
        {
            Forward = camera.transform.forward;
            LocalToWorldMatrix = camera.transform.localToWorldMatrix;
            WorldToCameraMatrix = camera.worldToCameraMatrix;
            ProjectionMatrix = camera.projectionMatrix;
        }
    }
    
    public static class CameraUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 WorldToViewportPoint(this float3 position, in CameraProject cameraData)
        {
            float4 worldPos = new float4(position, 1f);
            float4 viewPos = mul(cameraData.WorldToCameraMatrix, worldPos);
            float4 projPos = mul(cameraData.ProjectionMatrix, viewPos);
            float3 ndcPos = projPos.xyz / projPos.w;

            float3 viewportPos = new float3(mad(ndcPos.xy,0.5f,0.5f), -viewPos.z);
            return viewportPos;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 WorldToViewportPoint(this float3 position, in float4x4 worldToCameraMatrix, in float4x4 projectionMatrix)
        {
            float4 worldPos = new float4(position, 1f);
            float4 viewPos = mul(worldToCameraMatrix, worldPos);
            float4 projPos = mul(projectionMatrix, viewPos);
            float3 ndcPos = projPos.xyz / projPos.w;

            float3 viewportPos = new float3(mad(ndcPos.xy,0.5f,0.5f), -viewPos.z);
            return viewportPos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 WorldToScreenPoint(in float4x4 projectionMatrix, in float4x4 worldToCameraMatrix, float3 pos) 
        {
            float4x4 world2Screen = mul(projectionMatrix, worldToCameraMatrix);
            float3 screenPos = world2Screen.MultiplyPoint(pos);
            // (-1, 1)'s clip => (0 ,1)'s viewport
            screenPos = new float3(screenPos.x + 1f, screenPos.y + 1f, screenPos.z + 1f) / 2f;
            // viewport => screen
            return new float2(screenPos.x * Screen.width, screenPos.y * Screen.height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ScreenToWorldPoint(in float4x4 projectionMatrix, in float4x4 worldToCameraMatrix, in float4x4 localToWorldMatrix, in float3 screenPos)
        {
            float4x4 world2Screen = mul(mul(projectionMatrix, worldToCameraMatrix), localToWorldMatrix);
            float4x4 screen2World = inverse(mul(projectionMatrix, worldToCameraMatrix));
            float depth = world2Screen.MultiplyPoint(screenPos).z;
            // viewport pos (0 ,1)
            float3 viewPos = new float3(screenPos.x / Screen.width, screenPos.y / Screen.height, (depth + 1f) / 2f);
            // clip pos (-1, 1)
            float3 clipPos = viewPos * 2f - new float3(1);
            // world pos
            return screen2World.MultiplyPoint(clipPos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ray ScreenPointToRay(in float4x4 projectionMatrix, in float4x4 worldToCameraMatrix, in float4x4 localToWorldMatrix, in float3 forward, in float3 screenPos) {
            return new Ray(ScreenToWorldPoint(projectionMatrix, worldToCameraMatrix, localToWorldMatrix, screenPos), forward);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ray ScreenPointToRay(this float2 screenPos, CameraDataProject data) {
            return new Ray(ScreenToWorldPoint(data.ProjectionMatrix, data.WorldToCameraMatrix, data.LocalToWorldMatrix, new float3(screenPos,0)), data.Forward);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 MultiplyPoint3x4(this float4x4 matrix, in float3 point)
        {
            float3 res;
            res.x = matrix.c0.x * point.x + matrix.c0.y * point.y + matrix.c0.z * point.z + matrix.c0.w;
            res.y = matrix.c1.x * point.x + matrix.c1.y * point.y + matrix.c1.z * point.z + matrix.c1.w;
            res.z = matrix.c2.x * point.x + matrix.c2.y * point.y + matrix.c2.z * point.z + matrix.c2.w;
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 MultiplyPoint(this float4x4 matrix, in float3 point)
        {
            float3 res;
            float w;
            res.x = matrix.c0.x * point.x + matrix.c0.y * point.y + matrix.c0.z * point.z + matrix.c0.w;
            res.y = matrix.c1.x * point.x + matrix.c1.y * point.y + matrix.c1.z * point.z + matrix.c1.w;
            res.z = matrix.c2.x * point.x + matrix.c2.y * point.y + matrix.c2.z * point.z + matrix.c2.w;
            w     = matrix.c3.x * point.x + matrix.c3.y * point.y + matrix.c3.z * point.z + matrix.c3.w;

            w = 1f / w;
            res.x *= w;
            res.y *= w;
            res.z *= w;
            return res;
        }
        
        
        /// <summary>
        /// Convert point from world space to screen space
        /// </summary>
        /// <param name="point">Point in World Space</param>
        /// <param name="cameraPos">Camera position in World Space</param>
        /// <param name="camProjMatrix">Camera.projectionMatrix</param>
        /// <param name="camUp">Camera.transform.up</param>
        /// <param name="camRight">Camera.transform.right</param>
        /// <param name="camForward">Camera.transform.forward</param>
        /// <param name="pixelWidth">Camera.pixelWidth</param>
        /// <param name="pixelHeight">Camera.pixelHeight</param>
        /// <param name="scaleFactor">Canvas.scaleFactor</param>
        /// <returns></returns>
        public static float2 ConvertWorldToScreenCoordinates(float3 point, float3 cameraPos, float4x4 camProjMatrix, float3 camUp, float3 camRight, float3 camForward, float pixelWidth, float pixelHeight, float scaleFactor)
        {
            /*
            * 1 convert P_world to P_camera
            */
            float4 pointInCameraCoodinates = ConvertWorldToCameraCoordinates(point, cameraPos, camUp, camRight, camForward);
     
     
            /*
            * 2 convert P_camera to P_clipped
            */
            float4 pointInClipCoordinates = math.mul(camProjMatrix, pointInCameraCoodinates);
     
            /*
            * 3 convert P_clipped to P_ndc
            * Normalized Device Coordinates
            */
            float4 pointInNdc = pointInClipCoordinates / pointInClipCoordinates.w;
     
     
            /*
            * 4 convert P_ndc to P_screen
            */
            float2 pointInScreenCoordinates;
            pointInScreenCoordinates.x = pixelWidth / 2.0f * (pointInNdc.x + 1);
            pointInScreenCoordinates.y = pixelHeight / 2.0f * (pointInNdc.y + 1);
     
     
            // return screencoordinates with canvas scale factor (if canvas coords required)
            return pointInScreenCoordinates / scaleFactor;
        }
     
        private static float4 ConvertWorldToCameraCoordinates(float3 point, float3 cameraPos, float3 camUp, float3 camRight, float3 camForward)
        {
            // translate the point by the negative camera-offset
            //and convert to Vector4
            float4 translatedPoint = new float4(point - cameraPos, 1f);
     
            // create transformation matrix
            float4x4 transformationMatrix = identity;
            
            transformationMatrix.c0 = new float4(camRight.x, camUp.x, -camForward.x, 0);
            transformationMatrix.c1 = new float4(camRight.y, camUp.y, -camForward.y, 0);
            transformationMatrix.c2 = new float4(camRight.z, camUp.z, -camForward.z, 0);
     
            float4 transformedPoint = math.mul(transformationMatrix, translatedPoint);
     
            return transformedPoint;
        }


    }
}
