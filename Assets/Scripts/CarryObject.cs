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

    [Header("Posicions per als minions")]
    public Transform[] carryPositions;      // punts on es col·loquen els minions al voltant

    // ── Estat intern ──────────────────────────────────────────────────────────
    private List<MinionAI> assignedMinions = new List<MinionAI>();
    private NavMeshAgent agent;
    private bool isBeingCarried = false;
    private bool isDelivered = false;

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

        // Porta el minion a la seva posició de transport
        int index = assignedMinions.Count - 1;
        if (index < carryPositions.Length)
        {
            minion.agent.SetDestination(carryPositions[index].position);
            minion.StartCoroutine(WaitAndAttach(minion, carryPositions[index]));
        }

        // Si ja tenim prou minions, comencem a moure l'objecte
        if (assignedMinions.Count >= minionsRequired && !isBeingCarried)
            StartCoroutine(CarryToDestination());
    }

    private IEnumerator WaitAndAttach(MinionAI minion, Transform slot)
    {
        // Espera fins que el minion arribi a la posició
        yield return new WaitUntil(() =>
            !minion.agent.pathPending && minion.agent.remainingDistance < 0.3f);

        // Enganxa el minion a la posició de transport
        minion.agent.enabled = false;
        minion.transform.SetParent(slot);
        minion.transform.localPosition = Vector3.zero;
        minion.transform.localRotation = Quaternion.identity;
    }

    private IEnumerator CarryToDestination()
    {
        isBeingCarried = true;
        if (agent != null) { agent.enabled = true; agent.speed = moveSpeed; }

        // Espera que tots els minions estiguin a posició
        yield return new WaitForSeconds(0.5f);

        if (destination != null)
        {
            if (agent != null)
                agent.SetDestination(destination.position);

            yield return new WaitUntil(() =>
                agent != null && !agent.pathPending && agent.remainingDistance < 0.3f);
        }

        // Entrega completada
        OnDelivered();
    }

    private void OnDelivered()
    {
        isDelivered = true;

        // Allibera els minions (desapareixen en núvol de fum)
        foreach (MinionAI minion in assignedMinions)
        {
            minion.transform.SetParent(null);
            // Aquí pots afegir un efecte de fum i Destroy
            //Destroy(minion.gameObject, 0.1f);
            minion.PopToSpawn();
        }
        assignedMinions.Clear();

        // Aquí pots afegir la lògica de captura (DOTween, efectes, etc.)
        //Destroy(gameObject, 0.5f);
    }

    public bool IsDelivered => isDelivered;
    public int AssignedCount => assignedMinions.Count;
}