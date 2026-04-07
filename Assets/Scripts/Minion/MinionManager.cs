using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class MinionManager : MonoBehaviour
{
    public static MinionManager Instance { get; private set; } // Singleton per accés global des de MinionCursor i altres scripts

    [Header("Llistes de Minions")]
    private List<MinionAI> allMinions = new List<MinionAI>();
    private List<MinionAI> activeMinions = new List<MinionAI>(); // estat Activat

    [Header("Cursor")]
    public MinionCursor cursor; // assigna des de l'inspector

    [Header("Activació per esfera (LT)")]
    public float activationRadius = 2f; // radi del cursor per activar minions

    [Header("Llançament (RT)")]
    public float launchArcHeight = 3f;
    public float launchDuration = 0.6f;

    [Header("Reactivació (Debilitat / CasiMort)")]
    public float reactivationRadius = 2f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Recull tots els minions de l'escena
        allMinions = FindObjectsOfType<MinionAI>().ToList();
    }

    // ── Cridades des de MinionCursor ──────────────────────────────────────────
    //Quan LT es mantingut, activa els minions desactivats a prop del cursor.

    public void TryActivateMinionsAtCursor(Vector3 cursorWorldPos)
    {
        foreach (MinionAI minion in allMinions)
        {
            if (minion.currentState != MinionAI.MinionState.Desactivat) continue;
            float dist = Vector3.Distance(minion.transform.position, cursorWorldPos);
            if (dist <= activationRadius)
            {
                minion.Activate();
                RegisterActive(minion);
            }
        }
    }

    //Quan RT es premut, llança el minion actiu mes proper a la posicio del cursor de manera parabolica.
    public void LaunchMinionToCursor(Vector3 cursorWorldPos, Vector3 playerPos)
    {
        if (activeMinions.Count == 0) return;

        // Agafa el minion actiu més proper al jugador
        MinionAI toThrow = activeMinions
            .OrderBy(m => Vector3.Distance(m.transform.position, playerPos))
            .First();

        activeMinions.Remove(toThrow);
        toThrow.LaunchTo(cursorWorldPos, launchArcHeight, launchDuration);
    }

    //Reactiva un minion a prop del cursor que estigui en estat Debilitat o CasiMort.
    public void TryReactivateAtCursor(Vector3 cursorWorldPos)
    {
        foreach (MinionAI minion in allMinions)
        {
            float dist = Vector3.Distance(minion.transform.position, cursorWorldPos);
            if (dist > reactivationRadius) continue;

            if (minion.currentState == MinionAI.MinionState.Debilitat)
            {
                minion.ReactivateFromWeakness();
                RegisterActive(minion);
            }
            else if (minion.currentState == MinionAI.MinionState.CasiMort
                     && !minion.nearDeathExpired)
            {
                // Comprova si hi ha un enemic a prop per tornar directament a Atacar
                Collider[] hits = Physics.OverlapSphere(cursorWorldPos, 2f);
                Transform enemy = null;
                foreach (Collider col in hits)
                    if (col.CompareTag("Enemy")) { enemy = col.transform; break; }

                if (enemy != null)
                    minion.ReactivateToAttack(enemy);
                else
                {
                    minion.ReactivateFromWeakness();
                    RegisterActive(minion);
                }
            }
        }
    }

    // ── Gestió interna de la llista ───────────────────────────────────────────

    public void RegisterActive(MinionAI minion)
    {
        if (!activeMinions.Contains(minion))
            activeMinions.Add(minion);
    }

    public void UnregisterActive(MinionAI minion)
    {
        activeMinions.Remove(minion);
    }

    public int ActiveCount => activeMinions.Count;

    // ── Assignació de tasques des de l'exterior ───────────────────────────────

    //Assigna l'enemic a atacar al primer minion actiu de la llista.
    public void AssignAttackToFirst(Transform enemy)
    {
        if (activeMinions.Count == 0) return;
        MinionAI minion = activeMinions[0];
        activeMinions.RemoveAt(0);
        minion.AssignAttackTarget(enemy);
    }

    //Assigna el objecte a portar al primer minion actiu de la llista.
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