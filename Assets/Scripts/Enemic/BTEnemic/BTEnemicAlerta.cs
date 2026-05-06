// ── 4. ALERTA ─────────────────────────────────────────────────────────────────
// Target molt a prop (radi d'alerta, 360°) sense necessitat de visió.
// Es gira cap a ell i el marca com a target perquè el node Atacar o Perseguir
// el puguin agafar en el proper frame.
using UnityEngine;

[CreateAssetMenu(fileName = "BTEnemicAlerta", menuName = "BehaviourTree/Enemic/Alerta")]
public class BTEnemicAlerta : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        if (enemic.isScreaming)
        {
            enemic.FaceTarget(enemic.targetHealth.transform.position);
            return true; // sigue en este nodo pero sin moverse
        }

        HealthComponent alerted = enemic.CheckAlertRadius();
        if (alerted == null) return false;

        // Assigna com a target i para el ghost
        enemic.targetHealth = alerted;
        enemic.StopGhost();
        enemic.TriggerScream();
        enemic.FaceTarget(alerted.transform.position);

        return true;
    }
}