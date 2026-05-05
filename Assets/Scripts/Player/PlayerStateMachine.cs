using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class PlayerStateMachine : MonoBehaviour
{
    // ── Enums ─────────────────────────────────────────────────────────────────
    public enum PlayerViewMode { ThirdPerson, OrthographicView }
    public enum PlayerState { Idle, Walking, OrthoIdle, OrthoMoving, LockOnIdle, LockOnMoving }
    public PlayerViewMode CurrentViewMode => playerViewMode;

    // ── Input System ──────────────────────────────────────────────────────────
    private InputSystem_Actions inputActions;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction confirmAction;
    private InputAction leftTrigger;    // LT → detecta si el cursor/lock-on és actiu

    // ── Variables d'input ─────────────────────────────────────────────────────
    private Vector2 moveInput;
    private Vector2 lookInput;

    // ── Estat ─────────────────────────────────────────────────────────────────
    private PlayerViewMode playerViewMode;
    private PlayerState currentState;

    // ── Components ────────────────────────────────────────────────────────────
    [Header("Components")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    private static readonly int ThrowTrigger = Animator.StringToHash("Throw");
    private static readonly int ThrowLayerIndex = 1;
    private bool isThrowingAnimation = false;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Ortho Settings")]
    [SerializeField] private OrthoCursor orthoCursor;
    [SerializeField] private float linkSpeed = 8f;
    [SerializeField] private float arrivalDistance = 0.3f;

    [Header("Lock-On Orbital Movement")]
    [SerializeField] private float orbitalSpeed = 3f;       // velocitat de strafe al voltant de l'enemic
    [SerializeField] private float orbitalMinDist = 2f;     // distància mínima a l'enemic (s'hi queda)
    [SerializeField] private float orbitalMaxDist = 8f;     // distància màxima (s'hi apropa)
    [SerializeField] private float lockOnRotationSpeed = 8f;// velocitat de gir cap a l'enemic

    [Header("Transforms")]
    public Transform playerFollowPosition;
    public Transform playerThrowPosition;

    [Header("Minion Cursor")]
    [SerializeField] private MinionCursor minionCursor;

    // ── NavMesh Link ──────────────────────────────────────────────────────────
    [SerializeField] private bool traversingLink = false;

    // ── Light Particles ───────────────────────────────────────────────────────
    [SerializeField] private int MaxParticles;
    [SerializeField] private int numberOfParticles;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        moveAction = inputActions.Player.Move;
        lookAction = inputActions.Player.Look;
        confirmAction = inputActions.Player.Confirm;
        leftTrigger = inputActions.Player.CursorMode;

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.speed = moveSpeed;
        agent.autoTraverseOffMeshLink = false;
        agent.acceleration = 99999f;
        agent.angularSpeed = 99999f;
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        confirmAction.performed += OnConfirmPerformed;

        // Subscriu als events del LockOnSystem
        if (LockOnSystem.Instance != null)
        {
            LockOnSystem.Instance.OnLockOnAcquired += OnLockOnAcquired;
            LockOnSystem.Instance.OnLockOnLost += OnLockOnLost;
        }
    }

    void OnDisable()
    {
        confirmAction.performed -= OnConfirmPerformed;
        inputActions.Player.Disable();

        if (LockOnSystem.Instance != null)
        {
            LockOnSystem.Instance.OnLockOnAcquired -= OnLockOnAcquired;
            LockOnSystem.Instance.OnLockOnLost -= OnLockOnLost;
        }
    }

    // ── LockOn callbacks ──────────────────────────────────────────────────────

    private void OnLockOnAcquired(Transform target)
    {
        if (playerViewMode != PlayerViewMode.ThirdPerson) return;
        currentState = PlayerState.LockOnIdle;
    }

    private void OnLockOnLost()
    {
        if (currentState == PlayerState.LockOnIdle || currentState == PlayerState.LockOnMoving)
            currentState = PlayerState.Idle;
    }

    // ── Confirm (Ortho) ───────────────────────────────────────────────────────

    private void OnConfirmPerformed(CallbackContext ctx)
    {
        if (playerViewMode != PlayerViewMode.OrthographicView) return;
        if (traversingLink) return;

        switch (orthoCursor.CurrentMode)
        {
            case OrthoCursor.CursorMode.NavMesh:
                agent.SetDestination(orthoCursor.WorldPosition);
                currentState = PlayerState.OrthoMoving;
                break;
            case OrthoCursor.CursorMode.Free:
                TryInteract();
                break;
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(orthoCursor.WorldPosition + Vector3.up * 5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            if (hit.collider.CompareTag("Interactable"))
                GameManager.instance.HandleInteraction(hit.collider.gameObject);
            else
                Debug.Log("No hi ha cap objecte interactuable aqui");
        }
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    void Start()
    {
        playerViewMode = PlayerViewMode.ThirdPerson;
        currentState = PlayerState.Idle;
        if (orthoCursor != null) orthoCursor.SetActive(false);
    }

    // ── SetViewMode ───────────────────────────────────────────────────────────

    public void SetViewMode(PlayerViewMode mode)
    {
        playerViewMode = mode;

        if (mode == PlayerViewMode.OrthographicView)
        {
            currentState = PlayerState.OrthoIdle;
            agent.ResetPath();
            agent.speed = moveSpeed;
            if (orthoCursor != null)
            {
                orthoCursor.transform.position = transform.position;
                orthoCursor.SetActive(true);
            }
        }
        else
        {
            currentState = PlayerState.Idle;
            if (orthoCursor != null) orthoCursor.SetActive(false);
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        ProcessInputActions();

        switch (currentState)
        {
            case PlayerState.Idle: HandleIdle(); break;
            case PlayerState.Walking: HandleWalking(); break;
            case PlayerState.OrthoIdle: HandleOrthoIdle(); break;
            case PlayerState.OrthoMoving: HandleOrthoMoving(); break;
            case PlayerState.LockOnIdle: HandleLockOnIdle(); break;
            case PlayerState.LockOnMoving: HandleLockOnMoving(); break;
        }

        if (agent.isOnOffMeshLink && !agent.pathPending && !traversingLink)
            StartCoroutine(TraverseLink());

        UpdateAnimator();

        if (minionCursor != null && minionCursor.IsActive)
        {
            FaceTowardsCursor();
        }
    }

    // ── ProcessInputActions ───────────────────────────────────────────────────

    private void ProcessInputActions()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        if (playerViewMode == PlayerViewMode.ThirdPerson)
        {
            bool lockedOn = LockOnSystem.Instance != null && LockOnSystem.Instance.IsLockedOn;

            if (lockedOn)
            {
                // Mode lock-on orbital
                currentState = moveInput.sqrMagnitude > 0.01f
                    ? PlayerState.LockOnMoving
                    : PlayerState.LockOnIdle;
            }
            else
            {
                // Mode normal (LT cursor o lliure)
                if (currentState == PlayerState.LockOnIdle || currentState == PlayerState.LockOnMoving)
                    currentState = PlayerState.Idle;

                currentState = moveInput.sqrMagnitude > 0.01f
                    ? PlayerState.Walking
                    : PlayerState.Idle;
            }
        }

        if (playerViewMode == PlayerViewMode.OrthographicView && orthoCursor != null)
            orthoCursor.SetMoveInput(moveInput);
    }

    // ── States ────────────────────────────────────────────────────────────────

    private void HandleIdle()
    {
        agent.SetDestination(transform.position);
    }

    private void HandleWalking()
    {
        Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();
        Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;

        agent.SetDestination(transform.position + moveDirection * moveSpeed);

        if (moveDirection.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDirection),
                rotationSpeed * Time.deltaTime);
    }

    private void HandleOrthoIdle()
    {
        agent.SetDestination(transform.position);
    }

    private void HandleOrthoMoving()
    {
        Vector3 dir = agent.destination - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                rotationSpeed * Time.deltaTime);

        if (!agent.pathPending && agent.remainingDistance <= arrivalDistance)
        {
            currentState = PlayerState.OrthoIdle;
            agent.ResetPath();
        }
    }

    // ── Lock-On Orbital (estil Zelda) ─────────────────────────────────────────

    private void HandleLockOnIdle()
    {
        if (LockOnSystem.Instance == null || !LockOnSystem.Instance.IsLockedOn) return;
        Transform target = LockOnSystem.Instance.Target;

        FaceTarget(target);
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    private void HandleLockOnMoving()
    {
        if (LockOnSystem.Instance == null || !LockOnSystem.Instance.IsLockedOn) return;
        Transform target = LockOnSystem.Instance.Target;

        FaceTarget(target);

        Vector3 toEnemy = target.position - transform.position;
        toEnemy.y = 0f;
        float currentDist = toEnemy.magnitude;
        Vector3 dirToEnemy = toEnemy.normalized;

        Vector3 strafeDir = Vector3.Cross(Vector3.up, dirToEnemy);
        Vector3 lateralMove = strafeDir * moveInput.x * orbitalSpeed;

        float forwardInput = moveInput.y;
        Vector3 radialMove = Vector3.zero;

        if (forwardInput > 0 && currentDist > orbitalMinDist)
            radialMove = dirToEnemy * forwardInput * orbitalSpeed;
        else if (forwardInput < 0 && currentDist < orbitalMaxDist)
            radialMove = dirToEnemy * forwardInput * orbitalSpeed;

        Vector3 desiredVelocity = lateralMove + radialMove;

        // Assegura distància mínima
        Vector3 nextPos = transform.position + desiredVelocity * Time.deltaTime;
        Vector3 newToEnemy = target.position - nextPos; newToEnemy.y = 0f;
        if (newToEnemy.magnitude < orbitalMinDist)
            desiredVelocity = Vector3.zero;

        // Mou directament via agent.velocity, sense SetDestination
        agent.ResetPath();
        agent.velocity = desiredVelocity;
    }

    private void FaceTarget(Transform target)
    {
        Vector3 dir = target.position - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                lockOnRotationSpeed * Time.deltaTime);
    }

    // ── OffMeshLink Traversal ─────────────────────────────────────────────────

    private IEnumerator TraverseLink()
    {
        traversingLink = true;
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 finalDestination = agent.destination;
        float realDistance = Vector3.Distance(data.startPos, data.endPos);
        float requiredSpeed = realDistance / (2f / moveSpeed);

        agent.speed = requiredSpeed;
        agent.autoTraverseOffMeshLink = true;
        while (agent.isOnOffMeshLink) yield return null;

        agent.velocity = Vector3.zero;
        agent.speed = moveSpeed;
        agent.autoTraverseOffMeshLink = false;
        agent.SetDestination(finalDestination);
        traversingLink = false;
    }

    private void UpdateAnimator()
    {
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }

    public void PlayThrowAnimation()
    {
        if (isThrowingAnimation) return;
        StartCoroutine(ThrowAnimationRoutine());
    }

    private IEnumerator ThrowAnimationRoutine()
    {
        isThrowingAnimation = true;

        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            animator.SetLayerWeight(ThrowLayerIndex, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        animator.SetLayerWeight(ThrowLayerIndex, 1f);

        animator.SetTrigger(ThrowTrigger);

        yield return new WaitForSeconds(0.5f);

        MinionManager.Instance?.ExecutePendingLaunch();

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            animator.SetLayerWeight(ThrowLayerIndex, Mathf.Clamp01(1f - elapsed / duration));
            yield return null;
        }
        animator.SetLayerWeight(ThrowLayerIndex, 0f);

        isThrowingAnimation = false;
    }


    private void FaceTowardsCursor()
    {
        Vector3 dir = minionCursor.WorldPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                rotationSpeed * Time.deltaTime);
    }



    // ── Input callbacks (legacy, per compatibilitat) ──────────────────────────
    private void OnMove(CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnLook(CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
}