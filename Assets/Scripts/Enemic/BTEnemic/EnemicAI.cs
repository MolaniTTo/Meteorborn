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
    [Tooltip("Radi al voltant del guardPoint on es cura (evita el NavMeshObstacle del centre)")]
    public float healRadius = 3f;
    public float healRate = 2f;

    // ── Radis ─────────────────────────────────────────────────────────────────
    [Header("Radis")]
    public float patrolRadius = 6f;
    public float chaseRadius = 12f;
    public float alertRadius = 2.5f;
    public float attackRadius = 1.5f;
    public float minPointDistance = 2.5f;

    // ── Visió ─────────────────────────────────────────────────────────────────
    [Header("Visió")]
    public float visionRange = 8f;
    [Tooltip("Angle total del con de visió (ex: 90 = 45° a cada costat)")]
    public float visionAngle = 90f;
    public LayerMask obstacleMask;
    [Tooltip("Transform del cap del enemic, d'on surten els raigs de visió")]
    public Transform eyeTransform;
    
    [HideInInspector] public bool hasScreamed = false;
    [HideInInspector] public bool isScreaming = false;
    [SerializeField] private float screamDuration = 1.5f;
    private float screamTimer = 0f;

    // ── Energia ───────────────────────────────────────────────────────────────
    [Header("Energia")]
    public float maxEnergia = 100f;
    public float energia = 100f;
    public float damagedEnergiaPerSecond = 2f;

    // ── Searching ─────────────────────────────────────────────────────────────
    [Header("Searching")]
    public float searchDuration = 3f;

    // ── Agents ────────────────────────────────────────────────────────────────
    [Header("Agents")]
    public NavMeshAgent agent;
    public NavMeshAgent ghostAgent;

    // ── Animator ──────────────────────────────────────────────────────────────
    [HideInInspector] public Animator animator;

    // ── Estat públic (llegit pels nodes) ─────────────────────────────────────
    [HideInInspector] public HealthComponent targetHealth;
    [HideInInspector] public Vector3 lastSeenPosition;
    [HideInInspector] public float searchTimer = 0f;
    [HideInInspector] public bool isHealing = false;

    // ── Patrulla (intern) ─────────────────────────────────────────────────────
    private Vector3[] patrolPoints;
    private int currentPatrolIndex = 0;
    private int patrolDirection = 1;
    [SerializeField] private int numOfPatrolPoints = 5;

    // ── Hashes animator ───────────────────────────────────────────────────────
    private static readonly int SpeedHash = Animator.StringToHash("speed");
    private static readonly int AttackHash = Animator.StringToHash("attack");

    // ── Llista d'atacants ───────────────────────────────────────────────────────
    private List<Transform> attackers = new List<Transform>();

    // ─────────────────────────────────────────────────────────────────────────


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        agent.speed = ghostAgent.speed;
        GeneratePatrolPoints(numOfPatrolPoints);
        HealthComponent hc = GetComponent<HealthComponent>();
        if (hc != null) hc.OnDeath += OnDeath;
    }

    void Update()
    {
        FollowGhost();
        UpdateAnimator();
        UpdateHealing();
        UpdateFaceAttackers();
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

        Vector3 dirToGhost = (ghostAgent.transform.position - transform.position);
        dirToGhost.y = 0f;
        if (dirToGhost.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dirToGhost),
                10f * Time.deltaTime);

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

    private void GeneratePatrolPoints(int count)
    {
        patrolPoints = new Vector3[count];

        // Rotació aleatòria inicial perquè cada enemic comenci diferent
        float angleOffset = Random.Range(0f, 360f);
        // Sentit aleatori
        patrolDirection = Random.value > 0.5f ? 1 : -1;

        int generated = 0;
        for (int i = 0; i < count; i++)
        {
            float angle = angleOffset + (360f / count) * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 candidate = new Vector3(
                guardPoint.position.x + Mathf.Cos(rad) * patrolRadius,
                guardPoint.position.y,
                guardPoint.position.z + Mathf.Sin(rad) * patrolRadius
            );

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                patrolPoints[generated] = hit.position;
                generated++;
            }
            else
            {
                // Si el punt no és vàlid, busca el més proper al perímetre
                if (NavMesh.SamplePosition(candidate, out NavMeshHit fallback, patrolRadius * 2f, NavMesh.AllAreas))
                {
                    patrolPoints[generated] = fallback.position;
                    generated++;
                }
            }
        }

        // Si no ha generat tots els punts, duplica els que té per omplir l'array
        for (int i = generated; i < count; i++)
            patrolPoints[i] = patrolPoints[i % Mathf.Max(generated, 1)];

        // Comença per un punt aleatori del circuit
        currentPatrolIndex = Random.Range(0, count);
    }

    public Vector3 GetHealPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * healRadius;
            Vector3 candidate = new Vector3(
                guardPoint.position.x + randomCircle.x,
                guardPoint.position.y,
                guardPoint.position.z + randomCircle.y
            );
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }
        return transform.position;
    }

    // ── Visió ─────────────────────────────────────────────────────────────────

    public HealthComponent CheckVision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange);
        HealthComponent best = null;
        float bestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            HealthComponent hc = col.GetComponent<HealthComponent>();
            if (hc == null) continue;
            if (hc == GetComponent<HealthComponent>()) continue;
            if (hc.IsDead()) continue;
            if (!hc.IsTargetableByEnemy) continue;
            if (!CanSeeTarget(col.transform)) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < bestDist) { bestDist = dist; best = hc; }
        }
        return best;
    }

    public HealthComponent CheckAlertRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius);
        foreach (Collider col in hits)
        {
            HealthComponent hc = col.GetComponent<HealthComponent>();
            if (hc == null) continue;
            if (hc == GetComponent<HealthComponent>()) continue;
            if (hc.IsDead()) continue;
            if (!hc.IsTargetableByEnemy) continue;
            return hc;
        }
        return null;
    }

    public bool CanSeeTarget(Transform target)
    {
        Vector3 origin = eyeTransform != null ? eyeTransform.position : transform.position + Vector3.up * 1.5f;
        Vector3 dirToTarget = target.position - origin;
        float dist = dirToTarget.magnitude;

        if (dist > visionRange) return false;
        if (Vector3.Angle(transform.forward, dirToTarget.normalized) > visionAngle / 2f) return false;
        if (Physics.Raycast(origin, dirToTarget.normalized, dist, obstacleMask)) return false;

        return true;
    }

    // ── Helpers pels nodes ────────────────────────────────────────────────────

    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                10f * Time.deltaTime);
    }

    public bool ShouldHeal() => energia < maxEnergia * 0.3f;

    public void TakeDamage(float amount)
    {
        energia = Mathf.Max(0f, energia - amount);
    }

    public void SetAttackAnimation(bool value)
    {
        animator.SetBool(AttackHash, value);
    }

    private void UpdateAnimator()
    {
        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        animator.SetBool(AttackHash, false);
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
        if (!attackers.Contains(attacker))
            attackers.Add(attacker);
    }

    public void UnregisterAttacker(Transform attacker)
    {
        attackers.Remove(attacker);
        // Si era el que miravem, mira al seguent
        if (attackers.Count > 0)
            FaceTarget(attackers[0].position);
    }

    private void UpdateFaceAttackers()
    {
        // Neteja atacants morts o nuls
        attackers.RemoveAll(a => a == null || !a.gameObject.activeInHierarchy);
        if (attackers.Count > 0)
            FaceTarget(attackers[0].position);
    }
    public void OnAttackHit()
    {
        if (targetHealth == null || targetHealth.IsDead()) return;
        targetHealth.TakeDamage(damagedEnergiaPerSecond);
    }

    public void TriggerScream()
    {
        if (hasScreamed) return;
        hasScreamed = true;
        isScreaming = true;
        screamTimer = screamDuration;
        animator.SetTrigger("Scream");
    }
    private void UpdateScream()
    {
        if (!isScreaming) return;
        screamTimer -= Time.deltaTime;
        StopGhost();
        if (screamTimer <= 0f)
            isScreaming = false;
    }
    public void LoseTarget()
    {
        targetHealth = null;
        hasScreamed = false;
    }

    private void OnDeath()
    {
        Debug.Log($"{gameObject.name} ha mort.");
        animator.SetTrigger("Mort");
        agent.enabled = false;
        ghostAgent.enabled = false;
        enabled = false; // para el Update y el BT
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