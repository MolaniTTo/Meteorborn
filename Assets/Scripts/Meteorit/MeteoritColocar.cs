using UnityEngine;
using UnityEngine.InputSystem;

public class MeteoritColocar : MonoBehaviour
{
    [SerializeField] Transform[] posicions;
    [SerializeField] Transform[] objectes;
    [SerializeField] bool[] posicionats;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void Actualitzar()
    {
        for (int i = 0; i < objectes.Length; i++)
        {
            if (posicionats[i])
            {
                objectes[i].position = posicions[i].position;
                objectes[i].rotation = posicions[i].rotation;

                Rigidbody tempRigid = objectes[i].gameObject.GetComponent<Rigidbody>();

                tempRigid.isKinematic = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("meteorPart"))
        {
            for (int i = 0; i < objectes.Length; i++)
            {
                if (objectes[i] == other.transform)
                {
                    posicionats[i] = true;
                    break;
                }
            }

            Actualitzar();
        }
    }
}
