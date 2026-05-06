using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MinionManager : MonoBehaviour
{
    public static MinionManager Instance { get; private set; }

    [Header("Cursor")]
    public MinionCursor cursor;

    [Header("Costs de partícules")]
    [Tooltip("Cost per activar un minion desactivat")]
    public int activationCost = 3;
    [Tooltip("Cost per reactivar un minion en estat Debilitat")]
    public int weaknessCost = 5;

    [Header("Radi d'activació del cursor")]
    public float activationRadius = 2f;
    public float reactivationRadius = 2f;

    [Header("Llançament")]
    public float launchArcHeight = 3f;
    public float launchDuration = 1.2f;

    public PlayerStateMachine player;

    private MinionAI pendingLaunch = null;
    private Vector3 pendingLaunchTarget;

    private float pendingLaunchTimer = 0f;
    private const float maxPendingTime = 5f;

    // ── Llistes internes ──────────────────────────────────────────────────────
    [SerializeField] private List<MinionAI> allMinions = new List<MinionAI>();
    [SerializeField] private List<MinionAI> activeMinions = new List<MinionAI>();

    // ── Highlight: minion sota el cursor pendent de confirmar ─────────────────
    private MinionAI pendingMinion = null;   // el que el cursor té a sobre
    private bool pendingIsReactivation = false;

    public MinionAI GetPendingLaunch() => pendingLaunch;

    public void ClearPendingLaunch()
    {
        pendingLaunch = null;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (pendingLaunch != null)
        {
            pendingLaunchTimer += Time.deltaTime;
            if (pendingLaunchTimer > maxPendingTime)
            {
                Debug.LogWarning("[MinionManager] pendingLaunch timeout, netejant");
                pendingLaunch = null;
                pendingLaunchTimer = 0f;
            }
        }
        else
        {
            pendingLaunchTimer = 0f;
        }

        if (cursor == null || !cursor.IsActive)
        {
            ClearPending();
            return;
        }

        UpdatePendingHighlight(cursor.WorldPosition);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Registre de minions (cridat pels MinionSpawners)
    // ────────────────────────────────────────────────────────────────────────

    public void RegisterMinion(MinionAI minion)
    {
        if (!allMinions.Contains(minion))
            allMinions.Add(minion);
    }

    public void UnregisterMinion(MinionAI minion)
    {
        allMinions.Remove(minion);
        activeMinions.Remove(minion);
        if (pendingMinion == minion) ClearPending();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Highlight: cada frame que el cursor passa per sobre d'un minion
    // es marca com a "pendent" però NO es cobra res fins a RT
    // ────────────────────────────────────────────────────────────────────────

    private void UpdatePendingHighlight(Vector3 cursorPos)
    {
        MinionAI closest = null;
        float closestDist = float.MaxValue;
        bool isReactivation = false;

        foreach (MinionAI minion in allMinions)
        {
            float dist = Vector3.Distance(minion.transform.position, cursorPos);

            if (minion.currentState == MinionAI.MinionState.Desactivat && dist <= activationRadius)
            {
                if (dist < closestDist) { closestDist = dist; closest = minion; isReactivation = false; }
            }
            else if (minion.currentState == MinionAI.MinionState.Debilitat && dist <= reactivationRadius)
            {
                if (dist < closestDist) { closestDist = dist; closest = minion; isReactivation = true; }
            }
        }

        // Neteja el highlight anterior si ha canviat
        if (pendingMinion != closest)
        {
            if (pendingMinion != null) pendingMinion.isHighlighted = false;
            pendingMinion = closest;
            pendingIsReactivation = isReactivation;
            if (pendingMinion != null) pendingMinion.isHighlighted = true;
        }
    }

    private void ClearPending()
    {
        if (pendingMinion != null) pendingMinion.isHighlighted = false;
        pendingMinion = null;
        pendingIsReactivation = false;
    }

    // ────────────────────────────────────────────────────────────────────────
    // RT confirmat: activa o reactiva el minion pendent si hi ha prou partícules
    // Cridat des de MinionCursor quan RT és premut
    // ────────────────────────────────────────────────────────────────────────

    public void ConfirmCursorAction(Vector3 cursorWorldPos, Vector3 playerThrowPos)
    {
        // Prioritat 1: hi ha un minion pendent sota el cursor → activar/reactivar
        if (pendingMinion != null)
        {
            int cost = pendingIsReactivation ? weaknessCost : activationCost;

            if (!PlayerParticles.Instance.HasEnough(cost))
            {
                Debug.Log($"[Minions] No hi ha prou partícules! Calen {cost}, tens {PlayerParticles.Instance.Current}");
                return;
            }

            PlayerParticles.Instance.Spend(cost);

            if (pendingIsReactivation) //Si esta en debilitat, el reactiva, sino el activa normalment
            {
                pendingMinion.ReactivateFromWeakness();
                RegisterActive(pendingMinion);
                Debug.Log($"[Minions] Minion reactivat des de Debilitat. Cost: {cost}p. Queden: {PlayerParticles.Instance.Current}p");
            }
            else
            {
                pendingMinion.Activate();
                RegisterActive(pendingMinion);
                Debug.Log($"[Minions] Minion activat. Cost: {cost}p. Queden: {PlayerParticles.Instance.Current}p");
            }

            ClearPending();
            return;
        }

        // Prioritat 2: no hi ha minion pendent → llança un minion actiu cap al cursor
        LaunchMinionToCursor(cursorWorldPos, playerThrowPos);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Llançament
    // ────────────────────────────────────────────────────────────────────────

    public void LaunchMinionToCursor(Vector3 cursorWorldPos, Vector3 playerThrowPos)
    {
        Debug.Log($"[Launch] pendingLaunch={pendingLaunch}, activeMinions={activeMinions.Count}");

        if (pendingLaunch != null)
        {
            Debug.Log("[Launch] BLOQUEJAT per pendingLaunch actiu");
            return;
        }

        if (activeMinions.Count == 0)
        {
            Debug.Log("[Launch] No hi ha minions actius");
            return;
        }

        MinionAI toThrow = activeMinions
            .OrderBy(m => Vector3.Distance(m.transform.position, playerThrowPos))
            .First();

        float dist = Vector3.Distance(toThrow.transform.position, playerThrowPos);
        Debug.Log($"[Launch] Minion mes proper: {toThrow.name}, dist={dist}");

        if (dist > 2f)
        {
            Debug.Log("[Launch] Minion massa lluny, no es llança");
            return;
        }

        activeMinions.Remove(toThrow);
        pendingLaunch = toThrow;
        pendingLaunchTarget = cursorWorldPos;

        Debug.Log($"[Launch] Llançant {toThrow.name}, activeMinions restants={activeMinions.Count}");
        player.PlayThrowAnimation();
    }

    public void ExecutePendingLaunch()
    {
        Debug.Log($"[Execute] pendingLaunch={pendingLaunch?.name ?? "NULL"}");
        if (pendingLaunch == null) return;
        pendingLaunch.LaunchTo(pendingLaunchTarget, launchArcHeight, launchDuration);
        pendingLaunch = null;
        pendingLaunchTimer = 0f;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Gestió de llistes
    // ────────────────────────────────────────────────────────────────────────

    public void RegisterActive(MinionAI minion)
    {
        if (!activeMinions.Contains(minion)) activeMinions.Add(minion);
    }

    public void UnregisterActive(MinionAI minion)
    {
        activeMinions.Remove(minion);
    }

    public int ActiveCount => activeMinions.Count;

    // ────────────────────────────────────────────────────────────────────────
    // Assignació externa de tasques
    // ────────────────────────────────────────────────────────────────────────

    public void AssignAttackToFirst(Transform enemy)
    {
        if (activeMinions.Count == 0) return;
        MinionAI m = activeMinions[0];
        activeMinions.RemoveAt(0);
        m.AssignAttackTarget(enemy);
    }

    public void AssignCarryObject(CarryObject obj, int count)
    {
        int assigned = 0;
        for (int i = activeMinions.Count - 1; i >= 0 && assigned < count; i--)
        {
            activeMinions[i].AssignCarryObject(obj);
            activeMinions.RemoveAt(i);
            assigned++;
        }
    }
}