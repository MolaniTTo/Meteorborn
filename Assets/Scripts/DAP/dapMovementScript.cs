using UnityEngine;

public class dapMovementScript : MonoBehaviour
{

    [SerializeField] Transform helice1;
    [SerializeField] Transform helice2;

    [SerializeField] float speedMultiplier;
    [SerializeField] float followSpeed = 5f;
    // [SerializeField] float sprintSpeed = 10f;

    private Transform playerTrans;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerTrans = GameObject.FindWithTag("PlayerFollowPoint").transform;
    }

    // Update is called once per frame
    void FixedUpdate() {
        helice1.Rotate(0f, 1f * speedMultiplier, 0f);
        helice2.Rotate(0f, -1f * speedMultiplier, 0f);
    }

    void Update() {
        float resultSpeed = followSpeed * (Vector3.Distance(playerTrans.position, transform.position) * 0.70f);

        transform.position = Vector3.MoveTowards(transform.position, playerTrans.position, resultSpeed * Time.deltaTime);
        
        transform.LookAt(playerTrans);
    }
}
