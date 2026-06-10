// SlotSelectMenu.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SlotSelectMenu : MonoBehaviour
{
    public enum MenuState { SelectingSlot, SlotSubmenu, ConfirmNewGame, ConfirmDelete }

    [Header("Refs UI")]
    [SerializeField] private SlotCardUI[] slotCards = new SlotCardUI[2];
    [SerializeField] private ConfirmDialogUI confirmDialog;

    [Header("Escena")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Botó tornar")]
    [SerializeField] private GameObject backButton;
    [SerializeField] private MainMenuController mainMenuController;

    [SerializeField] private float navigateCooldownTime = 0.2f;
    [SerializeField] private float submitCooldownTime = 0.2f;
    private float navigateCooldown = 0f;
    private float submitCooldown = 0f;

    private int focusedSlot = 0;
    private MenuState state = MenuState.SelectingSlot;

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

        confirmDialog.Init(navigateAction, submitAction, cancelAction);
    }

    void OnEnable()
    {
        inputActions.UI.Enable();
        navigateAction.performed += OnNavigate;
        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;
        navigateCooldown = navigateCooldownTime;
        submitCooldown = submitCooldownTime;
        state = MenuState.SelectingSlot;
        RefreshCards();
        SetFocus(0);
    }

    void OnDisable()
    {
        navigateAction.performed -= OnNavigate;
        submitAction.performed -= OnSubmit;
        cancelAction.performed -= OnCancel;
        inputActions.UI.Disable();
    }

    void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
            inputActions.Dispose();
            inputActions = null;
        }
    }

    void Update()
    {
        if (navigateCooldown > 0f) navigateCooldown -= Time.deltaTime;
        if (submitCooldown > 0f) submitCooldown -= Time.deltaTime;
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (state != MenuState.SelectingSlot) return;
        if (navigateCooldown > 0f) return;

        float y = ctx.ReadValue<Vector2>().y;
        if (y > 0.5f) { SetFocus(focusedSlot - 1); navigateCooldown = navigateCooldownTime; }
        else if (y < -0.5f) { SetFocus(focusedSlot + 1); navigateCooldown = navigateCooldownTime; }
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (state != MenuState.SelectingSlot) return;
        if (submitCooldown > 0f) return;

        submitCooldown = submitCooldownTime;

        if (focusedSlot == slotCards.Length)
        {
            mainMenuController?.ShowMain();
            return;
        }

        if (SaveManager.Instance.HasSaveSlot(focusedSlot))
            OpenSlotSubmenu(focusedSlot);
        else
            OpenConfirmNewGame(focusedSlot);
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (state != MenuState.SelectingSlot)
            CloseDialogAndReturn();
    }

    private void OpenSlotSubmenu(int slot)
    {
        state = MenuState.SlotSubmenu;
        submitAction.performed -= OnSubmit;
        navigateAction.performed -= OnNavigate;

        SaveManager.Instance.SetActiveSlot(slot);
        confirmDialog.Show(
            $"Partida {slot + 1}",
            confirmText: "Jugar",
            cancelText: "Eliminar",
            onConfirm: () => LoadAndEnterGame(slot),
            onCancel: () => RequestDeleteSlot(slot),
            onBack: () => { ResubscribeActions(); CloseDialogAndReturn(); }
        );
    }

    private void OpenConfirmNewGame(int slot)
    {
        state = MenuState.ConfirmNewGame;
        submitAction.performed -= OnSubmit;
        navigateAction.performed -= OnNavigate;

        confirmDialog.Show(
            "Vols començar una nova aventura?",
            confirmText: "Sí",
            cancelText: "No",
            onConfirm: () => NewGameAndEnter(slot),
            onCancel: () => { ResubscribeActions(); CloseDialogAndReturn(); }
        );
    }

    public void RequestDeleteSlot(int slot)
    {
        state = MenuState.ConfirmDelete;
        submitAction.performed -= OnSubmit;
        navigateAction.performed -= OnNavigate;

        confirmDialog.Show(
            $"Eliminar la partida {slot + 1}?",
            confirmText: "Sí, eliminar",
            cancelText: "No, tornar",
            onConfirm: () => ConfirmDelete(slot),
            onCancel: () => { ResubscribeActions(); CloseDialogAndReturn(); }
        );
    }

    private void ResubscribeActions()
    {
        navigateAction.performed -= OnNavigate;
        submitAction.performed -= OnSubmit;
        navigateAction.performed += OnNavigate;
        submitAction.performed += OnSubmit;
    }

    private void ConfirmDelete(int slot)
    {
        SaveManager.Instance.DeleteSlot(slot);
        CloseDialogAndReturn();
        RefreshCards();
    }

    private void LoadAndEnterGame(int slot)
    {
        confirmDialog.Hide();
        SaveManager.Instance.SetActiveSlot(slot);
        SaveManager.Instance.Load();
        SceneManager.sceneLoaded += OnGameSceneLoaded;
        SceneManager.LoadScene(gameSceneName);
    }

    private void NewGameAndEnter(int slot)
    {
        SaveManager.Instance.SetActiveSlot(slot);
        SaveManager.Instance.DeleteSlot(slot);
        SaveManager.Instance.NewGame();
        confirmDialog.Hide();
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
    }

    private void CloseDialogAndReturn()
    {
        state = MenuState.SelectingSlot;
        confirmDialog.Hide();
        SetFocus(focusedSlot);
    }

    private void SetFocus(int slot)
    {
        int maxIndex = slotCards.Length;
        focusedSlot = Mathf.Clamp(slot, 0, maxIndex);

        for (int i = 0; i < slotCards.Length; i++)
            slotCards[i].SetFocused(i == focusedSlot);

        Transform border = backButton?.transform.Find("FocusBorder");
        if (border != null)
            border.gameObject.SetActive(focusedSlot == slotCards.Length);
    }

    private void RefreshCards()
    {
        for (int i = 0; i < slotCards.Length; i++)
        {
            SlotPreviewData preview = SaveManager.Instance?.GetSlotPreview(i);
            slotCards[i].Refresh(i, preview);
        }
    }
}