using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject[] allCanvas;
    [SerializeField] private GameObject[] mainPanel;
    [SerializeField] private GameObject[] controlPanel;
    [SerializeField] private ScreenFade screenFade;

    [SerializeField] private GameObject[] mainButtons;
    [SerializeField] private GameObject controlsBackButton;
    [SerializeField] private float navigateCooldownTime = 0.2f;

    private float navigateCooldown = 0f;
    private int focusedButton = 0;
    private bool isPaused = false;
    private bool isOnControls = false;

    private InputSystem_Actions inputActions;
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;
    private InputAction pauseAction;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        navigateAction = inputActions.UI.Navigate; // o la teva acció de navigate
        submitAction = inputActions.UI.Submit;
        cancelAction = inputActions.UI.Cancel;
        pauseAction = inputActions.Player.Pause; // canvia per la teva acció de pausa (Start/Menu)
    }
    void Start()
    {
        Desactivar(allCanvas);
        Desactivar(mainPanel);
        Desactivar(controlPanel);
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.UI.Enable();
        pauseAction.performed += OnPause;
        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;
        navigateAction.performed += OnNavigate;
    }

    void OnDisable()
    {
        pauseAction.performed -= OnPause;
        submitAction.performed -= OnSubmit;
        cancelAction.performed -= OnCancel;
        navigateAction.performed -= OnNavigate;
        inputActions.Player.Disable();
        inputActions.UI.Disable();
    }

    void Update()
    {
        if (navigateCooldown > 0f) navigateCooldown -= Time.unscaledDeltaTime;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    private void OnPause(InputAction.CallbackContext ctx)
    {
        Debug.Log("Hola"); 
        TogglePause();

    }


    private void TogglePause()
    {
        if (!isPaused) OpenPause();
        else if (isOnControls) BackToMain();
        else ClosePause();
    }

    private void OpenPause()
    {
        Debug.Log($"OpenPause - allCanvas: {allCanvas.Length}, mainPanel: {mainPanel.Length}");
        isPaused = true;
        isOnControls = false;
        Activar(allCanvas);
        Activar(mainPanel);
        Desactivar(controlPanel);
        Time.timeScale = 0f;
        SetFocus(0);
    }

    private void ClosePause()
    {
        isPaused = false;
        Desactivar(mainPanel);
        Desactivar(controlPanel);
        Desactivar(allCanvas);
        Time.timeScale = 1f;
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (!isPaused || isOnControls) return;
        if (navigateCooldown > 0f) return;

        float y = ctx.ReadValue<Vector2>().y;
        if (y > 0.5f) { SetFocus(focusedButton - 1); navigateCooldown = navigateCooldownTime; }
        else if (y < -0.5f) { SetFocus(focusedButton + 1); navigateCooldown = navigateCooldownTime; }
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (!isPaused) return;

        if (isOnControls) { BackToMain(); return; }

        switch (focusedButton)
        {
            case 0: ResumeButton(); break;
            case 1: ControlsButton(); break;
            case 2: ExitButton(); break;
        }
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (!isPaused) return;
        if (isOnControls) BackToMain();
        else ClosePause();
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

    private void BackToMain()
    {
        isOnControls = false;
        Activar(mainPanel);
        Desactivar(controlPanel);
        SetFocus(0);

        Transform border = controlsBackButton?.transform.Find("FocusBorder");
        if (border != null) border.gameObject.SetActive(false);
    }

    public void ResumeButton()
    {
        ClosePause();
    }

    public void VolverButton() => BackToMain();

    public void ControlsButton()
    {
        isOnControls = true;
        Desactivar(mainPanel);
        Activar(controlPanel);
    }

    public void ExitButton()
    {
        Time.timeScale = 1f;
        if (screenFade != null)
        {
            screenFade.FadeOut();
            StartCoroutine(LoadAfterFade("MainMenu"));
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private System.Collections.IEnumerator LoadAfterFade(string scene)
    {
        yield return new WaitForSecondsRealtime(screenFade.fadeDuration);
        SceneManager.LoadScene(scene);
    }

    private void Desactivar(GameObject[] temp) { foreach (var go in temp) go.SetActive(false); }
    private void Activar(GameObject[] temp) { foreach (var go in temp) go.SetActive(true); }
}