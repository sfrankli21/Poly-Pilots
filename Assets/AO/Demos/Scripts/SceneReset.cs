using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Enables the scene reset input action, requires a Player Input component to be attached to the same object.
    /// </summary>
    public class SceneReset : MonoBehaviour
    {
        public PlayerInput playerInput;
        // Start is called before the first frame update
        void Start()
        {
            if (playerInput == null)
            {
                GetComponent<PlayerInput>().actions.FindAction("Reset").performed += ctx => ReloadScene();
            }
            else
            {
                playerInput.actions.FindAction("Reset").performed += ctx => ReloadScene();
            }
        }

        public void ReloadScene()
        {
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }
    }
}
