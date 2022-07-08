using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWald
{
    [Serializable]
    public struct MultiplePreselectData : IComponentData
    {
        public bool ClickDragPerformed;
        public float2 StartLMouse;
        public float2 EndLMouse;
    }

    public struct IsClickDragPerformed : IComponentData
    {
        public bool Value;
    }
    
    public struct StartLeftMousePosition : IComponentData
    {
        public float2 Value;
    }
    
    public struct EndLeftMousePosition : IComponentData
    {
        public float2 Value;
    }
    
    public struct SelectionBounds : IComponentData
    {
        public Bounds Value;
    }
}