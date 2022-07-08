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
    }
}
