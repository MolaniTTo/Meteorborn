using UnityEngine;

public class LookAtPlayerCanva : MonoBehaviour
{
    private Transform transformCanva;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // transformCanva = GameObject.FindWithTag("MainCamera");
    }

    // Update is called once per frame
    void FixedUpdate() {
        // Vector3.LookAt(transformCanva);
    }
}
