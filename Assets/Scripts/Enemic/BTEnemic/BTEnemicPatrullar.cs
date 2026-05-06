// ── 6. PATRULLAR (Fallback) ───────────────────────────────────────────────────
// Fallback quan no passa res. Mou el ghost entre punts aleatoris dins del
// patrolRadius al voltant del guardPoint, adaptant-se al NavMesh disponible.
using UnityEngine;

[CreateAssetMenu(fileName = "BTEnemicPatrullar", menuName = "BehaviourTree/Enemic/Patrullar")]
public class BTEnemicPatrullar : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        enemic.isHealing = false; // reset per si de cas
        enemic.MoveGhostTo(enemic.GetPatrolPoint());
        enemic.HasReachedPatrolPoint(); // genera nou punt si ha arribat
        return true;
    }
}