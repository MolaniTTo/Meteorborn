using UnityEngine;

public class FinalCohetScript : MonoBehaviour
{
    [Header("Camara")]
    private Transform transCamara;
    private Camera camara;

    [Header("Cohet")]
    private Rigidbody rigidbody;
    private float force = 1f;

    void Start()
    {
        transCamara = GameObject.FindWithTag("MainCamera").GetComponent<Transform>();
        camara = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

        rigidbody = gameObject.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        rigidbody.AddForce(transform.up * 7f * force);
        rigidbody.AddTorque(transform.forward * 0.015f);
        rigidbody.AddTorque(transform.right * 0.015f);

        force += 0.01f;
    }

    private void LateUpdate()
    {
        transCamara.LookAt(transform.position);
    }
}