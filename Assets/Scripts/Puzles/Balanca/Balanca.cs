using UnityEngine;
using UnityEngine.Events;

public class Balanca : MonoBehaviour
{
    [SerializeField] PlataformaBalanca plataformaBalanca1;
    [SerializeField] PlataformaBalanca plataformaBalanca2;

    [SerializeField] Transform rotadorBalanca;

    [SerializeField] UnityEvent consequencia;
    [SerializeField] AudioClip stoneSlideSound;
    private AudioSource audioSource;

    private float rotacioObjectiu = 0f;
    private bool actualitzant = false;

    private void Start() {
        audioSource = gameObject.GetComponent<AudioSource>();
    }

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

            if (!audioSource.isPlaying)
            {
                audioSource.clip = stoneSlideSound;
                audioSource.Play();
            }

            if (Quaternion.Angle(rotadorBalanca.rotation, rotacioFinal) < 0.1f)
            {
                audioSource.Stop();

                actualitzant = false;
            }

        }
    }

    public void Actualitzar()
    {
        float pesResult = plataformaBalanca1.pess - plataformaBalanca2.pess;

        rotacioObjectiu = pesResult * 3f;

        actualitzant = true;

        if (plataformaBalanca1.pess == 10f && plataformaBalanca2.pess == 10f)
        {
            consequencia.Invoke();
        }
    }
}