using UnityEngine;
using UnityEngine.InputSystem;


/*
Aquest script coloca les peçes de la nau / meteorit al lloc on toquen de manera suau.
*/
public class MeteoritColocar : MonoBehaviour
{
    [Tooltip("Pocicions objectiu")]
    [SerializeField] Transform[] posicions; //Pocicions objectiu
    [Tooltip("Objectes que s'han de posicionar")]
    [SerializeField] Transform[] objectes; //Objectes que s'han de posicionar
    [Tooltip("Objectes que han sigut posicionats")]
    [SerializeField] bool[] posicionats; //Objectes que han sigut posicionats
    private bool enMoviment = false;

    // Update is called once per frame
    void Update()
    {
        if (enMoviment)
        {
            Actualitzar();
        }
    }

    private void Actualitzar()
    {
        int trues = 0;
        int totals = 0;

        for (int i = 0; i < objectes.Length; i++)
        {
            if (posicionats[i])
            {
                totals += 1;

                if (objectes[i].position != posicions[i].position)
                {
                    objectes[i].position = Vector3.MoveTowards(objectes[i].position, posicions[i].position, 1f * Time.deltaTime);
                } else
                {
                    trues += 1;
                }

                objectes[i].rotation = posicions[i].rotation;

                Rigidbody tempRigid = objectes[i].gameObject.GetComponent<Rigidbody>();

                tempRigid.isKinematic = true;
            }
        }

        if (totals == trues)
        {
            enMoviment = false;
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

            enMoviment = true;
        }
    }
}
