using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

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

    [Header("UI")]
    [SerializeField] private GameObject minionCountPanel;
    [SerializeField] private TMPro.TMP_Text minionCountText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip carrySound; //es reprodueix quan l'objecte puja a la posició elevada abans de moure's

    [Header("Blueprint")]
    [SerializeField] private GameObject blueprintPanel;
    [SerializeField] private Color blueprintPlacedColor = new Color(0.4f, 0.7f, 0.4f, 0.7f);
    private enum CarryMode { FollowPlayer, GoToDestination }
    private CarryMode carryMode = CarryMode.FollowPlayer;

    [Header("Trigger de destí")]
    [SerializeField] private string destinationTriggerTag = "CarryDestinationZone";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        UpdateMinionCountUI();
    }
    private void UpdateMinionCountUI()
    {
        if (minionCountText != null)
            minionCountText.text = $"{assignedMinions.Count}/{minionsRequired}";
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
        UpdateMinionCountUI();
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
        carryMode = CarryMode.FollowPlayer;
        if (minionCountPanel != null) minionCountPanel.SetActive(false);

        float floatHeight = 1.5f;
        float floatDuration = 0.8f;

        if (audioSource != null && carrySound != null)
            audioSource.PlayOneShot(carrySound);

        Vector3 originalLocalPos = carryObject.transform.localPosition;
        Vector3 raisedLocalPos = originalLocalPos + Vector3.up * floatHeight;

        bool elevated = false;
        carryObject.transform.DOLocalMove(raisedLocalPos, floatDuration).SetEase(Ease.OutSine)
            .OnComplete(() => elevated = true);
        yield return new WaitUntil(() => elevated);

        bool reset = false;
        carryObject.transform.DOLocalMove(originalLocalPos, 0.2f)
            .OnComplete(() => reset = true);
        yield return new WaitUntil(() => reset);

        Transform playerFollow = null;
        while (playerFollow == null)
        {
            playerFollow = GameObject.FindGameObjectWithTag("PlayerCarryingFollow")?.transform;
            yield return null;
        }

        yield return new WaitUntil(() =>
            NavMesh.SamplePosition(playerFollow.position, out _, 1f, NavMesh.AllAreas));

        if (agent != null)
        {
            NavMeshObstacle obstacle = carryObject.GetComponent<NavMeshObstacle>();
            if (obstacle != null) obstacle.enabled = false;
            agent.enabled = true;
            agent.speed = moveSpeed;
            agent.autoTraverseOffMeshLink = false;
            agent.Warp(transform.position);
        }


        while (true)
        {
            if (isDelivered) yield break;

            if (agent.isOnOffMeshLink && !traversingCarryLink)
                StartCoroutine(TraverseCarryLink());

            if (carryMode == CarryMode.FollowPlayer && playerFollow != null)
            {
                agent.stoppingDistance = 1.5f;
                agent.SetDestination(playerFollow.position);
            }
            else if (carryMode == CarryMode.GoToDestination && destination != null)
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

    private bool traversingCarryLink = false;

    private IEnumerator TraverseCarryLink()
    {
        traversingCarryLink = true;
        float originalSpeed = agent.speed;
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        float realDistance = Vector3.Distance(data.startPos, data.endPos);
        float requiredSpeed = realDistance / (2f / agent.speed);
        agent.speed = requiredSpeed;
        agent.autoTraverseOffMeshLink = true;
        while (agent.isOnOffMeshLink) yield return null;
        agent.velocity = Vector3.zero;
        agent.speed = originalSpeed;
        agent.autoTraverseOffMeshLink = false;
        traversingCarryLink = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(destinationTriggerTag) && isBeingCarried)
        {
            carryMode = CarryMode.GoToDestination;
        }
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

    public void ActivateBlueprint()
    {
        if (blueprintPanel == null) return;
        Image img = blueprintPanel.GetComponent<Image>();
        if (img == null) img = blueprintPanel.GetComponentInChildren<Image>();
        if (img != null) img.color = blueprintPlacedColor;
    }

    private bool AllMinionsReady()
    {
        for (int i = 0; i < assignedMinions.Count; i++)
        {
            if (assignedMinions[i] == null) continue;
            if (i >= carryPositions.Length) continue;

            Vector3 mPos = assignedMinions[i].transform.position; mPos.y = 0f;
            Vector3 sPos = carryPositions[i].position; sPos.y = 0f;

            if (Vector3.Distance(mPos, sPos) > 2f) return false;
        }
        return true;
    }

    public bool IsDelivered => isDelivered;
    public int AssignedCount => assignedMinions.Count;
}