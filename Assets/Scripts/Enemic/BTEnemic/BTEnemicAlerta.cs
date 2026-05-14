using UnityEngine;

// ── 4. ALERTA ─────────────────────────────────────────────────────────────────
// Detecció 360° sense visió (alertRadius).
// Actua NOMÉS si no tenim target ja assignat.
// Assigna el target i crida TriggerScream → el node Scream (prioritat 2)
// agafarà el control el proper frame.
[CreateAssetMenu(fileName = "BTEnemicAlerta", menuName = "BehaviourTree/Enemic/Alerta")]
public class BTEnemicAlerta : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        // Si ja tenim un target viu, Atacar/Perseguir ho gestionen
        if (enemic.targetHealth != null && !enemic.targetHealth.IsDead()) return false;

        HealthComponent alerted = enemic.CheckAlertRadius();
        if (alerted == null) return false;

        enemic.GetComponent<EnemicAIDebug>()?.SetActiveNode("ALERTA");

        enemic.targetHealth = alerted;
        enemic.lastSeenPosition = alerted.transform.position;
        enemic.StopGhost();
        enemic.TriggerScream(alerted);
        return true;
    }
}