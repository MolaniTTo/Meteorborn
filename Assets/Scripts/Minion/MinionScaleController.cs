using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class MinionScaleController : MonoBehaviour
{
    [SerializeField] private float defaultScale = 0.1f; // Default scale factor
    [SerializeField] private float scaleDuration = 1.0f; // Duration of the scaling effect
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 0.15f;

    private HealthComponent health; // Reference to the HealthComponent script
    private Coroutine scaleCoroutine;
    private Coroutine breathCoroutine;
    private float breathMaxScale; //El maxim assolir a cada respiracio
    public bool IsAtMinScale => Mathf.Approximately(transform.localScale.x, minScale);

    void Start()
    {
        health = GetComponent<HealthComponent>();

        if(health != null) { health.OnDamageTaken += _ => UpdateScale(health.HealthPercent); }
    }

    private void OnDestroy()
    {
        if (health != null) { health.OnDamageTaken -= _ => UpdateScale(health.HealthPercent); }
    }


    public void UpdateScale(float healthpercent) //cambiar la escala según el porcentaje de vida, con un mínimo de minScale y un máximo de originalScale
    {
        Debug.Log($"[MinionScaleController] Actualizando escala. HealthPercent={healthpercent}");
        float targetScaleFactor = Mathf.Lerp(minScale, maxScale, healthpercent); //Interpola entre minScale y maxScale según el porcentaje de vida
        SmoothScale(Vector3.one * targetScaleFactor); //Llama a la función para escalar suavemente hacia la escala objetivo
    }

    public void SetMaxScale() //Llamado desde funciones directas como activar minion
    {
        SmoothScale(Vector3.one * maxScale);
    }

    public void SetMinScale() //Llamado desde funciones directas como activar minion
    {
        SmoothScale(Vector3.one * minScale);
    }

    private void SmoothScale(Vector3 targetScale)
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleCoroutine(targetScale));
    }

    private IEnumerator ScaleCoroutine(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < scaleDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / scaleDuration); //Interpola entre la escala inicial y la escala objetivo según el tiempo transcurrido
            yield return null; 
        }

        transform.localScale = targetScale; //Asegura que la escala final sea exactamente la escala objetivo al finalizar la interpolación
    }

    public void StartNearDeathBreath(float duration) //Cridat quan el minion entra en CasiMort
    {
        StopBreath();
        breathMaxScale = maxScale;
        breathCoroutine = StartCoroutine(NearDeathBreathCoroutine(duration));
    }

    public void StopBreath()
    {
        if (breathCoroutine != null)
        {
            StopCoroutine(breathCoroutine);
            breathCoroutine = null;
        }
    }

    private IEnumerator NearDeathBreathCoroutine(float totalDuration)
    {
        float elapsed = 0f;
        float breathSpeed = 1.5f; //segons per respiració completa

        while (elapsed < totalDuration)
        {
            // El màxim de cada respiració decreix proporcionalment al temps restant
            float lifeRatio = 1f - (elapsed / totalDuration);
            float currentMax = Mathf.Lerp(minScale, maxScale, lifeRatio);

            // Sine wave: va de minScale a currentMax i torna
            float t = (Mathf.Sin(elapsed * Mathf.PI * 2f / breathSpeed) + 1f) / 2f; // 0..1
            float targetScale = Mathf.Lerp(minScale, currentMax, t);
            transform.localScale = Vector3.one * targetScale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.one * minScale;
    }




}
