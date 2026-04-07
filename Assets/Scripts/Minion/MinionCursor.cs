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
    [SerializeField] private float cursorMoveSpeed = 8f;     // velocitat de desplaçament pel terreny
    [SerializeField] private float cursorSmoothTime = 0.1f;  // suavitat

    [Header("Visual")]
    [SerializeField] private GameObject cursorVisual;        // el mesh/prefab del cursor
    [SerializeField] private LineRenderer lineRenderer;      // línia del jugador al cursor (opcional)
    [SerializeField] private int linePoints = 8;

    [Header("Càmera en mode cursor")]
    [SerializeField] private CinemachineInputAxisController cinemachineInput;
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;

    // ── Estat intern ──────────────────────────────────────────────────────────
    private bool isCursorActive = false;
    private Vector3 cursorWorldPosition;
    private Vector2 currentScreenPos;

    // ── Input ─────────────────────────────────────────────────────────────────
    private InputSystem_Actions inputActions;
    private InputAction lookAction;       // joystick dret (moviment cursor)
    private InputAction leftTrigger;      // LT → activa cursor
    private InputAction rightTrigger;     // RT → llança minion

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        // Adapta els noms d'acció als teus Input Action Assets
        lookAction = inputActions.Player.Look;
        leftTrigger = inputActions.Player.CursorMode;   // LT → crea una acció "CursorMode" al teu asset
        rightTrigger = inputActions.Player.LaunchMinion; // RT → crea una acció "LaunchMinion" al teu asset
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        rightTrigger.performed += OnLaunchPerformed;
    }

    void OnDisable()
    {
        rightTrigger.performed -= OnLaunchPerformed;
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
            //UpdateCameraLock();
            UpdateLineRenderer();

            // Cada frame que LT és mantingut, intenta activar minions a prop del cursor
            MinionManager.Instance?.TryActivateMinionsAtCursor(cursorWorldPosition);
        }
    }

    // ── Entrada / sortida del mode cursor ────────────────────────────────────

    private void EnterCursorMode()
    {
        isCursorActive = true;

        // Inicializa en la posición del jugador sobre el NavMesh
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

    // ── Moviment del cursor amb joystick dret ────────────────────────────────

    private Vector3 cursorVelocity;
    private void UpdateCursorPosition()
    {
        Vector2 stickInput = lookAction.ReadValue<Vector2>();
        if (stickInput.magnitude < 0.15f) stickInput = Vector2.zero;

        // Direcciones basadas en la cámara (fija cuando cursor está activo)
        Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        // Candidato: mueve solo si hay input
        Vector3 candidatePos = cursorWorldPosition;
        if (stickInput != Vector2.zero)
        {
            candidatePos += (camForward * stickInput.y + camRight * stickInput.x)
                            * cursorMoveSpeed * Time.deltaTime;
        }

        // Intenta samplear NavMesh en el candidato
        if (NavMesh.SamplePosition(candidatePos, out NavMeshHit navHit, 0.5f, NavMesh.AllAreas))
        {
            // Solo avanza si el punto está en NavMesh
            cursorWorldPosition = navHit.position;
        }
        // Si no hay NavMesh, cursorWorldPosition NO cambia → cursor bloqueado en el borde

        transform.position = Vector3.SmoothDamp(transform.position, cursorWorldPosition, ref cursorVelocity, 0.08f);
    }


    // ── Línia del jugador al cursor ───────────────────────────────────────────

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null || playerTransform == null) return;
        for (int i = 0; i < linePoints; i++)
        {
            float t = (float)i / (linePoints - 1);
            Vector3 linePos = Vector3.Lerp(playerTransform.position + Vector3.up * 0.5f,
                                           cursorWorldPosition, t);
            // Arc parabòlic a la línia
            linePos.y += Mathf.Sin(Mathf.PI * t) * 1.5f;
            lineRenderer.SetPosition(i, linePos);
        }
    }

    // ── RT: llança el minion més proper ──────────────────────────────────────

    private void OnLaunchPerformed(InputAction.CallbackContext ctx)
    {
        if (!isCursorActive) return;
        MinionManager.Instance?.LaunchMinionToCursor(cursorWorldPosition, playerTransform.position);
    }

    // ── Propietat pública ─────────────────────────────────────────────────────

    public Vector3 WorldPosition => cursorWorldPosition;
    public bool IsActive => isCursorActive;
}