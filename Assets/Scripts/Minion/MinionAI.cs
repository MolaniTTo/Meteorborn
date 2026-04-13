using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MinionAI : MonoBehaviour
{
    // ── Estats ──────────────────────────────────────────────────────────────
    public enum MinionState { Desactivat, Activat, Treballant, Atacar, Debilitat, CasiMort }
    public MinionState currentState = MinionState.Desactivat;

    // ── Referències ──────────────────────────────────────────────────────────
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Animator animator;
    [SerializeField] private Transform playerFollow;
    [SerializeField] private Transform playerThrow;
    public BTNode rootNode;

    // ── Spawner ───────────────────────────────────────────────────────────────
    [HideInInspector] public MinionSpawner spawner;
    [HideInInspector] public Vector3 spawnPosition;

    // ── Stats ────────────────────────────────────────────────────────────────
    public float maxEnergy = 100f;
    public float energy = 100f;

    // ── Seguiment del jugador (activat) ──────────────────────────────────────
    [HideInInspector] public Transform playerTransform;
    public float followUpdateInterval = 0.2f;
    private Coroutine followCoroutine;

    // ── Objectiu d'atac ──────────────────────────────────────────────────────
    [HideInInspector] public Transform attackTarget;
    public float attackRange = 1.5f;
    public float energyDrainPerSecond = 10f;

    // ── CasiMort ─────────────────────────────────────────────────────────────
    public float nearDeathDuration = 15f;
    [HideInInspector] public float nearDeathTimer = 0f;
    [HideInInspector] public bool nearDeathExpired = false;
    [HideInInspector] public Transform watchedEnemy; // enemic vigilat mentre està en CasiMort

    // ── Vol (llançament) ─────────────────────────────────────────────────────
    [HideInInspector] public bool isFlying = false;

    // ── OffMeshLink ──────────────────────────────────────────────────────────
    [HideInInspector] public bool traversingLink = false;

    // ── Objecte que porta (treballant) ───────────────────────────────────────
    [HideInInspector] public CarryObject assignedObject;

    // ── Curiositat ────────────────────────────────────────────────────────────
    public float curiosityRadius = 3f;

    // ── Highlight per al cursor ───────────────────────────────────────────────
    [HideInInspector] public bool isHighlighted = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        playerTransform = FindFirstObjectByType<PlayerStateMachine>()?.playerFollowPosition;
        if (playerFollow == null && playerTransform != null)
            playerFollow = playerTransform;
        playerThrow = FindFirstObjectByType<PlayerStateMachine>()?.playerThrowPosition;
        if (playerThrow == null && playerTransform != null)
            playerThrow = playerTransform;
    }

    void Update()
    {
        if (isFlying) return;
        if (agent.isOnOffMeshLink && !traversingLink) StartCoroutine(TraverseLink());
        rootNode?.Execute(this);
    }

    // ── API pública ──────────────────────────────────────────────────────────

    public void Activate()
    {
        energy = maxEnergy;
        attackTarget = null;
        watchedEnemy = null;
        assignedObject = null;
        nearDeathExpired = false;
        nearDeathTimer = 0f;
        agent.enabled = true;
        ChangeState(MinionState.Activat);
    }

    public void AssignAttackTarget(Transform target)
    {
        attackTarget = target;
        ChangeState(MinionState.Atacar);
    }

    public void AssignCarryObject(CarryObject obj)
    {
        assignedObject = obj;
        assignedObject.AssignMinion(this);
        ChangeState(MinionState.Treballant);
    }

    public void ReactivateFromWeakness()
    {
        energy = maxEnergy;
        nearDeathTimer = 0f;
        nearDeathExpired = false;
        watchedEnemy = null;
        agent.enabled = true;
        ChangeState(MinionState.Activat);
    }

    public void ChangeState(MinionState newState)
    {
        currentState = newState;
        if (newState != MinionState.Activat && followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
        if (newState == MinionState.Activat)
            StartFollowing();
    }

    public void PopToSpawn()
    {
        MinionManager.Instance?.UnregisterMinion(this);
        if (spawner != null)
            spawner.SpawnMinion();
        else
            Destroy(gameObject);
    }

    // ── Seguiment ────────────────────────────────────────────────────────────

    public void StartFollowing()
    {
        if (followCoroutine != null) StopCoroutine(followCoroutine);
        followCoroutine = StartCoroutine(FollowRoutine());
    }

    private IEnumerator FollowRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(followUpdateInterval);
        while (currentState == MinionState.Activat)
        {
            if (agent.enabled && playerFollow != null)
                agent.SetDestination(playerFollow.position);
            yield return wait;
        }
    }

    // ── Vol parabòlic ────────────────────────────────────────────────────────

    public void LaunchTo(Vector3 targetPos, float arcHeight = 3f, float duration = 0.6f)
    {
        StartCoroutine(ArcFlight(targetPos, arcHeight, duration));
    }

    private IEnumerator ArcFlight(Vector3 targetPos, float arcHeight, float duration)
    {
        isFlying = true;
        agent.enabled = false;
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector3 linearPos = Vector3.Lerp(startPos, targetPos, t);
            float arc = arcHeight * Mathf.Sin(Mathf.PI * t);
            transform.position = linearPos + Vector3.up * arc;
            Vector3 dir = targetPos - transform.position;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            yield return null;
        }

        transform.position = targetPos;
        agent.enabled = true;
        isFlying = false;
        OnLanded();
    }

    private void OnLanded()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (Collider col in hits)
        {
            if (col.CompareTag("Enemy")) { AssignAttackTarget(col.transform); return; }
            CarryObject carry = col.GetComponent<CarryObject>();
            if (carry != null) { AssignCarryObject(carry); return; }
        }
        ChangeState(MinionState.Activat);
        MinionManager.Instance?.RegisterActive(this);
        Debug.Log("Minion registrat de nou");
    }

    // ── OffMeshLink ──────────────────────────────────────────────────────────

    public IEnumerator TraverseLink()
    {
        traversingLink = true;
        float originalSpeed = agent.speed;
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 finalDestination = agent.destination;
        float realDistance = Vector3.Distance(data.startPos, data.endPos);
        float requiredSpeed = realDistance / (2f / agent.speed);
        agent.speed = requiredSpeed;
        agent.autoTraverseOffMeshLink = true;
        while (agent.isOnOffMeshLink) yield return null;
        agent.velocity = Vector3.zero;
        agent.speed = originalSpeed;
        agent.autoTraverseOffMeshLink = false;
        agent.SetDestination(finalDestination);
        traversingLink = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, curiosityRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}