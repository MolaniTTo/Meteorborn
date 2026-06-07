using Unity.AI.Navigation;
using UnityEngine;

[RequireComponent(typeof(NavMeshLink))]
public class NavMeshLinkMarkers : MonoBehaviour
{
    [Header("Marker Settings")]
    [SerializeField] private float lightIntensity = 2f; 
    [SerializeField] private float lightRange = 3f;
    [SerializeField] private Color markerColor = new Color(0f, 0.8f, 1f, 1f); // cyan

    [Header("Pulse Settings")]
    [SerializeField] private float pulseSpeed = 1.5f; 
    [SerializeField] private float pulseMinIntensity = 1f;
    [SerializeField] private float pulseMaxIntensity = 3f;

    private NavMeshLink link;
    private Light startLight;
    private Light endLight;
    private bool isAligned = false;

    private void Awake()
    {
        link = GetComponent<NavMeshLink>();
        startLight = CreateMarker("Marker_Start");
        endLight = CreateMarker("Marker_End");
        UpdateMarkerPositions();
    }

    private void Update()
    {
        UpdateMarkerPositions();

        // Pulse
        float intensity = isAligned
            ? pulseMaxIntensity
            : Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);

        startLight.intensity = intensity;
        endLight.intensity = intensity;
    }

    private Light CreateMarker(string markerName)
    {
        GameObject go = new GameObject(markerName);
        go.transform.SetParent(transform);

        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = markerColor;
        l.intensity = lightIntensity;
        l.range = lightRange;

        return l;
    }

    private void UpdateMarkerPositions()
    {
        startLight.transform.position = transform.TransformPoint(link.startPoint) + Vector3.up * 0.5f;
        endLight.transform.position = transform.TransformPoint(link.endPoint) + Vector3.up * 0.5f;
    }

    // Crida des del DroneSnapDetector quan HasSnap canvia
    public void SetAligned(bool aligned)
    {
        isAligned = aligned;
        startLight.color = aligned ? Color.white : markerColor;
        endLight.color = aligned ? Color.white : markerColor;
    }
}