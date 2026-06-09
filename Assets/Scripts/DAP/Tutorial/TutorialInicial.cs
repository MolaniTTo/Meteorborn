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
    [SerializeField] private string[] droneMoveLines;
    [SerializeField] private string[] droneFOVLines;

    [Header("Línies de tutorial — Fase 3 (ortho)")]
    [SerializeField] private string[] orthoMoveLines;
    [SerializeField] private string[] orthoAcceptLines;
    [SerializeField] private string[] orthoExitLines;

    [Header("Línies de tutorial — Fase 4 (sortir dron)")]
    [SerializeField] private string[] exitDroneLines;

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
        yield return new WaitUntil(() => DroneSpeaker.Instance != null && !DroneSpeaker.Instance.IsSpeaking);
    }

    // Parla i espera que el jugador faci una acció — tanca sol quan es compleix
    private IEnumerator SpeakAndWaitForButton(TutorialEntry entry, System.Func<bool> condition)
    {
        if (entry == null) yield break;
        DroneSpeaker.Instance?.Speak(entry);

        // Espera 2 frames per assegurar que IsTyping s'ha activat
        yield return null;
        yield return null;
        yield return new WaitUntil(() => DroneSpeaker.Instance != null && !DroneSpeaker.Instance.IsTyping);

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
        player.canMove = true;
        yield return SpeakAndWaitForButton(moveEntry, () => player.IsMoving);
        yield return new WaitForSeconds(0.5f);

        // Entrar al dron: es tanca quan entra al dron
        // Desactivem el check de IsSpeaking al CameraSwitcher durant aquest pas
        droneMovement.UnfreezeFromTutorialPosition();
        player.canUseDrone = true;
        yield return SpeakAndWaitForButton(droneEntry, () =>
            player.CurrentViewMode == PlayerStateMachine.PlayerViewMode.DroneView);

        // ── FASE 2 — dron ─────────────────────────────────────────────────────
        yield return new WaitForSeconds(0.5f);

        // Moure el dron: es tanca quan s'ha mogut
        _droneStartPos = droneMovement.transform.position;
        yield return SpeakDroneHUDAndWait(droneMoveLines.Length > 0 ? droneMoveLines[0] : "Mou el dron", () =>
            Vector3.Distance(droneMovement.transform.position, _droneStartPos) > 1f);

        // Informació extra del dron si n'hi ha (amb A)
        if (droneMoveLines.Length > 1)
        {
            var extra = new System.Collections.Generic.List<string>();
            for (int i = 1; i < droneMoveLines.Length; i++) extra.Add(droneMoveLines[i]);
            yield return SpeakDroneHUD(extra.ToArray());
        }

        // Posicionar dron i aplanar: el text es tanca quan el jugador aplana amb B
        // No separem FOV i aplanar — el text es queda fins que realment aplana
        player.canUseOrtho = true;
        yield return SpeakDroneHUDAndWait(
            droneFOVLines.Length > 0 ? droneFOVLines[0] : "Posiciona el dron mirant cap a tu i prem B per aplanar",
            () => player.CurrentViewMode == PlayerStateMachine.PlayerViewMode.OrthographicView);

        // ── FASE 3 — ortho ────────────────────────────────────────────────────

        yield return new WaitForSeconds(0.3f);

        // Explicació del cursor (amb A, informatiu)
        if (orthoMoveLines.Length > 0)
            yield return SpeakDroneHUD(orthoMoveLines);

        // Acceptar posició: es tanca quan el player s'ha mogut a la posició
        // La condició és que el player surti d'ortho (va a ThirdPerson després d'acceptar)
        player.canInteract = true;
        Vector3 playerPosBeforeAccept = playerTransform.position;
        yield return SpeakDroneHUDAndWait(
            orthoAcceptLines.Length > 0 ? orthoAcceptLines[0] : "Prem A per acceptar la posició",
            () => Vector3.Distance(playerTransform.position, playerPosBeforeAccept) > 1.5f);

        yield return new WaitForSeconds(0.3f);

        // Desaplanar: es tanca quan surt d'ortho
        // Primer hem de tornar al dron des de ThirdPerson si cal
        // El jugador ha d'estar en DroneView per poder desaplanar
        // Esperem que torni al dron si estava en ThirdPerson
        if (player.CurrentViewMode == PlayerStateMachine.PlayerViewMode.ThirdPerson)
        {
            // El jugador va acceptar posició i va tornar a ThirdPerson
            // Ara li diem que torni al dron per desaplanar
            player.canUseDrone = true; // ja estava actiu
            yield return SpeakDroneHUDAndWait(
                "Torna al dron per desaplanar la imatge",
                () => player.CurrentViewMode == PlayerStateMachine.PlayerViewMode.DroneView
                   || !CameraSwitcher.IsOrthoMode);
        }

        // Desaplanar
        player.canExitDrone = true;
        if (orthoExitLines.Length > 0)
            yield return SpeakDroneHUDAndWait(orthoExitLines[0], () => !CameraSwitcher.IsOrthoMode);

        // FASE 4 — sortir dron
        yield return new WaitForSeconds(0.5f);
        DroneSpeaker.Instance?.ForceClose();
        droneHUD.HideTutorialPanel();

        yield return SpeakDroneHUDAndWait(
            exitDroneLines.Length > 0 ? exitDroneLines[0] : "Prem X per sortir del dron",
            () => player.CurrentViewMode == PlayerStateMachine.PlayerViewMode.ThirdPerson);

        // Espera llarga per assegurar que la transició ha acabat del tot
        yield return new WaitForSeconds(1.5f);
        DroneSpeaker.Instance?.ForceClose();

        // FASE 5
        yield return new WaitForSeconds(0.5f);
        player.canUseCursor = true;

        yield return SpeakAndWaitForButton(cursorEntry, () =>
            FindFirstObjectByType<MinionCursor>()?.IsActive == true);

        yield return SpeakAndWaitForButton(cursorHoldEntry, () =>
            FindFirstObjectByType<MinionCursor>()?.CylinderIsActive == true);

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
        TutorialManager.Instance?.CompleteTutorial();
        droneMovement?.UnfreezeFromTutorialPosition();
        if (visionDetector != null) visionDetector.tutorialActive = false;
        isTutorialCompleted = true;
    }
}