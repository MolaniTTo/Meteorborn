using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class DapMissions : MonoBehaviour
{
    [SerializeField] Canvas canvasEspacial;
    [SerializeField] ParticleSystem particle;
    [SerializeField] TextMeshProUGUI textoCanvas;
    [SerializeField] private int textEnMarxa = -1;
    [SerializeField] float textSpeed = 0.1f;

    [SerializeField] private string[] textoAMostrar;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvasEspacial.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (textEnMarxa != -1 && Keyboard.current.vKey.wasPressedThisFrame)
        {
            NextText();
        }
    }

    private void NextText()
    {
        textEnMarxa += 1;

        if (textEnMarxa >= textoAMostrar.Length)
        {
            textEnMarxa = -1;
            canvasEspacial.enabled = false;
            particle.Stop();

        } else
        {
            StartCoroutine(EscriureText());
        }
    }

    public void ShowText(string[] temptext)
    {
        textEnMarxa = 0;
        textoAMostrar = temptext;

        canvasEspacial.enabled = true;
        particle.Play();

        StartCoroutine(EscriureText());
    }

    IEnumerator EscriureText()
    {
        textoCanvas.text = "";

        if (textEnMarxa == -1)
        {
            yield return null;
        }

        for (int i = 0; i < textoAMostrar[textEnMarxa].Length; i++)
        {
            textoCanvas.text += textoAMostrar[textEnMarxa][i];

            yield return new WaitForSeconds(textSpeed);
        }
        
        yield return null;
    }
}
