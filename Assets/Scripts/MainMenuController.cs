// MainMenuController.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour
{
    public enum MainMenuState { Main, Load, Controls }

    [SerializeField] private GameObject[] mainPanel;
    [SerializeField] private GameObject[] loadPanel;
    [SerializeField] private GameObject[] controlPanel;

    [Header("Botons del menú principal (ordre: Jugar, Controls, Sortir)")]
    [SerializeField] private GameObject[] mainButtons;

    [Header("Controls panel")]
    [SerializeField] private GameObject controlsBackButton;

    [SerializeField] private float navigateCooldownTime = 0.2f;
    private float navigateCooldown = 0f;

    private MainMenuState currentState = MainMenuState.Main;
    private int focusedButton = 0;

    private InputSystem_Actions inputActions;
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        navigateAction = inputActions.UI.Navigate;
        submitAction = inputActions.UI.Submit;
        cancelAction = inputActions.UI.Cancel;
    }

    void OnEnable()
    {
        inputActions.UI.Enable();
        navigateAction.performed += OnNavigate;
        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;
    }

    void OnDisable()
    {
        navigateAction.performed -= OnNavigate;
        submitAction.performed -= OnSubmit;
        cancelAction.performed -= OnCancel;
        inputActions.UI.Disable();
    }

    void Start()
    {
        ShowMain();
    }

    void Update()
    {
        if (navigateCooldown > 0f) navigateCooldown -= Time.deltaTime;
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (currentState == MainMenuState.Controls)
        {
            SetControlsBackFocus(true);
            return;
        }
        if (currentState != MainMenuState.Main) return;
        if (navigateCooldown > 0f) return;

        float y = ctx.ReadValue<Vector2>().y;
        if (y > 0.5f) { SetFocus(focusedButton - 1); navigateCooldown = navigateCooldownTime; }
        else if (y < -0.5f) { SetFocus(focusedButton + 1); navigateCooldown = navigateCooldownTime; }
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {

        if (currentState == MainMenuState.Controls) { ShowMain(); return; }
        if (currentState != MainMenuState.Main) return;

        switch (focusedButton)
        {
            case 0: PlayButton(); break;
            case 1: ControlsButton(); break;
            case 2: ExitButton(); break;
        }
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (currentState == MainMenuState.Controls) ShowMain();
    }

    private void SetFocus(int index)
    {
        focusedButton = Mathf.Clamp(index, 0, mainButtons.Length - 1);
        for (int i = 0; i < mainButtons.Length; i++)
        {
            Transform border = mainButtons[i].transform.Find("FocusBorder");
            if (border != null) border.gameObject.SetActive(i == focusedButton);
        }
    }

    private void SetControlsBackFocus(bool focused)
    {
        Transform border = controlsBackButton?.transform.Find("FocusBorder");
        if (border != null) border.gameObject.SetActive(focused);
    }

    public void PlayButton()
    {
        currentState = MainMenuState.Load;
        Desactivar(mainPanel);
        Desactivar(controlPanel);
        Activar(loadPanel);
    }

    public void ControlsButton()
    {
        currentState = MainMenuState.Controls;
        Desactivar(mainPanel);
        Activar(controlPanel);
        Desactivar(loadPanel);
    }

    public void ShowMain()
    {
        currentState = MainMenuState.Main;
        Activar(mainPanel);
        Desactivar(controlPanel);
        Desactivar(loadPanel);
        navigateCooldown = navigateCooldownTime;
        SetFocus(0);
    }

    public void ExitButton() => Application.Quit();
    public void NewGame() => Debug.Log("NuevaPartida");

    private void Desactivar(GameObject[] temp) { foreach (var go in temp) go.SetActive(false); }
    private void Activar(GameObject[] temp) { foreach (var go in temp) go.SetActive(true); }
    void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
            inputActions.Dispose();
            inputActions = null;
        }
    }
}