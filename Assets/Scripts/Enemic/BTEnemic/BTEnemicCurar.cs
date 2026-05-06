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
        if (!enemic.ShouldHeal()) return false;

        // Primera vegada que entra: mou el ghost al punt de curació
        if (!enemic.isHealing)
        {
            enemic.isHealing = true;
            enemic.targetHealth = null;
            enemic.MoveGhostTo(enemic.GetHealPoint());
        }

        // Cura passiva si està a prop del guardPoint
        float distToGuard = Vector3.Distance(enemic.transform.position, enemic.guardPoint.position);
        if (distToGuard <= enemic.healRadius)
        {
            enemic.energia = Mathf.Min(enemic.maxEnergia, enemic.energia + enemic.healRate * Time.deltaTime);
        }

        // Ha recuperat prou? Torna a patrullar
        if (enemic.energia >= enemic.maxEnergia * 0.8f)
        {
            enemic.isHealing = false;
        }

        return true;
    }
}