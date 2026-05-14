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
        if (minion.currentState != MinionAI.MinionState.CasiMort) return false;

        // Primera vegada que entra en CasiMort
        if (!minion.nearDeathInitialized)
        {
            minion.nearDeathInitialized = true;
            minion.nearDeathTimer = 0f;
            minion.nearDeathExpired = false;
            minion.watchedEnemy = minion.attackTarget;
            minion.attackTarget = null;
            minion.agent.enabled = false;
            if (minion.animator != null) minion.animator.SetTrigger("CasiMort");
            minion.scaleController?.StartNearDeathBreath(minion.nearDeathDuration); // comença a parpadejar i respirar lentament
            minion.visualController?.StartBlink(minion.nearDeathDuration);
        }

        if (minion.nearDeathExpired) return true; // ja s'ha disparat el pop, espera destrucció

        // ── Cas A: l'enemic ha mort mentre el minion estava a terra ──────────
        if (minion.watchedEnemy != null)
        {
            HealthComponent eh = minion.watchedEnemy.GetComponent<HealthComponent>();
            if (eh == null || eh.IsDead())
            {
                minion.watchedEnemy = null;
                minion.energy = 0f;
                minion.scaleController?.StopBreath();
                minion.visualController?.StopBlink();
                minion.visualController?.MinLightAndEmission();
                minion.ChangeState(MinionAI.MinionState.Debilitat);
                return true;
            }
        }
        else
        {
            minion.scaleController?.StopBreath();
            minion.visualController?.StopBlink();
            minion.visualController?.MinLightAndEmission();
            minion.ChangeState(MinionAI.MinionState.Debilitat);
            return true;
        }

        // ── Cas B: countdown de 15 segons ────────────────────────────────────
        minion.nearDeathTimer += Time.deltaTime;
        if (minion.nearDeathTimer >= minion.nearDeathDuration)
        {
            minion.nearDeathExpired = true;
            minion.scaleController?.StopBreath();
            minion.visualController?.StopBlink();
            minion.visualController?.MinLightAndEmission();
            minion.PopToSpawn();
        }

        return true;
    }
}