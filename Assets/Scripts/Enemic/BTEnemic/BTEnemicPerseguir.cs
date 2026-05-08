// ── 3. PERSEGUIR ──────────────────────────────────────────────────────────────
// Target visible al con de visió o ja assignat. Mou el ghost cap al target.
// Si el perd de vista → busca (si energia > 50%) o patrulla directament.
// Si s'allunya del chaseRadius → igual.
using UnityEngine;

[CreateAssetMenu(fileName = "BTEnemicPerseguir", menuName = "BehaviourTree/Enemic/Perseguir")]
public class BTEnemicPerseguir : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        if (enemic.isScreaming)
        {
            enemic.FaceTarget(enemic.targetHealth.transform.position);
            return true;
        }

        if (enemic.targetHealth != null && !enemic.targetHealth.IsDead())
        {
            float distToGuard = Vector3.Distance(enemic.transform.position, enemic.guardPoint.position);

            if (distToGuard > enemic.chaseRadius)
            {
                if (enemic.energia > enemic.maxEnergia * 0.5f)
                    enemic.searchTimer = enemic.searchDuration;
                enemic.LoseTarget();
                return false;
            }

            if (enemic.CanSeeTarget(enemic.targetHealth.transform))
                enemic.lastSeenPosition = enemic.targetHealth.transform.position;

            enemic.MoveGhostTo(enemic.lastSeenPosition);
            return true;
        }

        HealthComponent spotted = enemic.CheckVision();
        if (spotted == null) return false;

        enemic.targetHealth = spotted;
        enemic.lastSeenPosition = spotted.transform.position;
        enemic.TriggerScream();
        enemic.MoveGhostTo(spotted.transform.position);
        return true;
    }
}