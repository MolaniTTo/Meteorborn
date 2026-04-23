using UnityEngine;

public class ParticulasScript : MonoBehaviour
{
    private Transform playerTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
    }

    void FixedUpdate() {
        transform.position = Vector3.MoveTowards(playerTransform.position, transform.position, 0.001f);
    }
}
