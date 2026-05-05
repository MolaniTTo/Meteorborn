using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gestiona el lock-on a enemics a l'estil Zelda.
/// - LB: fixa/desfixa l'enemic més proper al camp de visió
/// - Mentre LT és mantingut I hi ha lock-on actiu: moviment orbital, RT llança minions directament
/// - Si l'enemic mor o el jugador s'allunya: es desactiva sol
/// </summary>
public class LockOnSystem : MonoBehaviour
{
    public static LockOnSystem Instance { get; private set; }

    [Header("Configuració de detecció")]
    [SerializeField] private float lockOnRadius = 15f;          // distància màxima per fixar
    [SerializeField] private float lockOnAngle = 90f;           // angle de visió total (45° a cada costat)
    [SerializeField] private float breakDistance = 20f;         // distància per perdre el lock
    [SerializeField] private LayerMask enemyMask;               // layer dels enemics
    [SerializeField] private LayerMask obstacleMask;            // obstacles que bloquegen la visió

    [Header("Càmera lock-on (Cinemachine)")]
    [SerializeField] private CinemachineCamera lockOnCamera;            // càmera virtual de lock-on (prioritat alta)
    [SerializeField] private CinemachineCamera normalCamera;            // càmera normal (prioritat baixa)
    //[SerializeField] private Transform lockOnCameraTarget;      // target que la càmera de lock-on seguirà (entre player i enemic)
    [SerializeField] private float cameraHeightOffset = 1.5f;

    [Header("Visual lock-on")]
    [SerializeField] private GameObject lockOnReticle;          // indicador visual sobre l'enemic

    [Header("Referència al player")]
    [SerializeField] private Transform playerTransform;

    [Header("Referència al sistema de càmera")]
    [SerializeField] private CameraSwitcher cameraSwitcher;

    // ── Estat intern ──────────────────────────────────────────────────────────
    private Transform lockedTarget = null;
    private bool isLockedOn = false;

    // ── Input ─────────────────────────────────────────────────────────────────
    private InputSystem_Actions inputActions;
    private InputAction lockOnAction;   // LB

    // ── Events per notificar altres sistemes ──────────────────────────────────
    public System.Action<Transform> OnLockOnAcquired;   // target adquirit
    public System.Action OnLockOnLost;                   // target perdut

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inputActions = new InputSystem_Actions();
        lockOnAction = inputActions.Player.LockOn; // afegeix "LockOn" al teu Input Asset → LB
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        lockOnAction.started += OnLockOnStarted;   // LB premut → activa
        lockOnAction.canceled += OnLockOnCanceled;  // LB soltat → desactiva
    }

    void OnDisable()
    {
        lockOnAction.started -= OnLockOnStarted;
        lockOnAction.canceled -= OnLockOnCanceled;
        inputActions.Player.Disable();
    }

    private void OnLockOnStarted(InputAction.CallbackContext ctx)
    {
        if (cameraSwitcher != null && cameraSwitcher.IsOrthoMode) return;
        TryAcquireLockOn(); // LB premut → intenta fixar un enemic 
    }

    private void OnLockOnCanceled(InputAction.CallbackContext ctx)
    {
        LoseLockOn();
    }


    void Update()
    {
        if (!isLockedOn) return;

        // ── Comprova si el target segueix sent vàlid ───────────────────────
        if (lockedTarget == null)
        {
            LoseLockOn();
            return;
        }

        // Enemic mort?
        EnemyHealth eh = lockedTarget.GetComponent<EnemyHealth>();
        if (eh != null && eh.IsDead())
        {
            LoseLockOn();
            return;
        }

        // Massa lluny?
        float dist = Vector3.Distance(playerTransform.position, lockedTarget.position);
        if (dist > breakDistance)
        {
            LoseLockOn();
            return;
        }

        // ── Actualitza el target de la càmera (punt mig entre player i enemic) ──
        /*if (lockOnCameraTarget != null)
        {
            lockOnCameraTarget.position = Vector3.Lerp(
                playerTransform.position + Vector3.up * cameraHeightOffset,
                lockedTarget.position,
                0.5f);
        }*/

        // ── Actualitza el reticle visual ──────────────────────────────────────
        if (lockOnReticle != null)
            lockOnReticle.transform.position = lockedTarget.position + Vector3.up * 2.5f;
    }


    // ── Busca el millor target ────────────────────────────────────────────────

    private void TryAcquireLockOn()
    {
        Collider[] candidates = Physics.OverlapSphere(playerTransform.position, lockOnRadius, enemyMask);

        Transform bestTarget = null;
        float bestScore = float.MaxValue; // menor = millor (combinació de distància i angle)

        foreach (Collider col in candidates)
        {
            Vector3 dirToEnemy = (col.transform.position - playerTransform.position);
            float dist = dirToEnemy.magnitude;
            float angle = Vector3.Angle(playerTransform.forward, dirToEnemy.normalized);

            // Fora del camp de visió?
            if (angle > lockOnAngle / 2f) continue;

            // Hi ha obstacle entremig?
            if (Physics.Raycast(playerTransform.position + Vector3.up, dirToEnemy.normalized, dist, obstacleMask))
                continue;

            // Score: prioritza els més propers i centrats
            float score = dist + angle * 0.5f;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = col.transform;
            }
        }

        if (bestTarget != null)
            AcquireLockOn(bestTarget);
    }

    // ── Activar / desactivar lock-on ──────────────────────────────────────────

    private void AcquireLockOn(Transform target)
    {
        lockedTarget = target;
        isLockedOn = true;

        if (lockOnCamera != null)
        {
            //lockOnCamera.Target.TrackingTarget = lockedTarget; // Look At → l'enemic
            lockOnCamera.Priority = 20;
        }
        if (normalCamera != null) normalCamera.Priority = 10;

        if (lockOnReticle != null) lockOnReticle.SetActive(true);

        OnLockOnAcquired?.Invoke(lockedTarget);
    }

    public void LoseLockOn()
    {
        lockedTarget = null;
        isLockedOn = false;

        if (lockOnCamera != null)
        {
            //lockOnCamera.Target.TrackingTarget = null;
            lockOnCamera.Priority = 5;
        }
        if (normalCamera != null) normalCamera.Priority = 10;

        if (lockOnReticle != null) lockOnReticle.SetActive(false);

        OnLockOnLost?.Invoke();
    }

    // ── Propietats públiques ──────────────────────────────────────────────────

    public bool IsLockedOn => isLockedOn;
    public Transform Target => lockedTarget;

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (playerTransform == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, lockOnRadius);

        // Dibuixa el con de visió
        Vector3 leftLimit = Quaternion.Euler(0, -lockOnAngle / 2f, 0) * playerTransform.forward;
        Vector3 rightLimit = Quaternion.Euler(0, lockOnAngle / 2f, 0) * playerTransform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + leftLimit * lockOnRadius);
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + rightLimit * lockOnRadius);

        if (isLockedOn && lockedTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(playerTransform.position, lockedTarget.position);
        }
    }
}