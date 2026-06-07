using UnityEngine;

public class GeneradorParticulesDisparo : MonoBehaviour
{
    public Transform objectiu;

    private ParticleSystem particleSystem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        particleSystem = gameObject.GetComponentInChildren<ParticleSystem>();
        transform.LookAt(objectiu);

        particleSystem.Play();
        Destroy(gameObject, 1f);
    }

}
