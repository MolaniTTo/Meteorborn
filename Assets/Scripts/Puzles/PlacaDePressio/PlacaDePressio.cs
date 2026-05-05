using UnityEngine;
using UnityEngine.Events;

public class PlacaDePressio : MonoBehaviour
{
    public UnityEvent eventoOn;
    public UnityEvent eventoOff;
    private Vector3 objectivePosition;
    private Vector3 initialPosition;
    private HighlightObject highlightObject;
    private bool moving = false;
    [SerializeField] Transform placaTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialPosition = placaTransform.position;
        objectivePosition = placaTransform.position;

        highlightObject = gameObject.GetComponentInChildren<HighlightObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (moving)
        {
            placaTransform.position = Vector3.MoveTowards(placaTransform.position, objectivePosition, Time.deltaTime * (Vector3.Distance(placaTransform.position, objectivePosition) * 3f));

            highlightObject.intensity = Vector3.Distance(placaTransform.position, initialPosition) * 600;

            if (placaTransform.position == objectivePosition)
            {
                moving = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        objectivePosition = new Vector3(initialPosition[0], initialPosition[1] - 0.25f, initialPosition[2]);
        moving = true;
        highlightObject.Highlight(true);
        eventoOn.Invoke();
    }

    private void OnTriggerExit(Collider other) {
        objectivePosition = new Vector3(initialPosition[0], initialPosition[1], initialPosition[2]);
        moving = true;
        highlightObject.Highlight(false);
        eventoOff.Invoke();
    }
}
