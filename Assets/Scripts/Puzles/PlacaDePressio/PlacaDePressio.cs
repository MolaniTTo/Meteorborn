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
    private AudioSource audioSource;
    [SerializeField] AudioClip buttonPushSound;

    private int objectsInside = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialPosition = placaTransform.position;
        objectivePosition = placaTransform.position;

        highlightObject = gameObject.GetComponentInChildren<HighlightObject>();

        audioSource = gameObject.GetComponent<AudioSource>();
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
        if (objectsInside == 0)
        {
            objectivePosition = new Vector3(initialPosition[0], initialPosition[1] - 0.25f, initialPosition[2]);
            moving = true;
            highlightObject.Highlight(true);
            audioSource.PlayOneShot(buttonPushSound);
            
            eventoOn.Invoke();
        }
        objectsInside += 1;
    }

    private void OnTriggerExit(Collider other) {
        objectsInside -= 1;
        if (objectsInside == 0)
        {
            objectivePosition = new Vector3(initialPosition[0], initialPosition[1], initialPosition[2]);
            moving = true;
            highlightObject.Highlight(false);
            audioSource.PlayOneShot(buttonPushSound);
            
            eventoOff.Invoke();
        }
    }
}
