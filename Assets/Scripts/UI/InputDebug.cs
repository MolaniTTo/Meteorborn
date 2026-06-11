using UnityEngine;
using UnityEngine.InputSystem;

public class InputDebug : MonoBehaviour
{
    private InputSystem_Actions inputActions;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void OnEnable()
    {
        inputActions.UI.Enable();
        inputActions.UI.Navigate.performed += ctx => Debug.Log($"NAVIGATE: {ctx.ReadValue<Vector2>()}");
        inputActions.UI.Submit.performed += ctx => Debug.Log("SUBMIT");
    }

    void OnDisable()
    {
        inputActions.UI.Disable();
    }
}