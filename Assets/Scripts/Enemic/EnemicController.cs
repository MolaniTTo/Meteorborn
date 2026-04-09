using UnityEngine;
using UnityEngine.AI;

public class EnemicController : MonoBehaviour
{
    [SerializeField] Transform guardPoint;
    [SerializeField] private float speed = 0f;
    [SerializeField] float guardDistance = 6f;

    public float energia = 100f;
    public GameObject perseguint;
    public GameObject minionDintreRadi;

    public Vector3 nextPoint;

    public NavMeshAgent agent;

    private Animator animator;


    void Start()
    {
        Invoke("GenerarPoint", 10f);
        agent = GetComponent<NavMeshAgent>();
        GenerarPoint();

        animator = gameObject.GetComponentInChildren<Animator>();
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
            Debug.Log("Lluny de la font");
        }
        else
        {
            if (energia < 30f) //La energia es mes petita que 30%?
            {
                MoveToCenter();
                Debug.Log("Energia baixa");
                if (Vector3.Distance(transform.position, guardPoint.position) < 2f)
                {
                    Debug.Log("Curant");
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
                        Debug.Log("Viatjant al punt");
                        MoveToPoint();
                    }
                }
            }
        }
    }

    void MoveToMinion()
    {
        agent.SetDestination(perseguint.GetComponent<Transform>().position);
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

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("minion"))
        {
            minionDintreRadi = other.gameObject;
        }
    }
}