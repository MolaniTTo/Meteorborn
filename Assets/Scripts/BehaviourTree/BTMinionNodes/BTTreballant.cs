using UnityEngine.AI;

public class BTTreballant : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Treballant) return false;
        if (minion.assignedObject == null) { minion.ChangeState(MinionAI.MinionState.Activat); return false; }

        // Comprova si el CarryObject es mou mirant la velocitat del seu agent
        bool isMoving = false;
        NavMeshAgent carryAgent = minion.assignedObject.GetComponent<NavMeshAgent>();
        if (carryAgent != null && carryAgent.enabled)
            isMoving = carryAgent.velocity.sqrMagnitude > 0.1f;

        if (minion.animator != null)
            minion.animator.SetBool("IsMoving", isMoving);


        return true;
    }
}