using UnityEngine;

// ── 1. CURAR ─────────────────────────────────────────────────────────────────
// Prioritat màxima. Quan l'energia baixa del 30%, va a prop del guardPoint
// a recuperar-se. No va al centre exacte per evitar el NavMeshObstacle.
// Quan recupera el 80% d'energia, torna a patrullar.

[CreateAssetMenu(fileName = "BTEnemicCurar", menuName = "BehaviourTree/Enemic/Curar")]
public class BTEnemicCurar : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        enemic.GetComponent<EnemicAIDebug>()?.SetActiveNode("CURAR");

        if (!enemic.ShouldHeal()) return false;

        if (!enemic.isHealing)
        {
            enemic.isHealing = true;
            enemic.LoseTarget();
            enemic.MoveGhostTo(enemic.GetHealPoint());
        }

        float distToGuard = Vector3.Distance(enemic.transform.position, enemic.guardPoint.position);
        if (distToGuard <= enemic.healRadius)
        {
            enemic.energia = Mathf.Min(enemic.maxEnergia, enemic.energia + enemic.healRate * Time.deltaTime);
        }

        if (enemic.energia >= enemic.maxEnergia * 0.8f)
        {
            enemic.isHealing = false;
        }

        return true;
    }
}