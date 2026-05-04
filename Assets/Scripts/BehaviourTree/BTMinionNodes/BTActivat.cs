// ── 4. ACTIVAT ───────────────────────────────────────────────────────────────

using UnityEngine;

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
