using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Misc.UnityHelpers
{
    public class SceneActions : MonoBehaviour
    {
        public UnityEvent<Scene> sceneUnloaded;
        public UnityEvent<Scene> sceneLoaded;

        protected virtual void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected virtual void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        protected virtual void OnSceneUnloaded(Scene scene)
        {
            sceneUnloaded?.Invoke(scene);
        }

        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            sceneLoaded?.Invoke(scene);
        }
    }
}