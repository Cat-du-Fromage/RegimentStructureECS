using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace KaizerWald
{
    public static class MathUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(float a, float b)
        {
            return abs(b - a) < max(0.000001f * max(abs(a), abs(b)), EPSILON * 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(float2 a, float2 b)
        {
            return Approximately(a.x, b.x) && Approximately(a.y, b.y);
        }
    }
}
