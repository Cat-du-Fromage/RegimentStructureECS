using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KaizerWald
{
    /// <summary>
    /// IN NAPOLEON: Infantry, cavalry, artillerie
    /// Difference ?
    /// - Token
    /// - FormationType (Later)
    /// - Default Num Unts
    /// </summary>
    [CreateAssetMenu(fileName = "NewRegimentClass", menuName = "Regiment/RegimentClass", order = 1)]
    public class RegimentClassSO : ScriptableObject
    {
        [Header("Placement Prefabs")]
        public GameObject PrefabPlacement;

        [Header("Unit Stats")]
        public Vector3 UnitSize = Vector3.one;
        //public float OffsetBetweenUnits = 0.5f;
        //public float SpaceBetweenUnits => UnitSize.x + OffsetBetweenUnits;
    }
}