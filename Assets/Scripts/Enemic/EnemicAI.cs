using UnityEngine;

public class AnimatorEventsConnector : MonoBehaviour
{
    private EnemicAI enemy;
    

    private void Start()
    {
        enemy = GetComponentInParent<EnemicAI>();
    }

    public void OnAttackHitEvent()
    {
        if (enemy != null)
        {
            enemy.OnAttackHit();
        }
    }
}


