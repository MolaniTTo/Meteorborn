using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerVisionDetector : MonoBehaviour
{
    [Header("Detecció")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float detectionAngle = 60f;
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private LayerMask occlusionMask;

    [Header("Tutorials — primera vegada")]
    [SerializeField] private TutorialEntry tutorialFirstEnemy; 
    [SerializeField] private TutorialEntry tutorialFirstMinion;
    [SerializeField] private TutorialEntry tutorialFirstStatue;
    [SerializeField] private TutorialEntry tutorialFirstCohet;
    [SerializeField] private TutorialEntry tutorialFirstBalanca;

    [Header("Tutorials — informatiu (botó ajuda)")]
    [SerializeField] private TutorialEntry infoEnemy;
    [SerializeField] private TutorialEntry infoMinion;
    [SerializeField] private TutorialEntry infoStatue;
    [SerializeField] private TutorialEntry infoBalanca;
    // Input
    private InputSystem_Actions inputActions;

    // Referència al player per saber el mode
    private PlayerStateMachine playerStateMachine;

    public bool tutorialActive = false;

    void Awake()
    {
        playerStateMachine = GetComponent<PlayerStateMachine>();
        inputActions = new InputSystem_Actions();
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Help.performed += OnHelpPressed; // assigna "Cursor Down" al Help action al Input Actions asset
        StartCoroutine(DetectionLoop());
    }

    void OnDisable()
    {
        inputActions.Player.Help.performed -= OnHelpPressed;
        inputActions.Player.Disable();
        StopAllCoroutines();
    }

    // ── Loop de detecció automàtica ───────────────────────────────────────────

    private IEnumerator DetectionLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            if (!tutorialActive &&
                playerStateMachine.CurrentViewMode == PlayerStateMachine.PlayerViewMode.ThirdPerson)
                CheckVision();
            yield return wait;
        }
    }

    private void CheckVision()
    {
        bool seesEnemy = SeesObjectOfType<EnemicAI>();
        bool seesMinion = SeesObjectOfType<MinionAI>();
        bool seesStatue = SeesObjectOfType<StatueSavePoint>();
        bool seesCohet = SeesObjectOfType<CarryObject>();
        bool seesBalanca = SeesObjectOfType<BalancaPuzzle>();

        if (seesEnemy)
        {
            TutorialManager.Instance?.TriggerIfNew("hasSeenEnemy", () =>
                DroneSpeaker.Instance?.Speak(tutorialFirstEnemy));
        }

        if (seesMinion)
        {
            TutorialManager.Instance?.TriggerIfNew("hasSeenMinion", () =>
                DroneSpeaker.Instance?.Speak(tutorialFirstMinion));
        }

        if (seesStatue)
        {
            TutorialManager.Instance?.TriggerIfNew("hasSeenStatue", () =>
                DroneSpeaker.Instance?.Speak(tutorialFirstStatue));
        }
        if (seesCohet)
        {
            TutorialManager.Instance?.TriggerIfNew("hasSeenCohet", () =>
                DroneSpeaker.Instance?.Speak(tutorialFirstCohet));
        }
        if (seesBalanca)
        {
            TutorialManager.Instance?.TriggerIfNew("hasSeenBalanca", () =>
                DroneSpeaker.Instance?.Speak(tutorialFirstBalanca));
        }
    }

    // ── Botó d'ajuda ──────────────────────────────────────────────────────────

    private void OnHelpPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (tutorialActive) return;
        if (DroneSpeaker.Instance != null && DroneSpeaker.Instance.IsSpeaking) return;

        bool seesEnemy = SeesObjectOfType<EnemicAI>();
        bool seesMinion = SeesObjectOfType<MinionAI>();
        bool seesStatue = SeesObjectOfType<StatueSavePoint>();
        bool seesBalanca = SeesObjectOfType<BalancaPuzzle>();

        if (seesEnemy) { DroneSpeaker.Instance?.Speak(infoEnemy); }
        else if (seesMinion) { DroneSpeaker.Instance?.Speak(infoMinion); }
        else if (seesStatue) { DroneSpeaker.Instance?.Speak(infoStatue); }
        else if (seesBalanca) { DroneSpeaker.Instance?.Speak(infoBalanca); }

    }

    // ── Utilitat ──────────────────────────────────────────────────────────────

    private bool SeesObjectOfType<T>() where T : MonoBehaviour
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (Collider col in hits)
        {
            T component = col.GetComponent<T>();
            if (component == null) continue;

            Vector3 dir = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dir);
            if (angle >= detectionAngle) continue;

            // Comprovació de línia de visió — si hi ha obstacle entre el player i l'objecte, ignora'l
            float distance = Vector3.Distance(transform.position, col.transform.position);
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, distance, occlusionMask))
                continue; // hi ha alguna cosa al mig

            return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}