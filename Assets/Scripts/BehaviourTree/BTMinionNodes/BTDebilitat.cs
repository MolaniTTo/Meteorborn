// ── 3. DEBILITAT ─────────────────────────────────────────────────────────────
// Queda parat fins que el jugador gasta partícules per reactivar-lo.

using UnityEngine;

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
