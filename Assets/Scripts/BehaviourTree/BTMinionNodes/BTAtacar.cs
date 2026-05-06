// ── 2. ATACAR ────────────────────────────────────────────────────────────────

using UnityEngine;

[CreateAssetMenu(fileName = "BTAtacar", menuName = "BehaviourTree/Minion/Atacar")]
public class BTAtacar : BTNode
{
    public override bool Execute(MinionAI minion)
    {

        if (minion.currentState != MinionAI.MinionState.Atacar) return false; //Si no esta en estat d'atacar, no fa res i retorna false per passar al següent node del selector

        if (minion.attackTarget == null) //Si no te objectiu d'atac, canvia a estat Activat, se suposa que si estava atacant s'ha mort l'enemic
        {
            minion.ChangeState(MinionAI.MinionState.Activat);
            return false;
        }

        float dist = Vector3.Distance(minion.transform.position, minion.attackTarget.position); //Calcula la distancia al objectiu d'atac

        if (dist > minion.attackRange) //si la distanciancia al objectiu d'atac es major que el rang d'atac, es mou cap a ell
        {
            minion.agent.isStopped = false;
            minion.agent.SetDestination(minion.attackTarget.position);
            if (minion.animator != null) minion.animator.SetBool("IsMoving", true);
        }
        else //si esta dins del rang d'atac, atura el minion i comença a atacar (drain d'energia i dany a l'enemic)
        {
            minion.agent.isStopped = true;
            if (minion.animator != null) minion.animator.SetBool("IsMoving", false);

            EnemicAI enemic = minion.attackTarget.GetComponent<EnemicAI>();
            if (enemic != null) enemic.RegisterAttacker(minion.transform);

            HealthComponent enemyHealth = minion.attackTarget.GetComponent<HealthComponent>();
            if (enemyHealth != null && enemyHealth.IsTargetableByMinion)
            {
                float drain = minion.energyDrainPerSecond * Time.deltaTime;
                enemyHealth.TakeDamage(drain);
                minion.energy = Mathf.Min(minion.maxEnergy, minion.energy + drain);

                Vector3 dir = minion.attackTarget.position - minion.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    minion.transform.rotation = Quaternion.Slerp(
                        minion.transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * 10f);

                if (minion.animator != null) minion.animator.SetTrigger("Attack");

                if (enemyHealth.IsDead())
                {
                    // L'enemic ha mort → Debilitat (esgotat per la lluita)
                    if (enemic != null) enemic.UnregisterAttacker(minion.transform);
                    minion.attackTarget = null;
                    minion.energy = 0f;
                    minion.ChangeState(MinionAI.MinionState.Debilitat);
                    if (minion.animator != null) minion.animator.SetTrigger("CasiMort");
                }
            }
            else
            {
                minion.attackTarget = null;
                minion.energy = 0f;
                minion.ChangeState(MinionAI.MinionState.Debilitat);

            }
        }

        return true;
    }
}