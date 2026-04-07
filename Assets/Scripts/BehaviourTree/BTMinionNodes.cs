// ═══════════════════════════════════════════════════════════════════════════
//  BEHAVIOUR TREE NODES — MINION
//  Cada node és un ScriptableObject que es crea des de Assets > BehaviourTree
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

// ── 1. CASI MORT (Prioritat màxima) ─────────────────────────────────────────
// Comprova si l'energia és 0 i gestiona el countdown de 15 segons.
// Si expira sense ser reactivat → torna al spawn desactivat.

[CreateAssetMenu(fileName = "BTCasiMort", menuName = "BehaviourTree/Minion/CasiMort")]
public class BTCasiMort : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        if (minion.energy > 0f) return false; // no és casi mort
        if (minion.currentState == MinionAI.MinionState.Debilitat) return false; // debilitat té la seva pròpia branca

        // Entra en estat CasiMort si no hi era
        if (minion.currentState != MinionAI.MinionState.CasiMort)
        {
            minion.ChangeState(MinionAI.MinionState.CasiMort);
            minion.nearDeathTimer = 0f;
            minion.nearDeathExpired = false;
            minion.agent.enabled = false; // queda parat
            if (minion.animator != null)
            {
                minion.animator.SetTrigger("CasiMort");
            }
        }

        // Compta el temps
        if (!minion.nearDeathExpired)
        {
            minion.nearDeathTimer += Time.deltaTime;
            if (minion.nearDeathTimer >= minion.nearDeathDuration)
            {
                // Temps exhaurit → torna al spawn
                minion.nearDeathExpired = true;
                minion.StartCoroutine(minion.ReturnToSpawnAndDeactivate());
            }
        }

        return true; //mentre estigui en CasiMort, aquest node té el control
    }
}


// ── 2. ATACAR ────────────────────────────────────────────────────────────────
// Persegueix l'objectiu i absorbeix energia. Quan l'enemic mor → Debilitat.

[CreateAssetMenu(fileName = "BTAtacar", menuName = "BehaviourTree/Minion/Atacar")]
public class BTAtacar : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Atacar) return false;
        if (minion.attackTarget == null)
        {
            // Objectiu perdut → torna a Activat
            minion.ChangeState(MinionAI.MinionState.Activat);
            return false;
        }

        float dist = Vector3.Distance(minion.transform.position, minion.attackTarget.position);

        if (dist > minion.attackRange)
        {
            // Persegueix
            minion.agent.isStopped = false;
            minion.agent.SetDestination(minion.attackTarget.position);
            if (minion.animator != null) minion.animator.SetBool("IsMoving", true);
        }
        else
        {
            // Ataca: absorbeix energia de l'enemic
            minion.agent.isStopped = true;

            // Busca un component d'energia a l'enemic (adapta-ho al teu sistema)
            EnemyHealth enemyHealth = minion.attackTarget.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                float drain = minion.energyDrainPerSecond * Time.deltaTime;
                enemyHealth.TakeDamage(drain);
                minion.energy = Mathf.Min(minion.maxEnergy, minion.energy + drain); // absorbeix

                if (minion.animator != null) minion.animator.SetTrigger("Attack");

                // Enemic mort?
                if (enemyHealth.IsDead())
                {
                    minion.attackTarget = null;
                    minion.energy = 0f; // queda esgotat
                    minion.ChangeState(MinionAI.MinionState.Debilitat);
                    if (minion.animator != null) minion.animator.SetTrigger("Debilitat");
                }
            }
            else
            {
                // L'enemic no té component d'energia → combat simple
                minion.attackTarget = null;
                minion.energy = 0f;
                minion.ChangeState(MinionAI.MinionState.Debilitat);
            }
        }

        return true;
    }
}


// ── 3. DEBILITAT ─────────────────────────────────────────────────────────────
// Post-combat. Queda parat fins que el jugador el reactiva amb partícules.

[CreateAssetMenu(fileName = "BTDebilitat", menuName = "BehaviourTree/Minion/Debilitat")]
public class BTDebilitat : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Debilitat) return false;

        // Queda parat esperant reactivació (la reactivació la fa MinionManager)
        minion.agent.isStopped = true;

        return true;
    }
}


// ── 4. ACTIVAT ───────────────────────────────────────────────────────────────
// Segueix el jugador en idle. La lògica de seguiment és a MinionAI (coroutine).

[CreateAssetMenu(fileName = "BTActivat", menuName = "BehaviourTree/Minion/Activat")]
public class BTActivat : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Activat) return false;

        minion.agent.isStopped = false;

        if (minion.animator != null)
        {
            bool isMoving = minion.agent.velocity.sqrMagnitude > 0.1f;
            minion.animator.SetBool("IsMoving", isMoving);
        }

        return true;
    }
}


// ── 5. TREBALLANT ────────────────────────────────────────────────────────────
// S'apropa a l'objecte, l'agafa amb els altres minions i desapareix en núvol.

[CreateAssetMenu(fileName = "BTTreballant", menuName = "BehaviourTree/Minion/Treballant")]
public class BTTreballant : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Treballant) return false;

        if (minion.assignedObject == null)
        {
            minion.ChangeState(MinionAI.MinionState.Activat);
            return false;
        }

        // La lògica de portar l'objecte la gestiona CarryObject + MinionManager
        // Aquest node simplement manté l'estat actiu i mou cap a la posició assignada
        if (minion.animator != null)
        {
            bool isMoving = minion.agent.velocity.sqrMagnitude > 0.1f;
            minion.animator.SetBool("IsMoving", isMoving);
        }

        return true;
    }
}


// ── 6. DESACTIVAT (Fallback) ─────────────────────────────────────────────────
// Punt fix. Si el jugador s'acosta, mira amb curiositat.

[CreateAssetMenu(fileName = "BTDesactivat", menuName = "BehaviourTree/Minion/Desactivat")]
public class BTDesactivat : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Desactivat) return false;

        minion.agent.isStopped = true;

        // Curiositat: si el jugador s'acosta, mira cap a ell
        if (minion.playerTransform != null)
        {
            float dist = Vector3.Distance(minion.transform.position, minion.playerTransform.position);
            if (dist < minion.curiosityRadius)
            {
                Vector3 dir = (minion.playerTransform.position - minion.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion target = Quaternion.LookRotation(dir);
                    minion.transform.rotation = Quaternion.Slerp(
                        minion.transform.rotation, target, Time.deltaTime * 3f);
                }
                if (minion.animator != null)
                    minion.animator.SetBool("IsCurious", true);
            }
            else
            {
                if (minion.animator != null)
                    minion.animator.SetBool("IsCurious", false);
            }
        }

        return true;
    }
}