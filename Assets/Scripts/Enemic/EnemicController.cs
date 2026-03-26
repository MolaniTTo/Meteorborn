using UnityEngine;
using UnityEngine.AI;

public class EnemicController : MonoBehaviour
{
    [SerializeField] Transform guardPoint;
    [SerializeField] float speed = 2f;
    [SerializeField] float guardDistance = 6f;

    public float energia = 100f;
    private GameObject perseguint;

    private Vector3 nextPoint;

    private NavMeshAgent agent;
    

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        GenerarPoint();
    }

    void Update()
    {
        //Arbre de comportament
        if ( Vector3.Distance(transform.position, guardPoint.position) > guardDistance) //Esta lluny de la font?
        {
            
        }
        else
        {
            if (energia < 30f) //La energia es mes petita que 30%?
            {
                MoveToCenter();
            }
            else
            {
                if (perseguint != null) //Perseguint?
                {
                    
                }
                else
                {
                    MoveToPoint();
                }
            }
        }


        
    }

    void MoveToPoint()
    {
        //transform.position = Vector3.MoveTowards(transform.position, nextPoint, speed * Time.deltaTime);
        agent.SetDestination(nextPoint);

        if (Vector3.Distance(transform.position, nextPoint) < 0.1f)
        {
            GenerarPoint();
        }
    }

    void MoveToCenter()
    {
        agent.SetDestination(guardPoint.position);
    }

    void GenerarPoint()
    {
        Vector3 tempTrans = new Vector3(guardPoint.position.x + Random.Range(-guardDistance, guardDistance), 0f, guardPoint.position.z + Random.Range(-guardDistance, guardDistance));
        nextPoint = tempTrans;
    }
}