using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace VRPortalToolkit.Examples
{
    /// <summary>
    /// Provides a simple way to restart the current scene using a specified input action.
    /// </summary>
    public class RestartScene : MonoBehaviour
    {
        [SerializeField] private InputActionProperty _restartAction;
        /// <summary>
        /// The input action that will trigger the scene restart when performed.
        /// </summary>
        public InputActionProperty restartAction
        {
            get => _restartAction;
            set => _restartAction = value;
        }

        protected void OnEnable()
        {
            if (_restartAction.action != null)
            {
                _restartAction.EnableDirectAction();
                _restartAction.action.performed += Restart;
            }
        }

        protected void OnDisable()
        {
            if (_restartAction.action != null)
                _restartAction.action.performed -= Restart;
        }

        private void Restart(InputAction.CallbackContext _)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
