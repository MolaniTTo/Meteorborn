using UnityEngine;

// ── 5. PERSEGUIR ──────────────────────────────────────────────────────────────
// lastSeenPosition s'actualitza SEMPRE mentre tenim target viu.
// Així quan el perdem, el punt de cerca és sempre el més recent, no el primer.
//
// Transicions a Buscar:
//   · Target surt del chaseRadius
//   · Target no visible NI dins attackRadius I hem arribat a lastSeenPosition
[CreateAssetMenu(fileName = "BTEnemicPerseguir", menuName = "BehaviourTree/Enemic/Perseguir")]
public class BTEnemicPerseguir : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        if (enemic.targetHealth != null && !enemic.targetHealth.IsDead())
        {
            float distToGuard = Vector3.Distance(
                enemic.transform.position, enemic.guardPoint.position);

            // Sortit del chaseRadius → inicia Buscar
            if (distToGuard > enemic.chaseRadius)
            {
                // lastSeenPosition ja és la posició actual del target (s'actualitza cada frame)
                enemic.searchTimer = enemic.searchDuration;
                enemic.LoseTarget();
                return false;
            }

            enemic.GetComponent<EnemicAIDebug>()?.SetActiveNode("PERSEGUIR");

            // ── Actualitza lastSeenPosition SEMPRE mentre tenim target ─────────
            // Independentment de si el veiem o no: mentre el perseguim,
            // la seva posició actual és sempre "l'últim punt conegut".
            enemic.lastSeenPosition = enemic.targetHealth.transform.position;

            enemic.FaceTarget(enemic.lastSeenPosition);
            enemic.MoveGhostTo(enemic.lastSeenPosition);
            return true;
        }

        // Target mort o perdut: comprova con de visió per agafar-ne un de nou
        HealthComponent spotted = enemic.CheckVision();
        if (spotted == null) return false;

        enemic.targetHealth = spotted;
        enemic.lastSeenPosition = spotted.transform.position;
        enemic.TriggerScream(spotted);
        enemic.MoveGhostTo(spotted.transform.position);
        return true;
    }
}