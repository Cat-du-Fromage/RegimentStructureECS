using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KaizerWald
{
    public static class FormationUtility
    {
        public static ComponentTypes GetFormationComponents()
        {
            ComponentTypes types = new ComponentTypes
            (
                new ComponentType[]
                {
                    ComponentType.ReadOnly<NumberUnits>(),
                    ComponentType.ReadOnly<MinLine>(),
                    ComponentType.ReadOnly<MaxLine>(),
                    ComponentType.ReadOnly<NumberLine>(),
                    ComponentType.ReadOnly<UnitsLastLine>(),
                    ComponentType.ReadOnly<UnitsPerLine>(),
                }
            );
            return types;
        }
    }
}
