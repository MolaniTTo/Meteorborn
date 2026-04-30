// ═══════════════════════════════════════════════════════════════════════════
//  BEHAVIOUR TREE NODES — MINION
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

// ── 1. CASI MORT (Prioritat màxima) ─────────────────────────────────────────
// El minion cau on estava. Vigila si l'enemic mor en 15s:
//   → Enemic mort en temps   : entra en Debilitat
//   → Temps esgotat sense mort: PopToSpawn() → Desactivat al spawnpoint

[CreateAssetMenu(fileName = "BTCasiMort", menuName = "BehaviourTree/Minion/CasiMort")]
public class BTCasiMort : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        // Condició d'entrada: energia a 0 i NO estar ja en Debilitat
        if (minion.energy > 0f) return false;
        if (minion.currentState == MinionAI.MinionState.Debilitat) return false;

        // Primera vegada que entra en CasiMort
        if (minion.currentState != MinionAI.MinionState.CasiMort)
        {
            minion.ChangeState(MinionAI.MinionState.CasiMort);
            minion.nearDeathTimer = 0f;
            minion.nearDeathExpired = false;

            // Guarda l'enemic que estava atacant per vigilar-lo
            minion.watchedEnemy = minion.attackTarget;
            minion.attackTarget = null;

            minion.agent.enabled = false; // queda al terra
            if (minion.animator != null) minion.animator.SetTrigger("CasiMort");
        }

        if (minion.nearDeathExpired) return true; // ja s'ha disparat el pop, espera destrucció

        // ── Cas A: l'enemic ha mort mentre el minion estava a terra ──────────
        if (minion.watchedEnemy != null)
        {
            EnemyHealth eh = minion.watchedEnemy.GetComponent<EnemyHealth>();
            if (eh == null || eh.IsDead())
            {
                // L'enemic ha mort → entra en Debilitat com els normals
                minion.watchedEnemy = null;
                minion.energy = 0f; // segueix amb 0 energia (Debilitat no necessita energia)
                minion.ChangeState(MinionAI.MinionState.Debilitat);
                if (minion.animator != null) minion.animator.SetTrigger("Debilitat");
                return true;
            }
        }
        else
        {
            // No hi havia enemic vigilat (va caure per altra causa) → directament Debilitat
            minion.ChangeState(MinionAI.MinionState.Debilitat);
            if (minion.animator != null) minion.animator.SetTrigger("Debilitat");
            return true;
        }

        // ── Cas B: countdown de 15 segons ────────────────────────────────────
        minion.nearDeathTimer += Time.deltaTime;
        if (minion.nearDeathTimer >= minion.nearDeathDuration)
        {
            minion.nearDeathExpired = true;
            minion.PopToSpawn(); // teleport al spawn en Desactivat
        }

        return true;
    }
}


// ── 2. ATACAR ────────────────────────────────────────────────────────────────

[CreateAssetMenu(fileName = "BTAtacar", menuName = "BehaviourTree/Minion/Atacar")]
public class BTAtacar : BTNode
{

    public override bool Execute(MinionAI minion)
    {

        if (minion.currentState != MinionAI.MinionState.Atacar) return false; //Si no esta en estat d'atacar, no fa res i retorna false per passar al següent node del selector

        if (minion.attackTarget == null) //Si no te objectiu d'atac, canvia a estat Activat, se suposa que si estava atacant s'ha mort l'enemic
        {
            minion.ChangeState(MinionAI.MinionState.Activat);
            return false;
        }

        float dist = Vector3.Distance(minion.transform.position, minion.attackTarget.position); //Calcula la distancia al objectiu d'atac

        if (dist > minion.attackRange) //si la distanciancia al objectiu d'atac es major que el rang d'atac, es mou cap a ell
        {
            minion.agent.isStopped = false;
            minion.agent.SetDestination(minion.attackTarget.position);
            if (minion.animator != null) minion.animator.SetBool("IsMoving", true);
        }
        else //si esta dins del rang d'atac, atura el minion i comença a atacar (drain d'energia i dany a l'enemic)
        {
            minion.agent.isStopped = true;
            if (minion.animator != null) minion.animator.SetBool("IsMoving", false);

            EnemyHealth enemyHealth = minion.attackTarget.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                float drain = minion.energyDrainPerSecond * Time.deltaTime;
                enemyHealth.TakeDamage(drain);
                minion.energy = Mathf.Min(minion.maxEnergy, minion.energy + drain);

                if (minion.animator != null) minion.animator.SetTrigger("Attack");

                if (enemyHealth.IsDead())
                {
                    // L'enemic ha mort → Debilitat (esgotat per la lluita)
                    minion.attackTarget = null;
                    minion.energy = 0f;
                    minion.ChangeState(MinionAI.MinionState.Debilitat);
                    if (minion.animator != null) minion.animator.SetTrigger("Debilitat");
                }
            }
            else
            {
                minion.attackTarget = null;
                minion.energy = 0f;
                minion.ChangeState(MinionAI.MinionState.Debilitat);

            }
        }

        return true;
    }
}


// ── 3. DEBILITAT ─────────────────────────────────────────────────────────────
// Queda parat fins que el jugador gasta partícules per reactivar-lo.

[CreateAssetMenu(fileName = "BTDebilitat", menuName = "BehaviourTree/Minion/Debilitat")]
public class BTDebilitat : BTNode
{


    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Debilitat) return false;
        minion.agent.isStopped = true;
        if (minion.animator != null) minion.animator.SetBool("IsMoving", false);
        return true;
    }
}


// ── 4. ACTIVAT ───────────────────────────────────────────────────────────────

[CreateAssetMenu(fileName = "BTActivat", menuName = "BehaviourTree/Minion/Activat")]
public class BTActivat : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Activat) return false;
        minion.agent.isStopped = false;
        if (minion.animator != null)
            minion.animator.SetBool("IsMoving", minion.agent.velocity.sqrMagnitude > 0.1f);
        return true;
    }
}


// ── 5. TREBALLANT ────────────────────────────────────────────────────────────

[CreateAssetMenu(fileName = "BTTreballant", menuName = "BehaviourTree/Minion/Treballant")]
public class BTTreballant : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Treballant) return false;
        if (minion.assignedObject == null) { minion.ChangeState(MinionAI.MinionState.Activat); return false; }
        if (minion.animator != null)
            minion.animator.SetBool("IsMoving", minion.agent.velocity.sqrMagnitude > 0.1f);
        return true;
    }
}


// ── 6. DESACTIVAT (Fallback) ─────────────────────────────────────────────────

[CreateAssetMenu(fileName = "BTDesactivat", menuName = "BehaviourTree/Minion/Desactivat")]
public class BTDesactivat : BTNode
{


    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Desactivat) return false;

        minion.agent.isStopped = true;

        if (minion.playerTransform != null)
        {
            float dist = Vector3.Distance(minion.transform.position, minion.playerTransform.position);
            bool curious = dist < minion.curiosityRadius;

            if (curious)
            {
                Vector3 dir = (minion.playerTransform.position - minion.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    minion.transform.rotation = Quaternion.Slerp(
                        minion.transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * 3f);
            }

            if (minion.animator != null)
                minion.animator.SetBool("IsCurious", curious);
        }

        return true;
    }
}