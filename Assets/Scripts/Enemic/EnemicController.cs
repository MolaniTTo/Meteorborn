using UnityEngine;

public class EnemicController : MonoBehaviour
{
    [SerializeField] Transform guardPoint;
    [SerializeField] float speed = 2f;

    private Transform nextPoint;

    void Start()
    {
        GenerarPoint();
    }

    void Update()
    {
        MoveToPoint();
    }

    void MoveToPoint()
    {
        transform.position = Vector3.MoveTowards(transform.position, nextPoint.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, nextPoint.position) < 0.1f)
        {
            GenerarPoint();
        }
    }

    void GenerarPoint()
    {
        Vector3 tempTrans = new Vector3(guardPoint.position.x + Random.Range(-5f, 5f), 0f, guardPoint.position.z + Random.Range(-5f, 5f));
        nextPoint.position = tempTrans;
    }
}