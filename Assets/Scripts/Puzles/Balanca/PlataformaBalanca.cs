using UnityEngine;

public class PlataformaBalanca : MonoBehaviour
{
    public float pess;
    [SerializeField] Transform plataformaTransform;

    public Vector3 objectivePosition;

    private void Start() {
        objectivePosition = plataformaTransform.position;
    }

    private void FixedUpdate() {
        if (objectivePosition != plataformaTransform.position)
        {
            plataformaTransform.position = Vector3.MoveTowards(plataformaTransform.position, objectivePosition, 0.01f);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("pesa"))
        {
            Rigidbody temporalRb = other.GetComponent<Rigidbody>();
            pess += temporalRb.mass;

            objectivePosition = new Vector3(plataformaTransform.position.x, plataformaTransform.position.y + -pess / 4, plataformaTransform.position.z);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("pesa"))
        {
            Rigidbody temporalRb = other.GetComponent<Rigidbody>();

            objectivePosition = new Vector3(plataformaTransform.position.x, plataformaTransform.position.y + pess / 4, plataformaTransform.position.z);

            pess -= temporalRb.mass;

        }
    }
}
