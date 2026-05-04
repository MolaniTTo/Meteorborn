using UnityEngine;

public class FontParticulesScript : MonoBehaviour
{
    [SerializeField] GameObject particula;

    private bool generar = false;

    private float contador = 0f;


    // Update is called once per frame
    void Update()
    {
        if (generar)
        {
            contador += Time.deltaTime;

            if (contador > 0.80f)
            {
                Vector3 tempVector = new Vector3(transform.position.x + Random.Range(-1f, 1f), transform.position.y + 10f, transform.position.z + Random.Range(-1f, 1f));
                Instantiate(particula, tempVector, Quaternion.identity);

                contador = 0f;
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player"))
        {
            generar = true;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player"))
        {
            generar = false;
        }
    }
}
