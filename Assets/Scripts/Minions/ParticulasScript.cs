using UnityEngine;

public class ParticulasScript : MonoBehaviour
{
    private Transform playerTransform;
    private Rigidbody rb;

    private float timePased = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();

        rb = gameObject.GetComponent<Rigidbody>();
    }

    void FixedUpdate() {
        timePased += Time.deltaTime;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        rb.AddForce(direction * (timePased * 0.2f), ForceMode.Force);

        if (transform.position.y <= -0)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y + 2f, transform.position.z);
        }

    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}
