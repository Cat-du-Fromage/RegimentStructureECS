using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KaizerWald
{
    /// <summary>
    /// IN NAPOLEON:
    /// (Infantry) : Line Infantry, skrimisher, milice, elite infantry, light infantry
    /// (cavalerie) : lancer, heavy, light...
    /// What differenciate them?
    /// - Range
    /// - Num Units
    /// - Formation Type (LATER)
    /// - Competence (spell)
    /// </summary>
    [CreateAssetMenu(fileName = "NewRegimentType", menuName = "Regiment/RegimentType", order = 2)]
    public class RegimentTypeSO : ScriptableObject
    {
        [Header("UnitStats")]
        public float Speed = 1f;
        public Vector3 UnitSize = Vector3.one;

        [Header("RegimentStats")] 
        public int BaseNumUnits = 20;
        public int MinRow = 4;
        public int MaxRow = 10;
        
        public float OffsetBetweenUnits = 0.5f;
        //public float SpaceBetweenUnits => UnitSize.x + OffsetBetweenUnits;
    }
}
