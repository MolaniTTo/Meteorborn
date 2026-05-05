using UnityEngine;

public class LookAtPlayerCanva : MonoBehaviour
{
    private Transform target;

    void Start()
    {
        target = Camera.main.transform;
    }

    void FixedUpdate()
    {
        transform.LookAt(target);

        transform.Rotate(0, 180, 0);
    }
}