using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Objecte que els minions poden portar entre tots.
/// Quan tots els minions assignats estan a posició, l'objecte es mou al destí.
/// </summary>
public class CarryObject : MonoBehaviour
{
    [Header("Configuració")]
    public int minionsRequired = 2;         // quants minions calen per moure'l
    public Transform destination;           // on s'ha de portar
    public float moveSpeed = 2f;
    public GameObject carryObject;

    [Header("Posicions per als minions")]
    public Transform[] carryPositions;      // punts on es col·loquen els minions al voltant

    // ── Estat intern ──────────────────────────────────────────────────────────
    private List<MinionAI> assignedMinions = new List<MinionAI>();
    private NavMeshAgent agent;
    private bool isBeingCarried = false;
    private bool isDelivered = false;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip carrySound; //es reprodueix quan l'objecte puja a la posició elevada abans de moure's

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
    }

    public void AssignMinion(MinionAI minion)
    {
        if (isDelivered) return;
        if (assignedMinions.Contains(minion)) return;

        assignedMinions.Add(minion);

        int index = assignedMinions.Count - 1;
        if (index < carryPositions.Length)
        {
            // Destí amb la Y del minion, no del slot
            Vector3 dest = carryPositions[index].position;
            dest.y = minion.transform.position.y;
            minion.agent.SetDestination(dest);
            minion.StartCoroutine(WaitAndReady(minion, carryPositions[index]));
        }
    }

    private int readyMinions = 0;

    private IEnumerator WaitAndReady(MinionAI minion, Transform slot)
    {
        // Espera fins que arribi al punt (només XZ)
        yield return new WaitUntil(() =>
        {
            Vector3 mPos = minion.transform.position; mPos.y = 0f;
            Vector3 sPos = slot.position; sPos.y = 0f;
            return Vector3.Distance(mPos, sPos) < 0.4f;
        });

        // Gira cap a l'objecte
        Vector3 dir = transform.position - minion.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            minion.transform.rotation = Quaternion.LookRotation(dir);

        readyMinions++;
        if (readyMinions >= minionsRequired && !isBeingCarried)
            StartCoroutine(CarryToDestination());
    }

    private IEnumerator CarryToDestination()
    {
        isBeingCarried = true;

        // Elevació abans de moure's
        float floatHeight = 1.5f;
        float floatDuration = 0.8f;
        Vector3 raisedPos = carryObject.transform.position + Vector3.up * floatHeight;

        if (audioSource != null && carrySound != null)
            audioSource.PlayOneShot(carrySound);
        bool elevated = false;
        carryObject.transform.DOMove(raisedPos, floatDuration).SetEase(Ease.OutSine).OnComplete(() => elevated = true);
        yield return new WaitUntil(() => elevated);

        // Ara activem el NavMeshAgent per moure's
        if (agent != null) { agent.enabled = true; agent.speed = moveSpeed; }

        Transform playerFollow = GameObject.FindGameObjectWithTag("PlayerCarryingFollow")?.transform;


        while (true)
        {
            // Si ja s'ha alliberat (per MeteoritColocar), sortim
            if (isDelivered) yield break;

            if (destination == null) break;

            bool hasLinks = PathHasOffMeshLinks(transform.position, destination.position);

            if (hasLinks && playerFollow != null)
            {
                agent.stoppingDistance = 2f;
                float distToPlayer = Vector3.Distance(transform.position, playerFollow.position);
                if (distToPlayer > agent.stoppingDistance)
                    agent.SetDestination(playerFollow.position);
            }
            else
            {
                agent.stoppingDistance = 0.3f;
                agent.SetDestination(destination.position);
                if (!agent.pathPending && agent.remainingDistance < 0.3f)
                    break;
            }

            for (int i = 0; i < assignedMinions.Count; i++)
            {
                if (i < carryPositions.Length
                    && assignedMinions[i] != null
                    && assignedMinions[i].agent != null
                    && assignedMinions[i].agent.enabled)
                {
                    Vector3 dest = carryPositions[i].position;
                    dest.y = assignedMinions[i].transform.position.y;
                    assignedMinions[i].agent.stoppingDistance = 0.5f;
                    assignedMinions[i].agent.SetDestination(dest);
                }
            }

            yield return null;
        }

    }

    private bool PathHasOffMeshLinks(Vector3 from, Vector3 to)
    {
        // Si hay un raycast directo de NavMesh sin interrupciones, no hay links
        if (NavMesh.Raycast(from, to, out NavMeshHit hit, NavMesh.AllAreas))
        {
            // El raycast fue bloqueado → hay un hueco en el NavMesh → necesita link
            return true;
        }
        return false;
    }

    public void OnDelivered()
    {
        isDelivered = true;

        UniqueID uid = GetComponent<UniqueID>();
        if (uid != null) { WorldManager.Instance?.RegisterMovedObject(uid.ID, transform.position, transform.rotation); }
        SaveManager.Instance?.Save();
        Debug.Log($"[CarryObject] Objecte '{name}' entregat a la destinació '{destination.name}'. Minions alliberats i dades guardades.");
    }

    public void ReleaseMinions()
    {
        isDelivered = true;

        if (agent != null) agent.enabled = false; // atura el CarryObject també

        foreach (MinionAI minion in assignedMinions)
        {
            minion.agent.enabled = true;
            minion.assignedObject = null;
            minion.ChangeState(MinionAI.MinionState.Activat);
            MinionManager.Instance?.RegisterActive(minion);
        }
        assignedMinions.Clear();
    }

    public bool IsDelivered => isDelivered;
    public int AssignedCount => assignedMinions.Count;
}