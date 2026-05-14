using UnityEngine;

// ── 3. ATACAR ─────────────────────────────────────────────────────────────────
// Target dins del attackRadius → para ghost, gira cap al target, ataca.
// El Scream ja ha acabat quan arribem aquí (node Scream té prioritat superior).
// AoE: el dany real es fa a OnAttackHit() (event d'animació).
// Si el target surt del radi → false, Perseguir agafa el control.
[CreateAssetMenu(fileName = "BTEnemicAtacar", menuName = "BehaviourTree/Enemic/Atacar")]
public class BTEnemicAtacar : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        if (enemic.targetHealth == null || enemic.targetHealth.IsDead())
        {
            enemic.targetHealth = null;
            enemic.SetAttackAnimation(false);
            return false;
        }

        float dist = Vector3.Distance(enemic.transform.position, enemic.targetHealth.transform.position);
        if (dist > enemic.attackRadius)
        {
            enemic.SetAttackAnimation(false);
            return false;
        }

        enemic.GetComponent<EnemicAIDebug>()?.SetActiveNode("ATACAR");

        enemic.StopGhost();
        enemic.FaceTarget(enemic.targetHealth.transform.position);
        enemic.SetAttackAnimation(true);
        return true;
    }
}