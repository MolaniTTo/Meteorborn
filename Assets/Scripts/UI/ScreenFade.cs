using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ScreenFade : MonoBehaviour
{
    public Image fadeImage; //la imatge que farem servir per fer el fade
    public float fadeDuration = 0.5f; //la durada del fade
    private IEnumerator fadeRoutine;
    private void Awake()
    {
        fadeImage = GetComponent<Image>();
    }

    private void Start() {
        FadeIn();
    }

    public void FadeOut()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        fadeRoutine = FadeOutRoutine();
        StartCoroutine(fadeRoutine);
    }
    public void FadeIn()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        fadeRoutine = FadeInRoutine();
        StartCoroutine(fadeRoutine);
    }
    private IEnumerator FadeOutRoutine()
    {
        float timer = 0f;
        Color color = fadeImage.color;
        while (timer < fadeDuration) //mentre el temps sigui menor que la durada dle fade
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0, 1, timer / fadeDuration); //interpolem entre 0 i 1 del alpha(transparent)
            fadeImage.color = color;
            yield return null;
        }
    }
    private IEnumerator FadeInRoutine()
    {
        float timer = 0f;
        Color color = fadeImage.color;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(1, 0, timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
    }
}