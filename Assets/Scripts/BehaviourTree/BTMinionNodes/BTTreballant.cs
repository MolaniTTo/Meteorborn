// ── 5. TREBALLANT ────────────────────────────────────────────────────────────

using UnityEngine;

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

