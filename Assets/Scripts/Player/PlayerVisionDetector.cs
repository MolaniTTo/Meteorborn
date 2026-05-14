using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerVisionDetector : MonoBehaviour
{
    [Header("Detecció")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float detectionAngle = 60f;
    [SerializeField] private float checkInterval = 0.5f;

    [Header("Tutorials — primera vegada")]
    [SerializeField] private TutorialEntry tutorialFirstEnemy;
    [SerializeField] private TutorialEntry tutorialFirstMinion;
    [SerializeField] private TutorialEntry tutorialFirstStatue;

    [Header("Tutorials — informatiu (botó ajuda)")]
    [SerializeField] private TutorialEntry infoEnemy;
    [SerializeField] private TutorialEntry infoMinion;
    [SerializeField] private TutorialEntry infoBoth;
    [SerializeField] private TutorialEntry infoStatue;

    // Input
    private InputSystem_Actions inputActions;

    // Referència al player per saber el mode
    private PlayerStateMachine playerStateMachine;

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
            // Només en ThirdPerson
            if (playerStateMachine.CurrentViewMode == PlayerStateMachine.PlayerViewMode.ThirdPerson)
                CheckVision();
            yield return wait;
        }
    }

    private void CheckVision()
    {
        bool seesEnemy = SeesObjectOfType<EnemicAI>();
        bool seesMinion = SeesObjectOfType<MinionAI>();
        bool seesStatue = SeesObjectOfType<StatueSavePoint>();

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
    }

    // ── Botó d'ajuda ──────────────────────────────────────────────────────────

    private void OnHelpPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (DroneSpeaker.Instance != null && DroneSpeaker.Instance.IsSpeaking) return;

        bool seesEnemy = SeesObjectOfType<EnemicAI>();
        bool seesMinion = SeesObjectOfType<MinionAI>();

        if (seesEnemy && seesMinion) { DroneSpeaker.Instance?.Speak(infoBoth); }
        else if (seesEnemy) { DroneSpeaker.Instance?.Speak(infoEnemy); }
        else if (seesMinion) { DroneSpeaker.Instance?.Speak(infoMinion); }
        else if (SeesObjectOfType<StatueSavePoint>()) { DroneSpeaker.Instance?.Speak(infoStatue); }
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
            if (angle < detectionAngle)
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