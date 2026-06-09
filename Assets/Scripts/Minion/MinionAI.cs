using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.GlobalIllumination;

public class MinionAI : MonoBehaviour
{
    // ── Estats ──────────────────────────────────────────────────────────────
    public enum MinionState { Desactivat, Activat, Treballant, Atacar, Debilitat, CasiMort }
    public MinionState currentState = MinionState.Desactivat;

    // ── Referències ──────────────────────────────────────────────────────────
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Animator animator;
    [SerializeField] private Transform playerLook;
    [SerializeField] private Transform playerFollow;
    [SerializeField] private Transform playerThrow;
    [HideInInspector] public MinionScaleController scaleController;
    [HideInInspector] public MinionVisualController visualController;
    [HideInInspector] public bool nearDeathInitialized = false;
    [SerializeField] private GameObject attackPSPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform target;
    [SerializeField] private GameObject carryPSPrefab;
    [SerializeField] private Transform carryPoint;
    [SerializeField] private Transform carryTarget;
    public HealthComponent healthComponent;
    public bool isAtMinScale => scaleController != null && scaleController.IsAtMinScale;

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
        scaleController = GetComponent<MinionScaleController>();
        visualController = GetComponent<MinionVisualController>();
    }

    void Start()
    {
        playerTransform = FindFirstObjectByType<PlayerStateMachine>()?.playerFollowPosition;
        if (playerFollow == null && playerTransform != null)
        {
            playerFollow = playerTransform;
        }
        playerThrow = FindFirstObjectByType<PlayerStateMachine>()?.playerThrowPosition;
        if (playerThrow == null && playerTransform != null)
        {
            playerThrow = playerTransform;
        }
        playerLook = FindFirstObjectByType<PlayerStateMachine>()?.transform;
        if (playerLook == null && playerTransform != null)
        {
            playerLook = playerTransform;
        }
        HealthComponent hc = GetComponent<HealthComponent>();
        if (hc != null) hc.OnDeath += OnDeath;

    }

    void Update()
    {
        if (isFlying) return;
        if (agent.isOnOffMeshLink && !traversingLink) StartCoroutine(TraverseLink());

        if (currentState == MinionState.Activat && playerLook != null)
        {
            Vector3 dir = (playerLook.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot, Time.deltaTime * 12f);
            }
        }

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
        scaleController?.SetMaxScale();
        visualController?.MaxLightAndEmission();
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
        if (animator != null) animator.SetTrigger("Reactivate");
        scaleController?.SetMaxScale();
        visualController?.MaxLightAndEmission();
        ChangeState(MinionState.Activat);
    }

    public void ChangeState(MinionState newState)
    {
        if (currentState == MinionState.Treballant && newState != MinionState.Treballant)
        {
            if (animator != null)
                animator.SetLayerWeight(animator.GetLayerIndex("UpperLayer"), 0f);
            //parar el carry PS si es necesario (opcional, dependiendo de cómo quieras manejar los efectos visuales al dejar de cargar un objeto)
             if (carryPSPrefab != null)
             {
                carryPSPrefab.GetComponentInChildren<ParticleSystem>().Stop();

                foreach (GeneradorParticulesDisparo ps in FindObjectsByType<GeneradorParticulesDisparo>(FindObjectsSortMode.None))
                {
                    if (ps.origen == carryPoint && ps.objectiu == carryTarget)
                    {
                        Destroy(ps.gameObject);
                    }
                }

            }
        }

        currentState = newState;

        if (newState != MinionState.Activat && followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
        if (newState == MinionState.Activat)
            StartFollowing();
        if (newState == MinionState.Treballant)
        {
            if (animator != null)
                animator.SetLayerWeight(animator.GetLayerIndex("UpperLayer"), 1f);

            carryTarget = assignedObject.transform;
            if (carryTarget != null)
            {
                GameObject go = Instantiate(carryPSPrefab);
                go.GetComponent<GeneradorParticulesDisparo>().Init(carryPoint, carryTarget);
            }
        }
    }

    public void PopToSpawn()
    {
        // Desenregistra aquest minion de qualsevol enemic que el tingui com atacant
        if (attackTarget != null)
        {
            EnemicAI enemic = attackTarget.GetComponent<EnemicAI>();
            if (enemic != null) enemic.UnregisterAttacker(transform);
            attackTarget = null;
        }
        // també comprova watchedEnemy per si estava en CasiMort
        if (watchedEnemy != null)
        {
            EnemicAI enemic = watchedEnemy.GetComponent<EnemicAI>();
            if (enemic != null) enemic.UnregisterAttacker(transform);
            watchedEnemy = null;
        }

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

    public void LaunchTo(Vector3 targetPos, float arcHeight = 3f, float duration = 1.2f)
    {
        StartCoroutine(ArcFlight(targetPos, arcHeight, duration));
    }

    private IEnumerator ArcFlight(Vector3 targetPos, float arcHeight, float duration)
    {
        animator.SetTrigger("Launch");
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
        animator.SetTrigger("Land");

        if (MinionManager.Instance?.GetPendingLaunch() == this)
            MinionManager.Instance.ClearPendingLaunch();

        Debug.Log($"[MinionAI] {name} ha aterrat");
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.1f);
        foreach (Collider col in hits)
        {
            if (col.CompareTag("Enemy") && col.GetComponent<HealthComponent>().currentHealth > 0) { Debug.Log("Enemic detectat");  AssignAttackTarget(col.transform); return; }
            CarryObject carry = col.GetComponent<CarryObject>();
            if (carry != null) { AssignCarryObject(carry); return; }
        }
        ChangeState(MinionState.Activat);
        MinionManager.Instance?.RegisterActive(this);
        Debug.Log($"[MinionAI] {name} registrat de nou com actiu");
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

    public void SetMinScale() { scaleController?.SetMinScale(); }

    private void OnDeath()
    {
        animator.SetTrigger("CasiMort");
        nearDeathInitialized = false; // perquè BTCasiMort s'inicialitzi bé
        ChangeState(MinionState.CasiMort);
    }

    public void LaunchAttack()
    {
        target = attackTarget;
        if (target != null)
        {
            GameObject go = Instantiate(attackPSPrefab);
            go.GetComponent<GeneradorParticulesDisparo>().Init(firePoint, target);
        }
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, curiosityRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}