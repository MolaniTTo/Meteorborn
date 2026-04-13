using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class MinionCursor : MonoBehaviour
{
    [Header("Referència a la càmera i al player")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform playerTransform;

    [Header("Moviment del cursor")]
    [SerializeField] private float cursorMoveSpeed = 8f;
    [SerializeField] private float cursorSmoothTime = 0.08f;

    [Header("Visual")]
    [SerializeField] private GameObject cursorVisual;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int linePoints = 8;

    [Header("Càmera en mode cursor (Cinemachine)")]
    [SerializeField] private CinemachineInputAxisController cinemachineInput;

    // ── Estat intern ──────────────────────────────────────────────────────────
    private bool isCursorActive = false;
    private Vector3 cursorWorldPosition;
    private Vector3 cursorVelocity;

    // ── Input ─────────────────────────────────────────────────────────────────
    private InputSystem_Actions inputActions;
    private InputAction lookAction;
    private InputAction leftTrigger;    // LT → activa mode cursor
    private InputAction rightTrigger;   // RT → confirma acció (activar / llançar)

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        lookAction = inputActions.Player.Look;
        leftTrigger = inputActions.Player.CursorMode;     // nova acció al teu Input Asset
        rightTrigger = inputActions.Player.LaunchMinion;   // nova acció al teu Input Asset
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        rightTrigger.performed += OnRightTriggerPerformed;
    }

    void OnDisable()
    {
        rightTrigger.performed -= OnRightTriggerPerformed;
        inputActions.Player.Disable();
    }

    void Update()
    {
        bool ltHeld = leftTrigger.ReadValue<float>() > 0.5f;

        if (ltHeld && !isCursorActive) EnterCursorMode();
        if (!ltHeld && isCursorActive) ExitCursorMode();

        if (isCursorActive)
        {
            UpdateCursorPosition();
            UpdateLineRenderer();
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
        if (lineRenderer != null) { lineRenderer.enabled = true; lineRenderer.positionCount = linePoints; }
    }

    private void ExitCursorMode()
    {
        isCursorActive = false;
        if (cinemachineInput != null) cinemachineInput.enabled = true;
        if (cursorVisual != null) cursorVisual.SetActive(false);
        if (lineRenderer != null) lineRenderer.enabled = false;
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

    // ── RT: acció unificada ───────────────────────────────────────────────────
    // MinionManager decideix si activar/reactivar un minion o llançar-ne un

    private void OnRightTriggerPerformed(InputAction.CallbackContext ctx)
    {
        if (!isCursorActive) return;
        MinionManager.Instance?.ConfirmCursorAction(cursorWorldPosition, playerTransform.position);
    }

    // ── Propietats públiques ──────────────────────────────────────────────────

    public Vector3 WorldPosition => cursorWorldPosition;
    public bool IsActive => isCursorActive;
}