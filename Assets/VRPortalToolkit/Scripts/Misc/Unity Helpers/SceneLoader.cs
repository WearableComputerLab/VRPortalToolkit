using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace Misc.UnityHelpers
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private string _sceneName;
        public string sceneName { get => _sceneName; set => _sceneName = value; }

        [SerializeField] private LoadSceneMode _loadSceneMode = LoadSceneMode.Single;
        public LoadSceneMode loadSceneMode { get => _loadSceneMode; set => _loadSceneMode = value; }

        [SerializeField] private LocalPhysicsMode _localPhysicsMode;
        public LocalPhysicsMode localPhysicsMode { get => _localPhysicsMode; set => _localPhysicsMode = value; }

        [SerializeField] private bool _async = false;
        public bool async { get => _async; set => _async = value; }

        public UnityEvent<Scene> sceneLoaded;
        public UnityEvent failed;

        public void LoadScene(string sceneName)
        {
            this.sceneName = sceneName;
            LoadScene();
        }

        public void LoadScene()
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                if (async)
                {
                    string currentSceneName = sceneName;
                    AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, new LoadSceneParameters(loadSceneMode, localPhysicsMode));

                    if (operation != null)
                    {
                        operation.completed += (_) =>
                        {
                            Scene scene = SceneManager.GetSceneByName(currentSceneName);

                            if (scene.IsValid())
                            {
                                Debug.Log($"Loaded Scene: " + sceneName);
                                sceneLoaded?.Invoke(scene);
                            }
                            else
                                failed?.Invoke();
                        };
                    }
                    else
                        failed?.Invoke();
                }
                else
                {
                    Scene scene = SceneManager.LoadScene(sceneName, new LoadSceneParameters(loadSceneMode, localPhysicsMode));

                    if (scene.IsValid())
                    {
                        Debug.Log($"Loaded Scene: " + sceneName);
                        sceneLoaded?.Invoke(scene);
                    }
                    else
                        failed?.Invoke();
                }
            }
            else
                failed?.Invoke();
        }
    }
}