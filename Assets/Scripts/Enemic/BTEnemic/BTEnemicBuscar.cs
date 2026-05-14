using UnityEngine;

// ── 5. BUSCAR ─────────────────────────────────────────────────────────────────
// Fase 1 - Caminar: va cap a lastSeenPosition.
//   Si veu/detecta algú → cedeix a Perseguir (sense Scream).
//   Quan arriba (o timer = 0) → inicia LookAround.
// Fase 2 - LookAround: animació de mirar al voltant.
//   Si veu/detecta algú → interromp, cedeix a Perseguir.
//   Quan acaba → ResumePatrolFromNearestPoint i cedeix a Patrullar.
[CreateAssetMenu(fileName = "BTEnemicBuscar", menuName = "BehaviourTree/Enemic/Buscar")]
public class BTEnemicBuscar : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        enemic.GetComponent<EnemicAIDebug>()?.SetActiveNode("BUSCAR");

        // Ni en cerca ni en LookAround → cedeix
        if (enemic.searchTimer <= 0f && !enemic.isLookingAround) return false;

        // ── Fase 2: LookAround ────────────────────────────────────────────────
        if (enemic.isLookingAround)
        {
            enemic.lookAroundTimer -= Time.deltaTime;

            // Comprova si veu algú durant el LookAround
            HealthComponent spotted = enemic.CheckVision() ?? enemic.CheckAlertRadius();
            if (spotted != null)
            {
                enemic.StopLookAround();
                enemic.searchTimer = 0f;
                enemic.targetHealth = spotted;
                enemic.lastSeenPosition = spotted.transform.position;
                // NO fem Scream aquí: ja estava buscant
                enemic.MoveGhostTo(spotted.transform.position);
                return false; // cedeix a Perseguir
            }

            if (enemic.lookAroundTimer <= 0f)
            {
                // Acabat sense trobar ningú → reprèn patrulla
                enemic.StopLookAround();
                enemic.searchTimer = 0f;
                enemic.ResumePatrolFromNearestPoint();
                return false;
            }

            enemic.StopGhost();
            return true;
        }

        // ── Fase 1: Caminar cap a lastSeenPosition ────────────────────────────
        enemic.searchTimer -= Time.deltaTime;

        // Comprova si veu algú mentre camina
        HealthComponent spottedWalking = enemic.CheckVision() ?? enemic.CheckAlertRadius();
        if (spottedWalking != null)
        {
            enemic.searchTimer = 0f;
            enemic.targetHealth = spottedWalking;
            enemic.lastSeenPosition = spottedWalking.transform.position;
            enemic.MoveGhostTo(spottedWalking.transform.position);
            return false; // cedeix a Perseguir
        }

        enemic.MoveGhostTo(enemic.lastSeenPosition);

        // Ha arribat al punt?
        bool arrived = !enemic.ghostAgent.pathPending &&
                       enemic.ghostAgent.remainingDistance < 0.8f;

        if (arrived || enemic.searchTimer <= 0f)
        {
            enemic.searchTimer = 0f;
            enemic.StartLookAround();
        }

        return true;
    }
}