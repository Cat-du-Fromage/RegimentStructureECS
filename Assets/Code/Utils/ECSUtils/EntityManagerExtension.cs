using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

namespace KaizerWald
{
    public static class EntityManagerExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SelectAddOrRemove<T>(this EntityManager em, in EntityQuery query, bool condition)
        {
            if (condition)
            {
                em.AddComponent<T>(query);
                return;
            }
            em.RemoveComponent<T>(query);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SelectAddOrRemove<T>(this EntityManager em, in Entity entity, bool condition)
        {
            if (condition)
            {
                em.AddComponent<T>(entity);
                return;
            }
            em.RemoveComponent<T>(entity);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SwapComponent<T>(this EntityManager em, Entity e1, Entity e2) where T : struct, IComponentData
        {
            T index1 = em.GetComponentData<T>(e1);
            T index2 = em.GetComponentData<T>(e2);
            
            em.SetComponentData(e1,index2);
            em.SetComponentData(e2,index1);
        }
    }
}
