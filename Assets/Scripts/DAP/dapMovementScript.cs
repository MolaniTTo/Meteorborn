using UnityEngine;

public class dapMovementScript : MonoBehaviour
{
    [SerializeField] Transform helice1;
    [SerializeField] Transform helice2;
    [SerializeField] float speedMultiplier;
    [SerializeField] float followSpeed = 5f;
    private Transform playerTrans;

    [SerializeField] private Transform tutorialPosition; // posició fixa davant el player

    private bool isFrozenAtTutorialPos = false;
    public bool isControlledByPlayer = false;

    void Start()
    {
        playerTrans = GameObject.FindWithTag("DroneFollow").transform;
    }

    void FixedUpdate()
    {
        helice1.Rotate(0f, 1f * speedMultiplier, 0f);
        helice2.Rotate(0f, -1f * speedMultiplier, 0f);
    }

    void Update()
    {
        if (isControlledByPlayer) return;
        if (isFrozenAtTutorialPos) return;

        float resultSpeed = followSpeed * (Vector3.Distance(playerTrans.position, transform.position) * 0.70f);
        transform.position = Vector3.MoveTowards(transform.position, playerTrans.position, resultSpeed * Time.deltaTime);
        transform.LookAt(playerTrans);
    }

    public void FreezeAtTutorialPosition()
    {
        isFrozenAtTutorialPos = true;
        if (tutorialPosition != null)
            transform.SetPositionAndRotation(tutorialPosition.position, tutorialPosition.rotation);
    }

    public void UnfreezeFromTutorialPosition()
    {
        isFrozenAtTutorialPos = false;
    }
}