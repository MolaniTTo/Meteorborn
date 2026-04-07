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
    public BTNode rootNode;

    // ── Stats ────────────────────────────────────────────────────────────────
    public float maxEnergy = 100f;
    public float energy = 100f;

    // ── Patrulla (desactivat) ────────────────────────────────────────────────
    // El minion desactivat no es mou, queda al seu punt de spawn
    [HideInInspector] public Vector3 spawnPosition;

    // ── Seguiment del jugador (activat) ──────────────────────────────────────
    [HideInInspector] public Transform playerTransform;
    public float followUpdateInterval = 0.2f;
    private Coroutine followCoroutine;

    // ── Objectiu d'atac ──────────────────────────────────────────────────────
    [HideInInspector] public Transform attackTarget;
    public float attackRange = 1.5f;
    public float energyDrainPerSecond = 10f; // energia que absorbeix de l'enemic per segon

    // ── CasiMort ─────────────────────────────────────────────────────────────
    public float nearDeathDuration = 15f;
    [HideInInspector] public float nearDeathTimer = 0f;
    [HideInInspector] public bool nearDeathExpired = false;

    // ── Vol (llançament) ─────────────────────────────────────────────────────
    [HideInInspector] public bool isFlying = false;

    // ── Link (atravessant link) ──────────────────────────────────────────────
    [HideInInspector] public bool traversingLink = false;

    // ── Objecte que porta (treballant) ───────────────────────────────────────
    [HideInInspector] public CarryObject assignedObject;

    // ── Curiositat (desactivat amb jugador a prop) ────────────────────────────
    public float curiosityRadius = 3f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (isFlying) return; // mentre vola no executa el BT
        if (agent.isOnOffMeshLink && !traversingLink) { StartCoroutine(TraverseLink()); }
        rootNode?.Execute(this);
    }

    // ── API pública per al MinionManager ────────────────────────────────────

    //Per activar al minion des del MinionManager, resetejant tots els paràmetres rellevants
    public void Activate()
    {
        energy = maxEnergy;
        attackTarget = null;
        assignedObject = null;
        nearDeathExpired = false;
        nearDeathTimer = 0f;
        ChangeState(MinionState.Activat);
    }

    //Assigna un objectiu d'atac i canvia a estat Atacar, començant a drenar energia de l'enemic quan estigui a prop. Si l'enemic mor o s'allunya, torna a Activat.
    public void AssignAttackTarget(Transform target)
    {
        attackTarget = target;
        ChangeState(MinionState.Atacar);
    }

    //Assigna un objecte portador i canvia a estat Treballant, fent que el minion el porti seguint al jugador. Quan arribi al punt de recollida, el minion el recollirà i seguirà al jugador portant-lo. El MinionManager s'encarregarà de detectar quan es deixa l'objecte a la zona de lliurament per canviar a Activat.
    public void AssignCarryObject(CarryObject obj)
    {
        assignedObject = obj;
        ChangeState(MinionState.Treballant);
    }

    //Reactiva des de CasiMort a Activat, per si no hi ha enemic actiu pero esta en CasiMort
    public void ReactivateFromWeakness()
    {
        energy = maxEnergy;
        nearDeathTimer = 0f;
        nearDeathExpired = false;
        ChangeState(MinionState.Activat);
    }

    //Reactiva des de CasiMort a Atacar en cas que hi hagi un enemic actiu, per si el minion esta en CasiMort pero hi ha un enemic actiu que pot atacar
    public void ReactivateToAttack(Transform target)
    {
        energy = maxEnergy;
        nearDeathTimer = 0f;
        nearDeathExpired = false;
        attackTarget = target;
        ChangeState(MinionState.Atacar);
    }

    public void ChangeState(MinionState newState) //Oer canviar d'estat des de dins del BT o des del MinionManager
    {
        currentState = newState;

        // Para el seguiment si no és Activat
        if (newState != MinionState.Activat && followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }

        if (newState == MinionState.Activat)
            StartFollowing();
    }

    // ── Seguiment coroutine ──────────────────────────────────────────────────

    public void StartFollowing()
    {
        if (followCoroutine != null) StopCoroutine(followCoroutine);
        followCoroutine = StartCoroutine(FollowRoutine());
    }

    private IEnumerator FollowRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(followUpdateInterval); //Per no actualitzar cada frame, sinó cada cert temps per optimitzar el rendiment
        while (currentState == MinionState.Activat)
        {
            if (agent.enabled && playerTransform != null)
                agent.SetDestination(playerFollow.position);
            yield return wait;
        }
    }

    // ── Vol (llançament físic) ───────────────────────────────────────────────

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

            // Interpolació lineal + arc parabòlic
            Vector3 linearPos = Vector3.Lerp(startPos, targetPos, t);
            float arc = arcHeight * Mathf.Sin(Mathf.PI * t);
            transform.position = linearPos + Vector3.up * arc;

            // Rotació cap al desti
            Vector3 dir = (targetPos - transform.position);
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
                transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));

            yield return null;
        }

        transform.position = targetPos;
        agent.enabled = true;
        isFlying = false;

        // Un cop aterrat, comprova si hi ha un objectiu a prop
        OnLanded();
    }

    private void OnLanded()
    {
        // Comprova si hi ha un enemic o objecte interactuable a prop
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (Collider col in hits)
        {
            // Enemic
            if (col.CompareTag("Enemy"))
            {
                AssignAttackTarget(col.transform);
                return;
            }
            // Objecte portador
            CarryObject carry = col.GetComponent<CarryObject>();
            if (carry != null)
            {
                AssignCarryObject(carry);
                return;
            }
        }
        // Si no hi ha res, segueix al jugador
        ChangeState(MinionState.Activat);
    }

    // ── Retorn al spawn (desactivat després de CasiMort perdut) ──────────────

    public IEnumerator ReturnToSpawnAndDeactivate()
    {
        agent.enabled = true;
        agent.SetDestination(spawnPosition);
        yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance < 0.3f);
        ChangeState(MinionState.Desactivat);
        agent.enabled = false;
        transform.position = spawnPosition;
    }

    public IEnumerator TraverseLink()
    {
        traversingLink = true;
        float moveSpeed = agent.speed;
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 finalDestination = agent.destination;

        float realDistance = Vector3.Distance(data.startPos, data.endPos);

        // Tots els links mesuren 1 unitat aparent, calculem la velocitat per recorrer
        // la distancia real 3D en el mateix temps que tardaria en caminar 1 unitat normal
        float expectedTime = 2f / agent.speed;
        float requiredSpeed = realDistance / expectedTime;

        agent.speed = requiredSpeed;
        agent.autoTraverseOffMeshLink = true;

        while (agent.isOnOffMeshLink)
            yield return null;

        agent.velocity = Vector3.zero;

        agent.speed = moveSpeed;
        agent.autoTraverseOffMeshLink = false;
        agent.SetDestination(finalDestination);

        traversingLink = false;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, curiosityRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}