using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace KaizerWald
{
    public static class RearrangmentUtils
    {
        private static void Test()
        {
            
        }
        private static bool IsUnitValid(EntityQuery unitKilledQuery, Entity unit)
        {
            return unit != Entity.Null && !unitKilledQuery.Matches(unit);
        }
        
        public static int RearrangeInline(EntityQuery unitKilledQuery, NativeArray<Entity> units, int index)
        {
            //Debug.Log($"Pass Inline at: {index}");
            if (index == units.Length - 1) return -1;
            int maxIteration = units.Length - index;
            for (int i = 1; i < maxIteration; i++) //Begin at 1, so we start at the next index
            {
                int indexToCheck = index + i;
                if (IsUnitValid(unitKilledQuery, units[indexToCheck])) return indexToCheck;
            }
            return -1;
        }
        
        private static bool IsNextRowValid(EntityQuery unitKilledQuery, NativeArray<Entity> units, int yLine, in DynamicFormation formation)
        {
            int nextYLineIndex = yLine + 1;
            int lastLineIndex = formation.NumLine - 1;
            
            if (nextYLineIndex > lastLineIndex) return false;
            int numUnitOnLine = select(formation.UnitsPerLine,formation.NumUnitsLastLine, nextYLineIndex == lastLineIndex);

            NativeSlice<Entity> lineToCheck = new (units,nextYLineIndex * formation.UnitsPerLine, numUnitOnLine);
            int numValid = 0;
            foreach (Entity unit in lineToCheck)
            {
                numValid += select(0,1,IsUnitValid(unitKilledQuery, unit));
            }
            return numValid > 0;
            /*
            int numInvalid = 0;
            foreach (Entity unit in lineToCheck)
            {
                numInvalid += select(0,1,!IsUnitValid(unitKilledQuery, unit));
            }
            return numInvalid != lineToCheck.Length;
            */
        }

        private static bool IsNextRowValid(EntityQuery unitKilledQuery, NativeArray<Entity> units, int yLine,
            int unitsPerLine, int numLine, int numUnitsLastLine)
        {
            int nextYLineIndex = yLine + 1;
            int lastLineIndex = numLine - 1;

            if (nextYLineIndex > lastLineIndex) return false;
            int numUnitOnLine = select(unitsPerLine,numUnitsLastLine,nextYLineIndex == lastLineIndex);

            NativeSlice<Entity> lineToCheck = new(units, nextYLineIndex * unitsPerLine, numUnitOnLine);
            for (int i = 0; i < lineToCheck.Length; i++)
            {
                if (IsUnitValid(unitKilledQuery, lineToCheck[i])) return true;
            }
            return false;
        }

        public static int GetIndexAround(EntityQuery unitKilledQuery, NativeArray<Entity> units, int index, in DynamicFormation formation)
        {
            int indexUnit = -1;
            int lastLineIndex = formation.NumLine - 1;
            
            int yInRegiment = index / formation.UnitsPerLine;
            int xInRegiment = index - (yInRegiment * formation.UnitsPerLine);
            
            //Inline if there is only ONE line
            bool nextRowValid = IsNextRowValid(unitKilledQuery, units, yInRegiment, formation);
            if (formation.NumLine == 1 || yInRegiment == lastLineIndex || !nextRowValid) return RearrangeInline(unitKilledQuery, units, index);

            for (int lineIndex = yInRegiment + 1; lineIndex < formation.NumLine; lineIndex++) //Tester avec ++lineIndex (enlever le +1 à yRegiment)
            {
                int lineWidth = select(formation.UnitsPerLine,formation.NumUnitsLastLine,lineIndex == lastLineIndex);
                int lastIndexCurrentLine = lineWidth - 1;
                bool2 leftRightClose = new (xInRegiment == 0, xInRegiment == lastIndexCurrentLine);//-1 because we want the index

                //Check NextLineValid
                //DOCUMENT!
                //IF there is only 1 value take return it!
                //if There is 2 Return the Second 1!
                
                //1) CHeck Unit RightBehind

                
                for (int i = 0; i <= lineWidth; i++) // 0 because we check unit right behind
                {
                    if (all(leftRightClose)) break;
                }
            }

            return indexUnit;
        }

        private static int GetIndexBehind(NativeArray<Entity> units,int xInRegiment, int yInRegiment,int numLine,int unitsPerLine)
        {
            int indexUnitBehind = mad(yInRegiment + 1, unitsPerLine, xInRegiment);
            
            int minIndex = unitsPerLine * (yInRegiment + 1);
            indexUnitBehind = max(minIndex, indexUnitBehind);
            indexUnitBehind = min(minIndex + unitsPerLine - 1, indexUnitBehind);
            //Debug.Log($"dead{mad(yInRegiment, unitsPerLine, xInRegiment)} unitLength: {units.Length} indexUnitBehind = {indexUnitBehind}; select {select(-1,indexUnitBehind,indexUnitBehind < units.Length)};");
            return select(-1,indexUnitBehind,indexUnitBehind < units.Length) ;
        }
        
        public static int GetIndexAround(EntityQuery unitKilledQuery, NativeArray<Entity> units, int index, 
            int numLine, int unitsPerLine, int numUnitsLastLine)
        {
            int indexUnit = -1;
            int lastLineIndex = numLine - 1;
            
            int yInRegiment = index / unitsPerLine;
            int xInRegiment = index - (yInRegiment * unitsPerLine);
            
            //Inline if there is only ONE line
            bool nextRowValid = IsNextRowValid(unitKilledQuery, units, yInRegiment, unitsPerLine, numLine, numUnitsLastLine);
            if (numLine == 1 || yInRegiment == lastLineIndex || !nextRowValid) return RearrangeInline(unitKilledQuery, units, index);
            
            for (int lineIndex = yInRegiment + 1; lineIndex < numLine; lineIndex++) //Tester avec ++lineIndex (enlever le +1 à yRegiment)
            {
                int lineWidth = select(unitsPerLine, numUnitsLastLine,lineIndex == lastLineIndex);
                int lastXCoordCurrentLine = lineWidth - 1;
                
                //ATTENTION: LA DERNIERE LIGNE SI Composé uniquement d'entité null ne sera pas considéré comme VIDE DONC JAMAIS INLINE!
                indexUnit = GetIndexBehind(units, xInRegiment, yInRegiment, numLine, unitsPerLine);
                if (indexUnit != -1 && IsUnitValid(unitKilledQuery, units[indexUnit])) return indexUnit;
                
                bool2 leftRightClose = new (xInRegiment == 0, xInRegiment == lastXCoordCurrentLine);//-1 because we want the index
                for (int i = 0; i <= lineWidth; i++) // 0 because we check unit right behind
                {
                    if (IsRightValid(i)) return indexUnit;
                    if (IsLeftValid(i)) return indexUnit;
                    
                    if (all(leftRightClose)) break;
                }
                
                bool IsRightValid(int i)
                {
                    if (leftRightClose.x) return false;
                    
                    //if line uneven, we readjust x so it's not out of bounds
                    int x = min(xInRegiment, lastXCoordCurrentLine);
                    
                    indexUnit = mad(lineIndex, unitsPerLine, x-i);
                    leftRightClose.x = x - i == 0;
                    return IsUnitValid(unitKilledQuery, units[indexUnit]);
                }

                bool IsLeftValid(int i)
                {
                    if (leftRightClose.y) return false;
                    int x = min(xInRegiment, lastXCoordCurrentLine);
                    
                    indexUnit = mad(lineIndex, unitsPerLine, x+i);
                    leftRightClose.y = x+i >= lastXCoordCurrentLine;
                    return IsUnitValid(unitKilledQuery, units[indexUnit]);
                }
            }
            return -1;
        }
        
    }
    
    
}
