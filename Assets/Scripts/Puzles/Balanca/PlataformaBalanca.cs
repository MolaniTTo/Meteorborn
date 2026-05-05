using UnityEngine;

public class PlataformaBalanca : MonoBehaviour
{
    public float pess;
    [SerializeField] Transform plataformaTransform;
    [SerializeField] Balanca balanca;

    public Vector3 objectivePosition;
    public Vector3 initialPosition;

    private float miniCounter;

    private void Start() {
        objectivePosition = plataformaTransform.position;
        initialPosition = objectivePosition;
    }

    private void FixedUpdate() {
        if (objectivePosition != plataformaTransform.position)
        {
            plataformaTransform.position = Vector3.MoveTowards(plataformaTransform.position, objectivePosition, 0.01f);
        }

        if (miniCounter >= 2f)
        {
            if (pess == 0)
            {
                objectivePosition = initialPosition;
            } else
            {
                objectivePosition[1] = initialPosition[1] - pess * 0.2f;
            }

            miniCounter = 0f;
        }

        miniCounter += Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("pesa"))
        {
            Rigidbody temporalRb = other.GetComponent<Rigidbody>();
            pess += temporalRb.mass;

            objectivePosition = new Vector3(plataformaTransform.position.x, initialPosition[1] - temporalRb.mass * 0.2f, plataformaTransform.position.z);

            balanca.Actualitzar();
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("pesa"))
        {
            Rigidbody temporalRb = other.GetComponent<Rigidbody>();

            objectivePosition = new Vector3(plataformaTransform.position.x, initialPosition[1] + temporalRb.mass * 0.2f, plataformaTransform.position.z);

            pess -= temporalRb.mass;

            balanca.Actualitzar();
        }
    }
}
