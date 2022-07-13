using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KaizerWald
{
    [CreateAssetMenu(fileName = "NewRegimentType", menuName = "Regiment/Regiment", order = 0)]
    public class RegimentSO : ScriptableObject
    {
        [Header("Unit Prefab")]
        public GameObject PrefabUnit;
        
        [Header("Regiment Class")]
        public RegimentClassSO regimentClass;
        
        public GameObject PrefabPlacement => regimentClass.PrefabPlacement;
        
        [Header("Regiment Type")]
        public RegimentTypeSO regimentType;
        
        public float Speed => regimentType.Speed;
        public Vector3 UnitSize => regimentType.UnitSize;
        public int BaseNumUnits => regimentType.BaseNumUnits;
        public int MinRow => regimentType.MinRow;
        public int MaxRow => regimentType.MaxRow;
        
        public float SpaceBetweenUnitsX => regimentClass.UnitSize.x + regimentType.OffsetBetweenUnits;
        public float SpaceBetweenUnitsZ => regimentClass.UnitSize.z + regimentType.OffsetBetweenUnits;
    }
}
