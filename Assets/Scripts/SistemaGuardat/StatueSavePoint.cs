using UnityEngine;
using UnityEngine.InputSystem;

public class StatueSavePoint : MonoBehaviour
{
    [Header("Detecció")]
    [SerializeField] private float detectionRadius = 3f;

    [Header("Tutorials")]
    [SerializeField] private TutorialEntry tutorialFirstStatue;
    [SerializeField] private TutorialEntry infoStatue;

    [Header("UI")]
    [SerializeField] private GameObject promptUI;       // imatge petita amb el botó a prémer

    private InputSystem_Actions inputActions;
    private bool playerInRange = false;
    private bool menuOpen = false;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Interact.performed += OnInteract; // assigna un botó "Interact" al Input Actions (per exemple, botó Sud/A/X)
        //inputActions.Player.Help.performed += OnHelp;
        inputActions.UI.Cancel.performed += OnCancel;
    }

    void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteract;
        //inputActions.Player.Help.performed -= OnHelp;
        inputActions.UI.Cancel.performed -= OnCancel;
        inputActions.Player.Disable();
        inputActions.UI.Disable();
    }

    void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        // Detecta si el player és a prop
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        bool found = false;
        foreach (Collider col in hits)
        {
            if (col.CompareTag("Player")) { found = true; break; }
        }

        if (found && !playerInRange)
        {
            playerInRange = true;
            //OnPlayerEnter();
        }
        else if (!found && playerInRange)
        {
            playerInRange = false;
            //OnPlayerExit();
        }
    }

    // ── Detecció ──────────────────────────────────────────────────────────────

    private void OnPlayerEnter()
    {
        if (promptUI != null) promptUI.SetActive(true);
        TutorialManager.Instance?.TriggerIfNew("hasSeenStatue", () => DroneSpeaker.Instance?.Speak(tutorialFirstStatue));
    }

    private void OnPlayerExit()
    {
        if (promptUI != null) promptUI.SetActive(false);
        if (menuOpen) CloseMenu();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnInteract(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (!playerInRange) return;
        if (DroneSpeaker.Instance != null && DroneSpeaker.Instance.IsSpeaking) return;
        if (!menuOpen) OpenMenu();
    }
    private void OnCancel(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (!menuOpen) return;
        CloseMenu();
    }

    /*private void OnHelp(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (!playerInRange) return;
        if (menuOpen) return;
        if (DroneSpeaker.Instance != null && DroneSpeaker.Instance.IsSpeaking) return;
        DroneSpeaker.Instance?.Speak(infoStatue);
    }*/

    // ── Menú ──────────────────────────────────────────────────────────────────

    private void OpenMenu()
    {
        menuOpen = true;
        inputActions.Player.Disable();
        inputActions.UI.Enable();
        if (promptUI != null) promptUI.SetActive(false);
        SaveMenuUI.Instance?.Open(this);
    }

    private void CloseMenu()
    {
        menuOpen = false;
        inputActions.UI.Disable();
        inputActions.Player.Enable();
        SaveMenuUI.Instance?.Close();
    }

    // Cridat des del SaveMenuUI quan es tanca
    public void OnMenuClosed()
    {
        menuOpen = false;
        inputActions.UI.Disable();
        inputActions.Player.Enable();
        if (playerInRange && promptUI != null) promptUI.SetActive(true);
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}