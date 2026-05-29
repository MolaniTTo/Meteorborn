using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class MinionCursor : MonoBehaviour
{
    [Header("Referència a la càmera i al player")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform playerThrow;

    [Header("Moviment del cursor")]
    [SerializeField] private float cursorMoveSpeed = 8f;
    [SerializeField] private float cursorSmoothTime = 0.08f;

    [Header("Visual")]
    [SerializeField] private GameObject cursorVisual;
    [SerializeField] private GameObject targetVisual;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int linePoints = 8;

    [Header("Càmera en mode cursor (Cinemachine)")]
    [SerializeField] private CinemachineInputAxisController cinemachineInput;

    [Header("Referència a la Cam i al Player")]
    [SerializeField] private PlayerStateMachine playerStateMachine;

    [Header("Visual")]
    public Transform visualCylinder;
    public Transform target = default;
    [SerializeField] private Vector3 targetOffset = Vector3.zero;

    // ── Estat intern ──────────────────────────────────────────────────────────
    private bool isCursorActive = false;
    private bool isCylinderActive = false;
    private bool isOnLockMode = false; //per saber quan esta amb un enemic lockejat
    private Vector3 cursorWorldPosition;
    private Vector3 cursorVelocity;

    // ── RT Hold ───────────────────────────────────────────────────────────────
    private bool isRTHeld = false; // Per saber si RT està sent mantingut per activar el mode de recollir/activar minions
    private float rtHoldTimer = 0f; // Temporitzador per controlar el temps que RT ha estat mantingut
    [SerializeField] private float rtHoldThreshold = 0.5f; // Temps que RT ha de ser mantingut per activar el mode de recollir/activar minions
    [SerializeField] private float cylinderRadius = 5f;
    public bool CylinderIsActive => isCylinderActive;
    public float CylinderRadius => cylinderRadius;

    // ── Input ─────────────────────────────────────────────────────────────────
    private InputSystem_Actions inputActions;
    private InputAction lookAction;
    private InputAction leftTrigger;    // LT → activa mode cursor
    private InputAction rightTrigger;   // RT → confirma acció (activar / llançar)

    public Vector3 PlayerThrowPosition => playerThrow.position;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        lookAction = inputActions.Player.Look;
        leftTrigger = inputActions.Player.CursorMode;     // nova acció al teu Input Asset
        rightTrigger = inputActions.Player.LaunchMinion;   // nova acció al teu Input Asset
        if (cursorVisual != null) cursorVisual.SetActive(false);
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (targetVisual != null) targetVisual.SetActive(false);
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        rightTrigger.started += OnRightTriggerStarted;  
        rightTrigger.canceled += OnRightTriggerReleased; 
    }

    void OnDisable()
    {
        rightTrigger.started -= OnRightTriggerStarted;
        rightTrigger.canceled -= OnRightTriggerReleased;
        inputActions.Player.Disable();
    }

    void Update()
    {
        if(playerStateMachine != null && playerStateMachine.CurrentViewMode == PlayerStateMachine.PlayerViewMode.OrthographicView)
        {
            if(isCursorActive) ExitCursorMode();
            return;
        }

        bool ltHeld = leftTrigger.ReadValue<float>() > 0.5f;
        bool hasLockOn = LockOnSystem.Instance != null && LockOnSystem.Instance.IsLockedOn;


        if (ltHeld && !hasLockOn && !isCursorActive)
        {
            EnterCursorMode();
        }

        else if ((!ltHeld || hasLockOn) && isCursorActive)
        {
            ExitCursorMode();
        }

        if (isCursorActive)
        {
            UpdateCursorPosition();
            UpdateLineRenderer();
            visualCylinder.transform.position = target.position;

            if (isRTHeld)
            {
                rtHoldTimer += Time.deltaTime;
                if(rtHoldTimer >= rtHoldThreshold && !isCylinderActive) //si el RT ha estat mantingut prou temps i el cilindre no està actiu, l'activem
                {
                    ActivateCylinder();
                }
            }
        }

    }

    // ── Mode cursor ───────────────────────────────────────────────────────────

    private void EnterCursorMode()
    {
        isCursorActive = true;

        if (NavMesh.SamplePosition(playerTransform.position, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            cursorWorldPosition = navHit.position;
        else
            cursorWorldPosition = playerTransform.position;

        transform.position = cursorWorldPosition;

        if (cinemachineInput != null) cinemachineInput.enabled = false;
        if (cursorVisual != null) cursorVisual.SetActive(true);
        if (targetVisual != null) targetVisual.SetActive(true);
        if (lineRenderer != null) { lineRenderer.enabled = true; lineRenderer.positionCount = linePoints; }
    }

    private void ExitCursorMode()
    {
        isCursorActive = false;
        if (cinemachineInput != null) cinemachineInput.enabled = true;
        if (cursorVisual != null) cursorVisual.SetActive(false);
        if (targetVisual != null) targetVisual.SetActive(false);
        if (lineRenderer != null) lineRenderer.enabled = false;

        if (isCylinderActive) DeactivateCylinder();
    }

    private void ActivateCylinder()
    {
        isCylinderActive = true;
        Debug.Log("[Cursor] ActivateCylinder");
        if (visualCylinder == null) return;

        visualCylinder.DOKill();
        visualCylinder.localScale = Vector3.zero;
        visualCylinder.DOScaleX(5f, 0.3f);
        visualCylinder.DOScaleZ(5f, 0.3f);
        visualCylinder.DOScaleY(2f, 0.2f).SetDelay(0.2f);
    }

    private void DeactivateCylinder()
    {
        isCylinderActive = false;
        if (visualCylinder == null) return;

        visualCylinder.DOKill();
        visualCylinder.DOScaleX(0f, 0.2f);
        visualCylinder.DOScaleZ(0f, 0.2f);
        visualCylinder.DOScaleY(0f, 0.1f);
    }

    // ── Moviment cursor ───────────────────────────────────────────────────────

    private void UpdateCursorPosition()
    {
        Vector2 stickInput = lookAction.ReadValue<Vector2>();
        if (stickInput.magnitude < 0.15f) stickInput = Vector2.zero;

        Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        Vector3 candidatePos = cursorWorldPosition;
        if (stickInput != Vector2.zero)
            candidatePos += (camForward * stickInput.y + camRight * stickInput.x) * cursorMoveSpeed * Time.deltaTime;

        if (NavMesh.SamplePosition(candidatePos, out NavMeshHit navHit, 0.5f, NavMesh.AllAreas))
            cursorWorldPosition = navHit.position;

        transform.position = Vector3.SmoothDamp(transform.position, cursorWorldPosition, ref cursorVelocity, cursorSmoothTime);
        target.position = cursorWorldPosition + targetOffset;
        target.up = Vector3.Lerp(target.up, Vector3.up, Time.deltaTime * 10f); 
    }

    // ── Línia jugador → cursor ─────────────────────────────────────────────────

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null || playerTransform == null) return;
        for (int i = 0; i < linePoints; i++)
        {
            float t = (float)i / (linePoints - 1);
            Vector3 pos = Vector3.Lerp(playerTransform.position + Vector3.up * 0.5f, cursorWorldPosition, t);
            pos.y += Mathf.Sin(Mathf.PI * t) * 1.5f;
            lineRenderer.SetPosition(i, pos);
        }
    }

    // ── RT: press ─────────────────────────────────────────────────────────────
    private void OnRightTriggerStarted(InputAction.CallbackContext ctx)
    {
        if(!isCursorActive) return; // només ens interessa si el cursor està actiu
        isRTHeld = true;
        rtHoldTimer = 0f; // Reiniciem el temporitzador quan RT comença a ser mantingut
    }

    //── RT: release ─────────────────────────────────────────────────────────────
    private void OnRightTriggerReleased(InputAction.CallbackContext ctx)
    {
        if (!isCursorActive) { isRTHeld = false; return; }

        bool wasHold = rtHoldTimer >= rtHoldThreshold;
        isRTHeld = false;
        rtHoldTimer = 0f;

        // Sempre desactivem el cilindre al soltar, independentment de wasHold
        if (isCylinderActive) DeactivateCylinder();

        // Només llancem si va ser un click ràpid (no hold)
        // i el cilindre NO estava actiu quan es va prémer
        if (!wasHold && !isCylinderActive)
        {
            bool hasLockOn = LockOnSystem.Instance != null && LockOnSystem.Instance.IsLockedOn;
            if (hasLockOn)
            {
                Vector3 enemyPos = LockOnSystem.Instance.Target.position;
                MinionManager.Instance?.LaunchMinionToCursor(enemyPos, playerThrow.position);
            }
            else
            {
                MinionManager.Instance?.LaunchMinionToCursor(cursorWorldPosition, playerThrow.position);
            }
        }
    }

    // ── Propietats públiques ──────────────────────────────────────────────────

    public Vector3 WorldPosition => cursorWorldPosition;
    public bool IsActive => isCursorActive;
}