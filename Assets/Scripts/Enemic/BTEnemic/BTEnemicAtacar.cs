// ── 2. ATACAR ─────────────────────────────────────────────────────────────────
// Target dins del radi d'atac. Para el ghost, es gira cap al target i ataca.
// Si el target surt del radi d'atac però segueix visible → persegueix.
// Si el target surt del radi de persecució → busca o patrulla.
using UnityEngine;

[CreateAssetMenu(fileName = "BTEnemicAtacar", menuName = "BehaviourTree/Enemic/Atacar")]
public class BTEnemicAtacar : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        if (enemic.targetHealth == null || enemic.targetHealth.IsDead())
        {
            enemic.targetHealth = null;
            return false;
        }

        float dist = Vector3.Distance(enemic.transform.position, enemic.targetHealth.transform.position);
        if (dist > enemic.attackRadius) return false;

        // Para el ghost i es gira cap al target
        enemic.StopGhost();
        enemic.FaceTarget(enemic.targetHealth.transform.position);
        enemic.SetAttackAnimation(true);

        //enemic.targetHealth.TakeDamage(enemic.damagedEnergiaPerSecond * Time.deltaTime);

        return true;
    }
}