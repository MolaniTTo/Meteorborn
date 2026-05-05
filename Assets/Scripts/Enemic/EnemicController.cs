using UnityEngine;
using UnityEngine.AI;

public class EnemicController : MonoBehaviour
{
    [SerializeField] Transform guardPoint;
    [SerializeField] private float speed = 0f;
    [SerializeField] private float targetSpeed = 3.5f;
    [SerializeField] float guardDistance = 6f;

    public float energia = 100f;
    public GameObject perseguint;
    public GameObject minionDintreRadi;

    public Vector3 nextPoint;

    public NavMeshAgent agent;

    [SerializeField] NavMeshAgent fantasmaAgent;

    private Animator animator;


    void Start()
    {
        Invoke("GenerarPoint", 10f);
        agent = GetComponent<NavMeshAgent>();
        GenerarPoint();

        animator = gameObject.GetComponentInChildren<Animator>();

        agent.speed = targetSpeed;
        fantasmaAgent.speed = targetSpeed;
    }

    void Update()
    {
        //Pasar parametres al Animator
        speed = Mathf.Abs(agent.velocity[0] + agent.velocity[1] + agent.velocity[2]);
        animator.SetFloat("speed", speed);
        animator.SetBool("attack", false);

        //Arbre de comportament
        if ( Vector3.Distance(transform.position, guardPoint.position) > guardDistance) //Esta lluny de la font?
        {
            MoveToCenter();

        }
        else
        {
            if (energia < 30f) //La energia es mes petita que 30%?
            {
                MoveToCenter();
                
                if (Vector3.Distance(transform.position, guardPoint.position) < 2f)
                {
                    
                    energia += Time.deltaTime * 2f;
                }
            }
            else
            {
                if (perseguint != null) //Perseguint?
                {
                    Debug.Log("Perseguint");
                    MoveToMinion();
                    if (Vector3.Distance(perseguint.transform.position, transform.position) < 1.5f)
                    {
                        if (MoreThan2Minions())
                        {
                            Debug.Log("Reduir vida grup de minions");
                        }
                        else
                        {
                            Debug.Log("Reduir vida minion");
                            animator.SetBool("attack", true);
                        }
                    }
                }
                else
                {
                    if (minionDintreRadi != null) //Hi ha un minion dintre del meu radi? 
                    {
                        //Esta el minion asignat a un altre enemic?
                        perseguint = minionDintreRadi;
                        minionDintreRadi = null;
                    }
                    else
                    {
                        //Wonder
                        
                        MoveToPoint();
                    }
                }
            }
        }
    }

    void MoveToMinion()
    {
        fantasmaAgent.SetDestination(perseguint.GetComponent<Transform>().position);
        agent.SetDestination(fantasmaAgent.transform.position);
    }

    void MoveToPoint()
    {
        //transform.position = Vector3.MoveTowards(transform.position, nextPoint, speed * Time.deltaTime);
        fantasmaAgent.SetDestination(nextPoint);
        agent.SetDestination(fantasmaAgent.transform.position);

        if (Vector3.Distance(transform.position, nextPoint) < 4f)
        {
            GenerarPoint();
        }
    }

    void MoveToCenter()
    {
        fantasmaAgent.SetDestination(guardPoint.position);
        agent.SetDestination(fantasmaAgent.transform.position);

        perseguint = null;
    }

    void GenerarPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * guardDistance;

        Vector3 randomPoint = new Vector3(
            guardPoint.position.x + randomCircle.x,
            guardPoint.position.y,
            guardPoint.position.z + randomCircle.y
        );

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, guardDistance, NavMesh.AllAreas))
        {
            nextPoint = hit.position;
        }
    }

    bool MoreThan2Minions() {
        Collider[] objetos = Physics.OverlapSphere(transform.position, 3);

        int contador = 0;

        foreach (Collider col in objetos)
        {
            if (col.CompareTag("minion"))
            {
                contador++;
            }
        }

        if (contador > 1) {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void DealDamage(float tempFlot)
    {
        energia -= tempFlot;
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("minion"))
        {
            minionDintreRadi = other.gameObject;
        }
    }
}