using System.Collections;
using UnityEngine;

public class MinionVisualController : MonoBehaviour
{
    [SerializeField] private Material mat;
    [SerializeField] private float minEmission = -10f;
    [SerializeField] private float maxEmission = 3f;
    [SerializeField] private Light minionLight;
    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private float minIntensity = 0f;
    [SerializeField] private float transitionDuration = 0.5f;

    private Color baseEmissionColor = Color.white;
    private Coroutine lightCoroutine;
    private Coroutine blinkCoroutine;

    [SerializeField] private Material originalMat; // referència a l'asset original

    void Start()
    {
        if (mat != null)
        {
            originalMat = mat;
            mat = new Material(mat);
            GetComponentInChildren<Renderer>().material = mat;
            mat.EnableKeyword("_EMISSION");

            // Ignora el color actual del material, usa sempre blanc com a base
            baseEmissionColor = Color.white;

            // Força el valor inicial explícitament
            SetEmission(minEmission);
        }

        if (minionLight != null) minionLight.intensity = minIntensity;

    }

    void OnDestroy()
    {
        // Restaura l'asset original per evitar acumulació entre plays
        if (originalMat != null)
        {
            originalMat.EnableKeyword("_EMISSION");
            originalMat.SetColor("_EmissionColor", Color.white * Mathf.Pow(2f, minEmission));
        }
    }

    private void SetEmission(float value)
    {
        if (mat != null)
            mat.SetColor("_EmissionColor", baseEmissionColor * Mathf.Pow(2f, value));
    }

    public void MaxLightAndEmission()
    {
        StopAllVisualCoroutines();
        float fromEmission = mat != null ? GetCurrentEmissionValue() : minEmission;
        float fromLight = minionLight != null ? minionLight.intensity : minIntensity;
        lightCoroutine = StartCoroutine(LerpVisuals(fromEmission, maxEmission, fromLight, maxIntensity, transitionDuration));
    }

    public void MinLightAndEmission()
    {
        StopAllVisualCoroutines();
        float fromEmission = mat != null ? GetCurrentEmissionValue() : maxEmission;
        float fromLight = minionLight != null ? minionLight.intensity : maxIntensity;
        lightCoroutine = StartCoroutine(LerpVisuals(fromEmission, minEmission, fromLight, minIntensity, transitionDuration));
    }

    public void StartBlink(float totalDuration)
    {
        StopAllVisualCoroutines();
        blinkCoroutine = StartCoroutine(BlinkCoroutine(totalDuration));
    }

    public void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }

    private IEnumerator BlinkCoroutine(float totalDuration)
    {
        float elapsed = 0f;
        float blinkSpeed = 1.5f;

        while (elapsed < totalDuration)
        {
            float lifeRatio = 1f - (elapsed / totalDuration);
            float currentMaxEmission = Mathf.Lerp(minEmission, maxEmission, lifeRatio);
            float currentMaxLight = Mathf.Lerp(minIntensity, maxIntensity, lifeRatio);

            float t = (Mathf.Sin(elapsed * Mathf.PI * 2f / blinkSpeed) + 1f) / 2f;

            SetEmission(Mathf.Lerp(minEmission, currentMaxEmission, t));
            if (minionLight != null)
                minionLight.intensity = Mathf.Lerp(minIntensity, currentMaxLight, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetEmission(minEmission);
        if (minionLight != null) minionLight.intensity = minIntensity;
    }

    private IEnumerator LerpVisuals(float fromEmission, float toEmission,
                                     float fromLight, float toLight, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetEmission(Mathf.Lerp(fromEmission, toEmission, t));
            if (minionLight != null)
                minionLight.intensity = Mathf.Lerp(fromLight, toLight, t);
            yield return null;
        }
        SetEmission(toEmission);
        if (minionLight != null) minionLight.intensity = toLight;
    }

    // Converteix el color HDR actual de tornada a valor float per poder fer Lerp
    private float GetCurrentEmissionValue()
    {
        Color current = mat.GetColor("_EmissionColor");
        float intensity = current.maxColorComponent / baseEmissionColor.maxColorComponent;
        return Mathf.Log(Mathf.Max(intensity, Mathf.Epsilon), 2f);
    }

    private void StopAllVisualCoroutines()
    {
        if (lightCoroutine != null) { StopCoroutine(lightCoroutine); lightCoroutine = null; }
        if (blinkCoroutine != null) { StopCoroutine(blinkCoroutine); blinkCoroutine = null; }
    }
}