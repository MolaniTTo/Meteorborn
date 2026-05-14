using UnityEngine;

// ── 6. PATRULLAR (Fallback) ───────────────────────────────────────────────────
[CreateAssetMenu(fileName = "BTEnemicPatrullar", menuName = "BehaviourTree/Enemic/Patrullar")]
public class BTEnemicPatrullar : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        enemic.GetComponent<EnemicAIDebug>()?.SetActiveNode("PATRULLAR");

        enemic.isHealing = false;
        enemic.SetAttackAnimation(false);
        enemic.MoveGhostTo(enemic.GetPatrolPoint());
        enemic.HasReachedPatrolPoint();
        return true;
    }
}