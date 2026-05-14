using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemicAI : MonoBehaviour
{
    // ── Behaviour Tree ────────────────────────────────────────────────────────
    [Header("Behaviour Tree")]
    public BTNodeEnemic rootNode;

    // ── Guard Point ───────────────────────────────────────────────────────────
    [Header("Guard Point")]
    public Transform guardPoint;
    public float healRadius = 3f;
    public float healRate = 2f;

    // ── Radis ─────────────────────────────────────────────────────────────────
    [Header("Radis")]
    public float patrolRadius = 8f;
    public float chaseRadius = 15f;
    public float alertRadius = 6f;
    public float attackRadius = 9f;
    public float minPointDistance = 2.5f;

    // ── Visió ─────────────────────────────────────────────────────────────────
    [Header("Visió")]
    public float visionRange = 8f;
    [Tooltip("Angle total del con de visió (ex: 90 = 45° a cada costat)")]
    public float visionAngle = 90f;
    public LayerMask obstacleMask;
    [Tooltip("Transform del cap del enemic, d'on surten els raigs de visió")]
    public Transform eyeTransform;
    [Tooltip("LayerMask del Player - per ignorar-lo als raycasts de visió")]
    public LayerMask playerMask;

    // ── Rotació ───────────────────────────────────────────────────────────────
    [Header("Rotació")]
    [Tooltip("Velocitat de gir en persecució/atac (graus/s aprox Slerp t)")]
    public float chaseTurnSpeed = 12f;
    [Tooltip("Velocitat de gir suau pre-Scream")]
    public float screamTurnSpeed = 8f;

    // ── Scream ────────────────────────────────────────────────────────────────
    [Header("Scream")]
    [SerializeField] private float screamDuration = 1.5f;

    [HideInInspector] public bool isScreaming = false;
    [HideInInspector] public bool isPreScream = false;
    private float screamTimer = 0f;
    private HealthComponent lastScreamTarget = null;

    // ── Energia ───────────────────────────────────────────────────────────────
    [Header("Energia")]
    public float maxEnergia = 100f;
    public float energia = 100f;
    public float damagedEnergiaPerSecond = 20f;

    // ── Searching ─────────────────────────────────────────────────────────────
    [Header("Searching")]
    public float searchDuration = 5f;

    // ── LookAround ────────────────────────────────────────────────────────────
    [Header("LookAround")]
    public float lookAroundDuration = 6.1f;
    [HideInInspector] public bool isLookingAround = false;
    [HideInInspector] public float lookAroundTimer = 0f;

    // ── Agents ────────────────────────────────────────────────────────────────
    [Header("Agents")]
    public NavMeshAgent agent;
    public NavMeshAgent ghostAgent;

    // ── Animator ──────────────────────────────────────────────────────────────
    [HideInInspector] public Animator animator;

    // ── Estat públic ─────────────────────────────────────────────────────────
    [HideInInspector] public HealthComponent targetHealth;
    [HideInInspector] public Vector3 lastSeenPosition;
    [HideInInspector] public float searchTimer = 0f;
    [HideInInspector] public bool isHealing = false;

    // ── Patrulla ──────────────────────────────────────────────────────────────
    private Vector3[] patrolPoints;
    private int currentPatrolIndex = 0;
    private int patrolDirection = 1;
    [SerializeField] private int numOfPatrolPoints = 5;

    // ── Hashes animator ───────────────────────────────────────────────────────
    private static readonly int SpeedHash = Animator.StringToHash("speed");
    private static readonly int AttackHash = Animator.StringToHash("attack");
    private static readonly int ScreamHash = Animator.StringToHash("Scream");
    private static readonly int LookAroundHash = Animator.StringToHash("LookAround");
    private static readonly int MortHash = Animator.StringToHash("Mort");

    // ── Atacants ──────────────────────────────────────────────────────────────
    private List<Transform> attackers = new List<Transform>();

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        agent.speed = ghostAgent.speed;
        // Desactivem la rotació automàtica del NavMeshAgent:
        // la rotació la gestionem manualment amb FaceTarget i UpdateScream.
        agent.updateRotation = false;
        GeneratePatrolPoints(numOfPatrolPoints);
        HealthComponent hc = GetComponent<HealthComponent>();
        if (hc != null) hc.OnDeath += OnDeath;
    }

    void Update()
    {
        FollowGhost();
        UpdateAnimatorSpeed();
        UpdateHealing();
        UpdateScream();
        rootNode?.Execute(this);
    }

    // ── Ghost ─────────────────────────────────────────────────────────────────

    private void FollowGhost()
    {
        float distToGhost = Vector3.Distance(transform.position, ghostAgent.transform.position);
        if (distToGhost <= 0.5f)
        {
            agent.ResetPath();
            return;
        }

        // Durant PreScream, Scream, o quan tenim un target actiu,
        // la rotació la gestionen UpdateScream() i FaceTarget().
        // FollowGhost només mou el cos, NO gira.
        bool rotationManagedElsewhere = isPreScream || isScreaming || targetHealth != null;
        if (!rotationManagedElsewhere)
        {
            Vector3 dirToGhost = ghostAgent.transform.position - transform.position;
            dirToGhost.y = 0f;
            if (dirToGhost.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dirToGhost),
                    10f * Time.deltaTime);
        }

        agent.SetDestination(ghostAgent.transform.position);
    }

    public void MoveGhostTo(Vector3 destination)
    {
        ghostAgent.SetDestination(destination);
    }

    public void StopGhost()
    {
        ghostAgent.SetDestination(ghostAgent.transform.position);
    }

    // ── Patrulla ──────────────────────────────────────────────────────────────

    public Vector3 GetPatrolPoint() => patrolPoints[currentPatrolIndex];

    public bool HasReachedPatrolPoint()
    {
        if (!ghostAgent.pathPending && ghostAgent.remainingDistance < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + patrolDirection + patrolPoints.Length) % patrolPoints.Length;
            return true;
        }
        return false;
    }

    public void ResumePatrolFromNearestPoint()
    {
        float bestDist = float.MaxValue;
        int bestIndex = 0;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, patrolPoints[i]);
            if (d < bestDist) { bestDist = d; bestIndex = i; }
        }
        currentPatrolIndex = bestIndex;
    }

    private void GeneratePatrolPoints(int count)
    {
        patrolPoints = new Vector3[count];
        float angleOffset = Random.Range(0f, 360f);
        patrolDirection = Random.value > 0.5f ? 1 : -1;

        int generated = 0;
        for (int i = 0; i < count; i++)
        {
            float angle = angleOffset + (360f / count) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 candidate = new Vector3(
                guardPoint.position.x + Mathf.Cos(rad) * patrolRadius,
                guardPoint.position.y,
                guardPoint.position.z + Mathf.Sin(rad) * patrolRadius);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            { patrolPoints[generated] = hit.position; generated++; }
            else if (NavMesh.SamplePosition(candidate, out NavMeshHit fallback, patrolRadius * 2f, NavMesh.AllAreas))
            { patrolPoints[generated] = fallback.position; generated++; }
        }

        for (int i = generated; i < count; i++)
            patrolPoints[i] = patrolPoints[i % Mathf.Max(generated, 1)];

        currentPatrolIndex = Random.Range(0, count);
    }

    public Vector3 GetHealPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 rc = Random.insideUnitCircle.normalized * healRadius;
            Vector3 candidate = new Vector3(guardPoint.position.x + rc.x, guardPoint.position.y, guardPoint.position.z + rc.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }
        return transform.position;
    }

    // ── Visió ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna el minion activat més proper visible al con de visió.
    /// El raig ignora el layer del Player per evitar falsos negatius.
    /// </summary>
    public HealthComponent CheckVision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange);
        HealthComponent best = null;
        float bestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            HealthComponent hc = col.GetComponent<HealthComponent>();
            if (hc == null || hc == GetComponent<HealthComponent>()) continue;
            if (hc.IsDead() || !hc.IsTargetableByEnemy) continue;
            if (!IsMinionActivat(col.gameObject)) continue;
            if (!CanSeeTarget(col.transform)) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < bestDist) { bestDist = dist; best = hc; }
        }
        return best;
    }

    /// <summary>
    /// Retorna el minion activat més proper dins del alertRadius (360°, sense visió).
    /// </summary>
    public HealthComponent CheckAlertRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius);
        HealthComponent best = null;
        float bestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            HealthComponent hc = col.GetComponent<HealthComponent>();
            if (hc == null || hc == GetComponent<HealthComponent>()) continue;
            if (hc.IsDead() || !hc.IsTargetableByEnemy) continue;
            if (!IsMinionActivat(col.gameObject)) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < bestDist) { bestDist = dist; best = hc; }
        }
        return best;
    }

    /// <summary>
    /// Comprova si pot veure el target. El raycast ignora el layer del Player.
    /// </summary>
    public bool CanSeeTarget(Transform target)
    {
        Vector3 origin = eyeTransform != null ? eyeTransform.position : transform.position + Vector3.up * 1.5f;
        Vector3 dirToTarget = target.position - origin;
        float dist = dirToTarget.magnitude;

        if (dist > visionRange) return false;
        if (Vector3.Angle(transform.forward, dirToTarget.normalized) > visionAngle / 2f) return false;

        // Ignorem el layer del Player al raycast per no bloquejar la visió dels minions
        LayerMask blockMask = obstacleMask & ~playerMask;
        if (Physics.Raycast(origin, dirToTarget.normalized, dist, blockMask)) return false;

        return true;
    }

    private bool IsMinionActivat(GameObject go)
    {
        MinionAI minion = go.GetComponent<MinionAI>();
        if (minion == null) return true;
        return minion.currentState != MinionAI.MinionState.Desactivat;
    }

    // ── AoE Atac ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Crida des de l'event d'animació. Aplica dany AoE a tots els minions
    /// activats dins del attackRadius.
    /// </summary>
    public void OnAttackHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRadius);
        foreach (Collider col in hits)
        {
            HealthComponent hc = col.GetComponent<HealthComponent>();
            if (hc == null || hc == GetComponent<HealthComponent>()) continue;
            if (hc.IsDead() || !hc.IsTargetableByEnemy) continue;
            if (!IsMinionActivat(col.gameObject)) continue;
            hc.TakeDamage(damagedEnergiaPerSecond);
        }
    }

    // ── Helpers pels nodes ────────────────────────────────────────────────────

    /// <summary>
    /// Gira cap al target amb la velocitat de persecució.
    /// </summary>
    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                chaseTurnSpeed * Time.deltaTime);
    }

    public bool ShouldHeal() => energia < maxEnergia * 0.3f;
    public void TakeDamage(float amount) => energia = Mathf.Max(0f, energia - amount);

    public void SetAttackAnimation(bool value) => animator.SetBool(AttackHash, value);

    private void UpdateAnimatorSpeed()
    {
        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

    private void UpdateHealing()
    {
        if (!isHealing) return;
        float distToGuard = Vector3.Distance(transform.position, guardPoint.position);
        if (distToGuard <= healRadius)
            energia = Mathf.Min(maxEnergia, energia + healRate * Time.deltaTime);
    }

    public void RegisterAttacker(Transform attacker)
    {
        if (!attackers.Contains(attacker)) attackers.Add(attacker);
    }

    public void UnregisterAttacker(Transform attacker)
    {
        attackers.Remove(attacker);
        if (attackers.Count > 0) FaceTarget(attackers[0].position);
    }

    // ── Scream ────────────────────────────────────────────────────────────────

    // Timeout de seguretat: si el gir no convergeix en X segons, força el Scream igualment
    [SerializeField] private float preScreamTimeout = 1.2f;
    private float preScreamTimer = 0f;

    public void TriggerScream(HealthComponent target)
    {
        if (target == null) return;
        if (lastScreamTarget == target) return;

        lastScreamTarget = target;
        isPreScream = true;
        isScreaming = false;
        screamTimer = 0f;
        preScreamTimer = 0f;
        StopGhost();
    }

    private void LaunchScream()
    {
        isPreScream = false;
        isScreaming = true;
        screamTimer = screamDuration;
        animator.SetTrigger(ScreamHash);
    }

    private void UpdateScream()
    {
        if (isPreScream)
        {
            if (targetHealth == null || targetHealth.IsDead())
            {
                // Target perdut durant el gir, cancel·lem
                isPreScream = false;
                preScreamTimer = 0f;
                return;
            }

            preScreamTimer += Time.deltaTime;

            Vector3 dir = targetHealth.transform.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);

                // RotateTowards garanteix convergència real (graus/segon fixes)
                float degreesPerSecond = screamTurnSpeed * 30f; // screamTurnSpeed=8 → 240°/s
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, degreesPerSecond * Time.deltaTime);

                float angle = Quaternion.Angle(transform.rotation, targetRot);

                // Ha girat prou O ha passat el timeout → llança Scream
                if (angle < 5f || preScreamTimer >= preScreamTimeout)
                {
                    preScreamTimer = 0f;
                    LaunchScream();
                }
            }
            else
            {
                // Ja mirem al target (distància ~0), llança directament
                LaunchScream();
            }

            StopGhost();
            return;
        }

        if (isScreaming)
        {
            screamTimer -= Time.deltaTime;
            StopGhost();
            if (screamTimer <= 0f) isScreaming = false;
        }
    }

    public void LoseTarget()
    {
        targetHealth = null;
        lastScreamTarget = null;
    }

    // ── LookAround ────────────────────────────────────────────────────────────

    public void StartLookAround()
    {
        isLookingAround = true;
        lookAroundTimer = lookAroundDuration;
        StopGhost();
        animator.SetTrigger(LookAroundHash);
    }

    public void StopLookAround()
    {
        isLookingAround = false;
        lookAroundTimer = 0f;
    }

    // ── Mort ─────────────────────────────────────────────────────────────────

    private void OnDeath()
    {
        UniqueID uid = GetComponent<UniqueID>();
        if (uid != null) { WorldManager.Instance?.RegisterEnemyDead(uid.ID); }

        Debug.Log($"{gameObject.name} ha mort.");
        animator.SetTrigger(MortHash);
        agent.enabled = false;
        ghostAgent.enabled = false;
        enabled = false;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (guardPoint == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(guardPoint.position, patrolRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alertRadius);
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(guardPoint.position, healRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Vector3 left = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward;
            Vector3 right = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + left * visionRange);
            Gizmos.DrawLine(transform.position, transform.position + right * visionRange);
        }
    }
}