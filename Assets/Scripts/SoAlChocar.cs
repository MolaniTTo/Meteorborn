using UnityEngine;

public class SoAlChocar : MonoBehaviour
{
    private AudioSource audioSource;
    [SerializeField] AudioClip audioClip;
    public float minImpactForce = 2f;

    private void Start() {
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > minImpactForce)
        {
            audioSource.PlayOneShot(audioClip);
        }
    }
}
