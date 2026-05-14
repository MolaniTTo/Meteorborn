using UnityEngine;

// ── 2. SCREAM ─────────────────────────────────────────────────────────────────
// Prioritat 2 (just després de Curar).
// Mentre isPreScream o isScreaming siguin true, aquest node agafa el control
// complet: para el ghost, gira suaument cap al target i llança l'animació.
// Quan acaba, retorna false i cedeix al node corresponent (Atacar/Perseguir).
[CreateAssetMenu(fileName = "BTEnemicScream", menuName = "BehaviourTree/Enemic/Scream")]
public class BTEnemicScream : BTNodeEnemic
{
    public override bool Execute(EnemicAI enemic)
    {
        // Si no estem en cap fase de Scream, no fem res
        if (!enemic.isPreScream && !enemic.isScreaming) return false;

        enemic.GetComponent<EnemicAIDebug>()?.SetActiveNode("SCREAM");

        // El UpdateScream() a EnemicAI ja gestiona tota la lògica de gir i timer.
        // Aquest node simplement pren el control mentre duri.
        enemic.StopGhost();
        return true;
    }
}