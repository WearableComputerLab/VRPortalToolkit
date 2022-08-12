using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.LowLevel;

namespace VRPortalToolkit.Utilities
{
    public static class PlayerLoopExtensions
    {
        public static bool InsertBefore<T>(this PlayerLoopSystem system, PlayerLoopSystem subSystem, out PlayerLoopSystem newSystem)
            => system.Insert<T>(subSystem, out newSystem, true);
        
        public static bool InsertAfter<T>(this PlayerLoopSystem system, PlayerLoopSystem subSystem, out PlayerLoopSystem newSystem)
            => system.Insert<T>(subSystem, out newSystem, false);

        private static bool Insert<T>(this PlayerLoopSystem system, PlayerLoopSystem subSystem, out PlayerLoopSystem newSystem, bool before)
        {
            if (system.subSystemList != null)
            {
                PlayerLoopSystem current;

                for (int i = 0; i < system.subSystemList.Length; i++)
                {
                    current = system.subSystemList[i];

                    if (current.type == typeof(T))
                    {
                        system.subSystemList = UpdatedList(system.subSystemList, subSystem, before ? i : (i + 1));
                        newSystem = system;
                        return true;
                    }

                    if (current.Insert<T>(subSystem, out newSystem, before))
                    {
                        system.subSystemList[i] = newSystem;
                        newSystem = system;
                        return true;
                    }
                }
            }

            newSystem = default(PlayerLoopSystem);
            return false;
        }

        public static PlayerLoopSystem[] UpdatedList(PlayerLoopSystem[] subSystemList, PlayerLoopSystem subSystem, int index)
        {
            PlayerLoopSystem[] newSubSystemList = new PlayerLoopSystem[subSystemList.Length + 1];
                
            Array.Copy(subSystemList, newSubSystemList, index);
            newSubSystemList[index] = subSystem;
            Array.Copy(subSystemList, index, newSubSystemList, index + 1, subSystemList.Length - index);

            return newSubSystemList;
        }
    }
}