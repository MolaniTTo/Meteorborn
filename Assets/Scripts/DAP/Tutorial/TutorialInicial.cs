using System.Collections;
using UnityEngine;

public class TutorialInicial : MonoBehaviour
{
    public static TutorialInicial Instance { get; private set; }
    public bool isTutorialCompleted = false;

    [Header("Referències")]
    [SerializeField] private PlayerStateMachine player;
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private dapMovementScript droneMovement;
    [SerializeField] private DroneHUD droneHUD;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerVisionDetector visionDetector;

    [Header("Detecció FOV dron → player")]
    [SerializeField] private Transform droneCameraTransform;
    [SerializeField] private float playerFOVAngle = 60f;
    [SerializeField] private float playerFOVDistance = 30f;

    [Header("Línies de tutorial — Fase 1 (world space)")]
    [SerializeField] private TutorialEntry introEntry;
    [SerializeField] private TutorialEntry moveEntry;
    [SerializeField] private TutorialEntry droneEntry;

    [Header("Línies de tutorial — Fase 2 (DroneHUD)")]
    [SerializeField] private TutorialEntry droneMoveEntry;
    [SerializeField] private TutorialEntry droneFOVEntry;

    [Header("Línies de tutorial — Fase 3 (ortho)")]
    [SerializeField] private TutorialEntry orthoMoveEntry;
    [SerializeField] private TutorialEntry orthoAcceptEntry;
    [SerializeField] private TutorialEntry orthoExitEntry;


    [Header("Línies de tutorial — Fase 4 (sortir dron)")]
    [SerializeField] private TutorialEntry exitDroneEntry;

    [Header("Línies de tutorial — Fase 5 (cursor)")]
    [SerializeField] private TutorialEntry cursorEntry;
    [SerializeField] private TutorialEntry cursorHoldEntry;
    [SerializeField] private TutorialEntry tutorialEndEntry;

    private InputSystem_Actions inputActions;
    private Vector3 _droneStartPos;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        inputActions = new InputSystem_Actions();
    }

    void OnEnable() => inputActions.Player.Enable();
    void OnDisable() => inputActions.Player.Disable();

    void Start()
    {
        StartCoroutine(InitTutorial());
    }

    private IEnumerator InitTutorial()
    {
        yield return null;

        Debug.Log($"[TutorialInicial] TutorialManager existe: {TutorialManager.Instance != null}");
        Debug.Log($"[TutorialInicial] IsTutorialCompleted: {TutorialManager.Instance?.IsTutorialCompleted()}");
        Debug.Log($"[TutorialInicial] SaveManager existe: {SaveManager.Instance != null}");
        Debug.Log($"[TutorialInicial] HasSave: {SaveManager.Instance?.HasSave()}");

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialCompleted())
        {
            Debug.Log("[TutorialInicial] Tutorial ja completat, desbloquejant tot.");
            player.canMove = true;
            player.canUseDrone = true;
            player.canUseOrtho = true;
            player.canInteract = true;
            player.canExitDrone = true;
            player.canUseCursor = true;
            player.canMoveOrthoCursor = true;
            yield break;
        }

        StartCoroutine(RunTutorial());
    }

    // ── UTILS ─────────────────────────────────────────────────────────────────

    // Parla i espera que el jugador premi A — per textos informatius
    private IEnumerator SpeakWorldSpace(TutorialEntry entry)
    {
        if (entry == null) yield break;
        DroneSpeaker.Instance?.Speak(entry);

        yield return null;
        yield return null; // espera a que IsSpeaking se active

        yield return new WaitUntil(() => DroneSpeaker.Instance != null && !DroneSpeaker.Instance.IsSpeaking);
    }

    // Parla i espera que el jugador faci una acció — tanca sol quan es compleix
    private IEnumerator SpeakAndWaitForButton(TutorialEntry entry, System.Func<bool> condition, System.Action onUnlock = null)
    {
        if (entry == null) yield break;

        int unlockAt = entry.inputUnlocksAtLine < 0 ? int.MaxValue : entry.inputUnlocksAtLine;
        bool unlocked = false;

        DroneSpeaker.Instance?.Speak(entry);

        yield return null;
        yield return null;

        while (DroneSpeaker.Instance != null && DroneSpeaker.Instance.IsSpeaking)
        {
            if (!unlocked && DroneSpeaker.Instance.CurrentLineIndex >= unlockAt && !DroneSpeaker.Instance.IsTyping)
            {
                onUnlock?.Invoke(); // activa el input justo aquí
                unlocked = true;
            }
            yield return null;
        }

        // Si el diálogo acabó sin desbloquear (pocas líneas), desbloqueamos igualmente
        if (!unlocked)
            onUnlock?.Invoke();

        yield return new WaitUntil(condition);
        DroneSpeaker.Instance?.ForceClose();
    }

    // Mostra text al HUD i espera que el jugador premi A — per textos informatius al dron
    private IEnumerator SpeakDroneHUD(string[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;

        foreach (string line in lines)
        {
            droneHUD.ShowTutorialText(line);
            bool confirmed = false;
            System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> onConfirm = _ => confirmed = true;
            inputActions.Player.Confirm.performed += onConfirm;
            droneHUD.ShowTutorialContinuePrompt();
            yield return new WaitUntil(() => confirmed);
            inputActions.Player.Confirm.performed -= onConfirm;
            // NO desactivis l'input aquí
        }

        droneHUD.HideTutorialPanel();
    }

    private IEnumerator SpeakDroneHUDEntry(TutorialEntry entry, System.Func<bool> condition, System.Action onUnlock = null)
    {
        if (entry == null) yield break;

        int unlockAt = entry.inputUnlocksAtLine < 0 ? int.MaxValue : entry.inputUnlocksAtLine;

        for (int i = 0; i < entry.lines.Length; i++)
        {
            droneHUD.ShowTutorialText(entry.lines[i]);

            if (i < unlockAt)
            {
                bool confirmed = false;
                System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> onConfirm = _ => confirmed = true;
                inputActions.Player.Confirm.performed += onConfirm;
                droneHUD.ShowTutorialContinuePrompt();
                yield return new WaitUntil(() => confirmed);
                inputActions.Player.Confirm.performed -= onConfirm;
            }
            else
            {
                onUnlock?.Invoke();
                onUnlock = null;

                yield return new WaitUntil(condition);
                droneHUD.HideTutorialPanel();
                yield return new WaitForSeconds(0.3f);
                yield break;
            }
        }

        // Todas informativas — ejecuta onUnlock aquí antes de salir
        onUnlock?.Invoke();
        droneHUD.HideTutorialPanel();
        yield return new WaitForSeconds(0.3f);
    }

    // Mostra text al HUD i espera una acció — tanca sol quan es compleix
    private IEnumerator SpeakDroneHUDAndWait(string line, System.Func<bool> condition)
    {
        droneHUD.ShowTutorialText(line);
        yield return new WaitUntil(condition);
        droneHUD.HideTutorialPanel();
        yield return new WaitForSeconds(0.3f); // petit marge per evitar salts
    }

    private bool PlayerInDroneFOV()
    {
        if (droneCameraTransform == null || playerTransform == null) return false;
        Vector3 dir = playerTransform.position - droneCameraTransform.position;
        if (dir.magnitude > playerFOVDistance) return false;
        return Vector3.Angle(droneCameraTransform.forward, dir.normalized) < playerFOVAngle * 0.5f;
    }

    // ── TUTORIAL FLOW ─────────────────────────────────────────────────────────

    private IEnumerator RunTutorial()
    {
        if (visionDetector != null) visionDetector.tutorialActive = true;
        player.canMove = false;
        player.canUseDrone = false;
        player.canUseOrtho = false;
        player.canInteract = false;
        player.canExitDrone = false;
        player.canUseCursor = false;

        droneMovement.FreezeAtTutorialPosition();

        // ── FASE 1 ────────────────────────────────────────────────────────────

        // Intro: espera A normal
        yield return SpeakWorldSpace(introEntry);

        // Moure's: es tanca quan el jugador es mou
        yield return SpeakAndWaitForButton(moveEntry, () => player.IsMoving, () => player.canMove = true);
        yield return new WaitForSeconds(0.5f);

        // Entrar al dron: es tanca quan entra al dron
        // Desactivem el check de IsSpeaking al CameraSwitcher durant aquest pas
        droneMovement.UnfreezeFromTutorialPosition();
        yield return SpeakAndWaitForButton(droneEntry, () =>
            player.CurrentViewMode == PlayerStateMachine.PlayerViewMode.DroneView,
            () => player.canUseDrone = true);

        // ── FASE 2 — dron ─────────────────────────────────────────────────────
        yield return new WaitForSeconds(0.5f);

        cameraSwitcher.droneMoveBlocked = true;

        // droneMoveEntry: inputUnlocksAtLine apunta a "Con el joystick izquierdo..."
        // El reset de posición ocurre en onUnlock, DESPUÉS de las frases informativas
        yield return SpeakDroneHUDEntry(droneMoveEntry,
             () => Vector3.Distance(droneMovement.transform.position, _droneStartPos) > 1f,
             () => {
                 _droneStartPos = droneMovement.transform.position;
                 cameraSwitcher.droneMoveBlocked = false; // desbloquea en unlockAt
             });

        yield return SpeakDroneHUDEntry(droneFOVEntry,
            () => player.CurrentViewMode == PlayerStateMachine.PlayerViewMode.OrthographicView,
            () => player.canUseOrtho = true);

        // ── FASE 3 — ortho ────────────────────────────────────────────────────
        yield return new WaitForSeconds(0.3f);

        // orthoMoveEntry: inputUnlocksAtLine = -1, todas informativas con A
        // onUnlock desbloquea el cursor en la línea que marques
        if (orthoMoveEntry != null)
            yield return SpeakDroneHUDEntry(orthoMoveEntry, () => true,
                () => player.canMoveOrthoCursor = true); // solo desbloquea el cursor ortho

        Vector3 playerPosBeforeAccept = playerTransform.position;
        yield return SpeakDroneHUDEntry(orthoAcceptEntry,
            () => Vector3.Distance(playerTransform.position, playerPosBeforeAccept) > 1.5f,
            () => StartCoroutine(UnlockInteractNextFrame()));

        yield return new WaitForSeconds(0.3f);

        // Esperamos que el jugador vuelva al dron (tras aceptar posición va a ThirdPerson)
        /*yield return new WaitUntil(() =>
            player.CurrentViewMode == PlayerStateMachine.PlayerViewMode.DroneView ||
            !CameraSwitcher.IsOrthoMode);*/

        // Si volvió al dron y sigue en ortho, explicamos cómo desaplanar

        yield return SpeakDroneHUDEntry(orthoExitEntry,
             () => !CameraSwitcher.IsOrthoMode,
             () => player.canExitDrone = true);

        // ── FASE 4 — sortir dron ──────────────────────────────────────────────
        yield return new WaitForSeconds(0.5f);
        droneHUD.HideTutorialPanel();

        yield return SpeakDroneHUDEntry(exitDroneEntry,
            () => player.CurrentViewMode == PlayerStateMachine.PlayerViewMode.ThirdPerson);

        yield return new WaitForSeconds(1.5f);
        droneHUD.HideTutorialPanel();

        // FASE 5
        yield return new WaitForSeconds(0.5f);
        yield return SpeakAndWaitForButton(cursorEntry,
            () => FindFirstObjectByType<MinionCursor>()?.IsActive == true,
            () => player.canUseCursor = true);

        yield return SpeakAndWaitForButton(cursorHoldEntry,
            () => FindFirstObjectByType<MinionCursor>()?.CylinderIsActive == true);

        // Fi — informatiu, espera A
        yield return SpeakWorldSpace(tutorialEndEntry);
        TutorialManager.Instance?.TriggerIfNew("hasUsedParticles", () => { });
        UnlockAll();
    }

    public void UnlockAll()
    {
        player.canMove = true;
        player.canUseDrone = true;
        player.canUseOrtho = true;
        player.canInteract = true;
        player.canExitDrone = true;
        player.canUseCursor = true;
        player.canMoveOrthoCursor = true;
        TutorialManager.Instance?.CompleteTutorial();
        droneMovement?.UnfreezeFromTutorialPosition();
        if (visionDetector != null) visionDetector.tutorialActive = false;
        isTutorialCompleted = true;
    }

    private IEnumerator UnlockInteractNextFrame()
    {
        yield return null; // espera un frame para que el A actual no dispare canInteract
        player.canInteract = true;
    }
}