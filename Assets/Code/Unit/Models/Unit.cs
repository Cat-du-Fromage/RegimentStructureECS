using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KaizerWald
{
    public class Unit : MonoBehaviour
    {
        public int IndexInRegiment { get; set; }
        
        public UnitAnimation Animation { get; private set; }
        
        //Propreties

        private float speed;

        //STATES
        public bool IsDead { get; private set; }
        public bool IsMoving { get; private set; }
        
        //======================================================
        //TEMPORARY: SHOOTMAnAGER
        
        public Unit Target { get; set; }
        
        //======================================================

        private void Awake()
        {
            Animation = GetComponent<UnitAnimation>();
        }
    }
}