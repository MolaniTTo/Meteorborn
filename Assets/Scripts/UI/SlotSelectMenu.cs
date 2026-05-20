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

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;

    private int focusedSlot = 0;
    private MenuState state = MenuState.SelectingSlot;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    void Awake()
    {
        var uiMap = playerInput.actions.FindActionMap("UI", throwIfNotFound: true);
        navigateAction = uiMap.FindAction("Navigate", throwIfNotFound: true);
        submitAction = uiMap.FindAction("Submit", throwIfNotFound: true);
        cancelAction = uiMap.FindAction("Cancel", throwIfNotFound: true);
    }

    void OnEnable()
    {
        navigateAction.performed += OnNavigate;
        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;
        RefreshCards();
        SetFocus(0);
    }

    void OnDisable()
    {
        navigateAction.performed -= OnNavigate;
        submitAction.performed -= OnSubmit;
        cancelAction.performed -= OnCancel;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (state != MenuState.SelectingSlot) return;

        float y = ctx.ReadValue<Vector2>().y; //Vertical per suportar navegació vertical (ex: joystick)
        if (y > 0.5f) SetFocus(0); //amunt → slot 0
        else if (y < -0.5f) SetFocus(1); //avall → slot 1

    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (state != MenuState.SelectingSlot) return;

        if (SaveManager.Instance.HasSaveSlot(focusedSlot))
            OpenSlotSubmenu(focusedSlot);
        else
            OpenConfirmNewGame(focusedSlot);
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (state == MenuState.SelectingSlot) return;
        CloseDialogAndReturn();
    }

    // ── Submenu slot ple ──────────────────────────────────────────────────────

    private void OpenSlotSubmenu(int slot)
    {
        state = MenuState.SlotSubmenu;
        SaveManager.Instance.SetActiveSlot(slot);
        confirmDialog.Show(
            $"Partida {slot + 1}",
            confirmText: "Jugar",
            cancelText: "Eliminar",
            onConfirm: () => LoadAndEnterGame(slot),
            onCancel: () => RequestDeleteSlot(slot), // ← botó Eliminar
            onBack: CloseDialogAndReturn            // ← botó B/Cercle sempre torna enrere
        );
    }

    // ── Confirmar nova partida ────────────────────────────────────────────────

    private void OpenConfirmNewGame(int slot)
    {
        state = MenuState.ConfirmNewGame;
        confirmDialog.Show(
            "Vols començar una nova aventura?",
            confirmText: "Sí",
            cancelText: "No",
            onConfirm: () => NewGameAndEnter(slot),
            onCancel: CloseDialogAndReturn
        );
    }

    // ── Confirmar eliminar ────────────────────────────────────────────────────

    public void RequestDeleteSlot(int slot)
    {
        state = MenuState.ConfirmDelete;
        confirmDialog.Show(
            $"Eliminar la partida {slot + 1}?",
            confirmText: "Sí, eliminar",
            cancelText: "No, tornar",
            onConfirm: () => ConfirmDelete(slot),
            onCancel: CloseDialogAndReturn
        );
    }

    private void ConfirmDelete(int slot)
    {
        SaveManager.Instance.DeleteSlot(slot);
        CloseDialogAndReturn();
        RefreshCards();
    }

    // ── Navegació i escena ────────────────────────────────────────────────────

    private void LoadAndEnterGame(int slot)
    {
        confirmDialog.Hide();
        SceneManager.sceneLoaded += OnGameSceneLoaded;
        SceneManager.LoadScene(gameSceneName);
    }

    private void NewGameAndEnter(int slot)
    {
        SaveManager.Instance.SetActiveSlot(slot);
        SaveManager.Instance.DeleteSlot(slot);
        confirmDialog.Hide();
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        SaveManager.Instance.Load();
    }

    private void CloseDialogAndReturn()
    {
        state = MenuState.SelectingSlot;
        confirmDialog.Hide();
        SetFocus(focusedSlot);
    }

    private void SetFocus(int slot)
    {
        focusedSlot = slot;
        for (int i = 0; i < slotCards.Length; i++)
            slotCards[i].SetFocused(i == focusedSlot);
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