// MenuFocusManager.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuFocusManager : MonoBehaviour
{
    public static MenuFocusManager Instance { get; private set; }

    [SerializeField] private PlayerInput playerInput;

    public InputAction NavigateAction { get; private set; }
    public InputAction SubmitAction { get; private set; }
    public InputAction CancelAction { get; private set; }

    void Awake()
    {
        Instance = this;
        var uiMap = playerInput.actions.FindActionMap("UI", throwIfNotFound: true);
        NavigateAction = uiMap.FindAction("Navigate", throwIfNotFound: true);
        SubmitAction = uiMap.FindAction("Submit", throwIfNotFound: true);
        CancelAction = uiMap.FindAction("Cancel", throwIfNotFound: true);
    }
}