using UnityEngine;

public class ParticulasScript : MonoBehaviour
{
    private Transform playerTransform;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();

        rb = gameObject.GetComponent<Rigidbody>();
    }

    void FixedUpdate() {

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        rb.AddForce(direction * 2f, ForceMode.Force);

    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}
