// ── 6. DESACTIVAT (Fallback) ─────────────────────────────────────────────────

using UnityEngine;

[CreateAssetMenu(fileName = "BTDesactivat", menuName = "BehaviourTree/Minion/Desactivat")]
public class BTDesactivat : BTNode
{
    public override bool Execute(MinionAI minion)
    {
        if (minion.currentState != MinionAI.MinionState.Desactivat) return false;

        minion.agent.isStopped = true;

        if (minion.playerTransform != null)
        {
            float dist = Vector3.Distance(minion.transform.position, minion.playerTransform.position);
            bool curious = dist < minion.curiosityRadius;

            if (curious)
            {
                Vector3 dir = (minion.playerTransform.position - minion.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    minion.transform.rotation = Quaternion.Slerp(
                        minion.transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * 3f);
            }

            if (minion.animator != null)
                minion.animator.SetBool("IsCurious", curious);
        }

        return true;
    }
}