using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona la lògica del menú de selecció de slots.
/// Col·loca aquest component al GameObject del menú principal.
/// 
/// SETUP:
///  1. Afegeix aquest script a un GameObject del MainMenu.
///  2. Assigna al Inspector: els dos SlotCardUI, el panell de confirmació,
///     el nom de l'escena de joc i el PlayerInput.
///  3. Al teu Action Map "UI" assegura't que tens Navigate (Vector2),
///     Submit i Cancel.
/// </summary>
public class SlotSelectMenu : MonoBehaviour
{
    [Header("Refs UI")]
    [SerializeField] private SlotCardUI[] slotCards = new SlotCardUI[2]; // [0] i [1]
    [SerializeField] private ConfirmDialogUI confirmDialog;

    [Header("Escena")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput; // component PlayerInput a l'escena

    // Estat intern
    private int focusedSlot = 0;
    private bool dialogOpen = false;
    private int pendingDeleteSlot = -1;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    // ?? Unity ?????????????????????????????????????????????????????????????

    void Awake()
    {
        // Obtenim les accions del mapa "UI"
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

    // ?? Input ?????????????????????????????????????????????????????????????

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (dialogOpen) return;

        float x = ctx.ReadValue<Vector2>().x;
        if (x > 0.5f) SetFocus(1);
        else if (x < -0.5f) SetFocus(0);
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (dialogOpen)
        {
            // El diàleg gestiona el seu propi focus intern; aquí només tanquem si es confirma
            return;
        }
        StartOrCreateSlot(focusedSlot);
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (dialogOpen)
        {
            CloseConfirmDialog();
        }
        // Si no hi ha diàleg, pots navegar cap enrere al menú principal aquí si cal
    }

    // ?? API pública (cridada pels botons dels SlotCardUI via UnityEvents) ??

    /// <summary>Cridada quan es prem "Jugar / Nova partida" al slot.</summary>
    public void StartOrCreateSlot(int slot)
    {
        SaveManager.Instance.SetActiveSlot(slot);

        if (SaveManager.Instance.HasSaveSlot(slot))
            LoadAndEnterGame(slot);
        else
            NewGameAndEnter(slot);
    }

    /// <summary>Obre el diàleg de confirmació per eliminar el slot indicat.</summary>
    public void RequestDeleteSlot(int slot)
    {
        pendingDeleteSlot = slot;
        dialogOpen = true;
        confirmDialog.Show(
            $"Eliminar la partida del slot {slot + 1}?",
            onConfirm: ConfirmDelete,
            onCancel: CloseConfirmDialog
        );
    }

    // ?? Privats ???????????????????????????????????????????????????????????

    private void LoadAndEnterGame(int slot)
    {
        // El SaveManager ja té el slot actiu; carregarem les dades un cop l'escena estigui carregada.
        SceneManager.sceneLoaded += OnGameSceneLoaded;
        SceneManager.LoadScene(gameSceneName);
    }

    private void NewGameAndEnter(int slot)
    {
        SaveManager.Instance.DeleteSlot(slot); // neteja per si hi ha restes
        // No cridem Load; l'escena comença des de zero
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        SaveManager.Instance.Load(); // aplica les dades del slot actiu al món ja carregat
    }

    private void ConfirmDelete()
    {
        SaveManager.Instance.DeleteSlot(pendingDeleteSlot);
        pendingDeleteSlot = -1;
        CloseConfirmDialog();
        RefreshCards();
    }

    private void CloseConfirmDialog()
    {
        dialogOpen = false;
        confirmDialog.Hide();
        SetFocus(focusedSlot); // restaura el focus
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
            slotCards[i].Refresh(i, preview); // preview == null ? slot buit
        }
    }
}