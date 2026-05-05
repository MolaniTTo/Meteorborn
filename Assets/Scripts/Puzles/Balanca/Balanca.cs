using UnityEngine;
using UnityEngine.Events;

public class Balanca : MonoBehaviour
{
    [SerializeField] PlataformaBalanca plataformaBalanca1;
    [SerializeField] PlataformaBalanca plataformaBalanca2;

    [SerializeField] Transform rotadorBalanca;

    [SerializeField] UnityEvent consequencia;

    private float rotacioObjectiu = 0f;
    private bool actualitzant = false;

    void Update()
    {
        if (actualitzant)
        {
            Quaternion rotacioFinal = Quaternion.Euler(0f, 0f, rotacioObjectiu);

            rotadorBalanca.rotation = Quaternion.RotateTowards(
                rotadorBalanca.rotation,
                rotacioFinal,
                3f * Time.deltaTime
            );

            if (Quaternion.Angle(rotadorBalanca.rotation, rotacioFinal) < 0.1f)
            {
                actualitzant = false;
            }
        }
    }

    public void Actualitzar()
    {
        float pesResult = plataformaBalanca1.pess - plataformaBalanca2.pess;

        rotacioObjectiu = pesResult * 3f;

        actualitzant = true;

        if (plataformaBalanca1.pess != 0f && plataformaBalanca1.pess == plataformaBalanca2.pess)
        {
            consequencia.Invoke();
        }
    }
}