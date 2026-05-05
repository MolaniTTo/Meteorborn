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
                if (minion.animator != null) minion.animator.SetTrigger("CasiMort");
                return true;
            }
        }
        else
        {
            // No hi havia enemic vigilat (va caure per altra causa) → directament Debilitat
            minion.ChangeState(MinionAI.MinionState.Debilitat);
            if (minion.animator != null) minion.animator.SetTrigger("CasiMort");
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