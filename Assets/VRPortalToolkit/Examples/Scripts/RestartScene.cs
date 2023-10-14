using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace VRPortalToolkit.Examples
{
    public class RestartScene : MonoBehaviour
    {
        [SerializeField] private InputActionProperty _restartAction;
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
