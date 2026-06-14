using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class FinalCohetScript : MonoBehaviour
{
    [Header("Camara")]
    private Transform transCamara;
    private Camera camara;
    [SerializeField] private SpriteRenderer fadeBlock;
    [SerializeField] private GameObject canvasGame;

    [Header("Cohet")]
    private Rigidbody rigidbody;
    private float force = 1.4f;

    [Header("Cambio escena")]
    [SerializeField] private string escenaDestino;
    [SerializeField] private float fadeSpeed = 1f;

    private bool cambiandoEscena = false;


    void Start()
    {
        transCamara = GameObject.FindWithTag("MainCamera").GetComponent<Transform>();
        camara = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

        rigidbody = gameObject.GetComponent<Rigidbody>();

        // Color c = fadeBlock.color;
        // c.a = 0;
        // fadeBlock.color = c;
        StartCoroutine("FadeIn");
    }


    void FixedUpdate()
    {
        rigidbody.AddForce(transform.up * 7f * force);
        rigidbody.AddTorque(transform.forward * 0.015f);
        rigidbody.AddTorque(transform.right * 0.015f);

        force += 0.01f;
    }


    private void LateUpdate()
    {
        transCamara.LookAt(transform.position);
    }


    public void CambiarEscena()
    {
        if (!cambiandoEscena)
        {
            StartCoroutine(FadeOut());
        }
    }


    IEnumerator FadeOut()
    {
        cambiandoEscena = true;

        Color c = fadeBlock.color;

        while (c.a < 1)
        {
            c.a += fadeSpeed * Time.deltaTime;
            fadeBlock.color = c;

            yield return null;
        }

        // Ya está completamente negro
        SceneManager.LoadScene(escenaDestino);
    }


    IEnumerator FadeIn()
    {
        Color c = fadeBlock.color;

        while (c.a > 0)
        {
            c.a -= fadeSpeed * Time.deltaTime;
            fadeBlock.color = c;

            yield return null;
        }

        yield return new WaitForSeconds(14f);

        canvasGame.SetActive(true);

        yield return new WaitForSeconds(4f);

        CambiarEscena();
    }
}