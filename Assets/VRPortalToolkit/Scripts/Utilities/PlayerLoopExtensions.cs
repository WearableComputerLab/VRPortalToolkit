using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.LowLevel;

namespace VRPortalToolkit.Utilities
{
    /// <summary>
    /// Provides extension methods for modifying Unity's PlayerLoop system.
    /// </summary>
    public static class PlayerLoopExtensions
    {
        /// <summary>
        /// Inserts a sub-system before a specific system type in the player loop.
        /// </summary>
        /// <typeparam name="T">The type of system to insert before.</typeparam>
        /// <param name="system">The player loop system to modify.</param>
        /// <param name="subSystem">The sub-system to insert.</param>
        /// <param name="newSystem">The resulting modified system.</param>
        /// <returns>True if the insertion was successful, false otherwise.</returns>
        public static bool InsertBefore<T>(this PlayerLoopSystem system, PlayerLoopSystem subSystem, out PlayerLoopSystem newSystem)
            => system.Insert<T>(subSystem, out newSystem, true);
        
        /// <summary>
        /// Inserts a sub-system after a specific system type in the player loop.
        /// </summary>
        /// <typeparam name="T">The type of system to insert after.</typeparam>
        /// <param name="system">The player loop system to modify.</param>
        /// <param name="subSystem">The sub-system to insert.</param>
        /// <param name="newSystem">The resulting modified system.</param>
        /// <returns>True if the insertion was successful, false otherwise.</returns>
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

        /// <summary>
        /// Creates a new array with a sub-system inserted at the specified index.
        /// </summary>
        /// <param name="subSystemList">The original sub-system list.</param>
        /// <param name="subSystem">The sub-system to insert.</param>
        /// <param name="index">The index at which to insert the sub-system.</param>
        /// <returns>A new array containing the inserted sub-system.</returns>
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