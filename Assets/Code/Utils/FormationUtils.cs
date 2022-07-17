using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using static Unity.Mathematics.float3;
using static Unity.Mathematics.quaternion;

namespace KaizerWald
{
    public static class FormationUtils
    {
        /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NumUnitsToAdd(NativeParallelHashSet<Entity> selectedRegiments, in float3 startPosition, in float3 endPosition)
        {
            float lineLength = length(endPosition - startPosition);
            float minRegimentsFormationSize = selectedRegiments.MinSizeFormation();
            
            int numUnitToAdd = (int)(lineLength - minRegimentsFormationSize);
            return max(0, numUnitToAdd);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MinSizeFormation(NativeParallelHashSet<Entity> selectedRegiments)
        {
            int numSelection = selectedRegiments.Count();
            float min = 0;
            float mediumSize = 0;
            for (int i = 0; i < numSelection; i++)
            {
                RegimentClass regimentClass = selectedRegiments[i].RegimentClass;
                float unitSpace = regimentClass.SpaceSizeBetweenUnit;
                min += unitSpace * regimentClass.MinRow;
                
                mediumSize += regimentClass.UnitSize.x/2f;
                
                if (i == 0) continue;
                float distanceBetweenUnit = selectedRegiments[i - 1].RegimentClass.SpaceSizeBetweenUnit;
                float spaceBetweenRegiment = distanceBetweenUnit + unitSpace / 2f;
                min += spaceBetweenRegiment;
            }

            mediumSize /= numSelection;
            return min - mediumSize;
        }
        */
    }
}
