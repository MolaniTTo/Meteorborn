using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using DG.Tweening;
using DG.Tweening.Core.Easing;

public class MovablePlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Vector3 moveAmount;            // Quantitat de moviment
    [SerializeField] private Vector3 rotationAmount;        // Quantitat de rotació
    [SerializeField] private float duration = 0.6f;         // Duració de l'animació
    [SerializeField] private Ease moveEase = Ease.OutBack;  // Ease del moviment
    [SerializeField] private bool useMove = true;           // Si cal moure
    [SerializeField] private bool useRotation = false;      // Si cal rotar
    [SerializeField] private bool useToggle = true;         // Si alterna entre dos estats

    [Header("NavMesh")]
    [SerializeField] private NavMeshSurface navMeshSurface; // NavMeshSurface propi (opcional, només si fa rebake)

    [Header("Player Detection")]
    [SerializeField] private Collider playerDetectionCollider;  // Trigger a la part superior

    //State
    private bool toggled = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isMoving = false;

    //Player
    private Transform playerTransform;
    private NavMeshAgent playerAgent;

    //Properties
    public bool IsMoving => isMoving;

    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerAgent = player.GetComponent<NavMeshAgent>();
        }
    }

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (playerDetectionCollider != null) { playerDetectionCollider.isTrigger = true; } //Ens assegurem que el collider de detecció és trigger
    }

    //INteract que ho crida el GameManager

    public void Interact()
    {
        if (isMoving) return;

        isMoving = true;

        // Si el player esta a sobre, el fem fill de la plataforma
        AttachPlayerIfOnTop();

        transform.DOComplete();

        Vector3 targetPosition = useToggle && toggled ? initialPosition : initialPosition + moveAmount;

        Vector3 targetRotation = useToggle && toggled ? initialRotation.eulerAngles : initialRotation.eulerAngles + rotationAmount;

        Sequence seq = DOTween.Sequence();

        if (useMove) { seq.Join(transform.DOMove(targetPosition, duration).SetEase(moveEase)); }

        if (useRotation) { seq.Join(transform.DORotate(targetRotation, duration, RotateMode.FastBeyond360).SetEase(moveEase)); }

        seq.OnComplete(() =>
        {
            if (useToggle) toggled = !toggled;

            DetachPlayer();             // Traiem el player de la jerarquia
            RebakeNavMesh();            // Rebake del NavMesh si té NavMeshSurface
            GameManager.instance.EvaluateConditions();  // Avaluem condicions
            isMoving = false;
        });
    }


    //Player attach/detach

    private bool playerOnTop = false;

    private void AttachPlayerIfOnTop()
    {
        if (!playerOnTop || playerTransform == null) return;

        playerAgent.enabled = false;
        playerTransform.SetParent(transform);
    }

    private void DetachPlayer()
    {
        if (playerTransform == null) return;
        if (playerTransform.parent != transform) return;

        playerTransform.SetParent(null);
        playerAgent.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerOnTop = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerOnTop = false;
    }

    // -------------------------------------------------------
    // NAVMESH REBAKE
    // -------------------------------------------------------

    private void RebakeNavMesh()
    {
        if (navMeshSurface == null) return;
        navMeshSurface.BuildNavMesh();
        Debug.Log($"NavMesh rebakeit: {gameObject.name}");
    }
}