// ── 5. BUSCAR ─────────────────────────────────────────────────────────────────
// Ha perdut el target de vista i té energia > 50%. Va a l'última posició
// vista i busca durant searchDuration segons. Si el troba, persegueix.
// Si s'esgota el temps, torna a patrullar.
using UnityEngine;

[CreateAssetMenu(fileName = "BTEnemicBuscar", menuName = "BehaviourTree/Enemic/Buscar")]
public class BTEnemicBuscar : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        if (enemic.searchTimer <= 0f) return false;

        enemic.searchTimer -= Time.deltaTime;
        enemic.MoveGhostTo(enemic.lastSeenPosition);

        // Comprova si el troba durant la cerca
        HealthComponent spotted = enemic.CheckVision();
        if (spotted != null)
        {
            enemic.targetHealth = spotted;
            enemic.searchTimer = 0f;
            return false; // deixa que Perseguir ho agafi al proper frame
        }

        HealthComponent alerted = enemic.CheckAlertRadius();
        if (alerted != null)
        {
            enemic.targetHealth = alerted;
            enemic.searchTimer = 0f;
            return false;
        }

        return true;
    }
}