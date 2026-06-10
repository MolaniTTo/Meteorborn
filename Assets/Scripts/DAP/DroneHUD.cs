using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DroneHUD : MonoBehaviour
{
    public static DroneHUD Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Canvas hudCanvas;

    [Header("Reticle")]
    [SerializeField] private Image reticleCircle;

    [Header("Snap Ring")]
    [SerializeField] private CanvasGroup snapRingGroup;

    [Header("Top Left")]
    [SerializeField] private TextMeshProUGUI modeText;

    [Header("Top Center — Compass")]
    [SerializeField] private RectTransform compassContainer;   // HorizontalLayoutGroup
    [SerializeField] private TextMeshProUGUI[] compassLabels;  // 9 TMP fills, centre = index 4
    [SerializeField] private float compassLabelSpacing = 40f;

    [Header("Top Right")]
    [SerializeField] private TextMeshProUGUI recTimerText;
    [SerializeField] private Image recDot;

    [Header("Bottom Left")]
    [SerializeField] private TextMeshProUGUI velValue;
    [SerializeField] private TextMeshProUGUI pitchValue;

    [Header("Altitude Bar — Centre Dreta")]
    [SerializeField] private RectTransform altBarFill;
    [SerializeField] private TextMeshProUGUI altValue;
    [SerializeField] private float altBarMaxHeight = 80f;
    [SerializeField] private float maxAltitude = 50f;

    [Header("Bottom Right")]
    [SerializeField] private TextMeshProUGUI snapLabel;
    [SerializeField] private TextMeshProUGUI snapValue;

    [Header("Flash")]
    [SerializeField] private Image flashImage;

    [Header("Colors")]
    [SerializeField] private Color cyanColor = new Color(0f, 0.86f, 0.71f, 0.85f);
    [SerializeField] private Color cyanDim = new Color(0f, 0.86f, 0.71f, 0.45f);
    [SerializeField] private Color snapColor = new Color(0f, 1f, 0.71f, 1f);

    [Header("Tutorial Panel")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private GameObject tutorialContinuePrompt;

    // Referencies externes
    private Transform droneCameraTransform;
    private dapMovementScript droneMovement;
    private DroneSnapDetector snapDetector;

    // Estat intern
    private bool isSnapped = false;
    private float snapRingAlpha = 0f;
    private float recSeconds = 0f;
    private bool isRecording = false;
    private Vector3 _lastDronePos;

    // Brúixola
    private readonly string[] _cardinals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        hudCanvas.gameObject.SetActive(false);
        if (flashImage != null)
        {
            flashImage.color = new Color(1f, 1f, 1f, 0f);
            flashImage.gameObject.SetActive(false);
        }
    }

    public void Initialize(Transform droneCam, dapMovementScript movement, DroneSnapDetector detector)
    {
        droneCameraTransform = droneCam;
        droneMovement = movement;
        snapDetector = detector;

        if (snapDetector != null)
            snapDetector.OnSnapChanged += OnSnapChanged;
    }

    public void Show()
    {
        hudCanvas.gameObject.SetActive(true);
        SetSnapped(false);
        recSeconds = 0f;
        isRecording = true;
        _lastDronePos = droneMovement != null ? droneMovement.transform.position : Vector3.zero;
    }

    public void Hide() => HideCompletely();

    public void HideCompletely()
    {
        hudCanvas.gameObject.SetActive(false);
        isRecording = false;
        recSeconds = 0f;
        if (snapDetector != null)
            snapDetector.OnSnapChanged -= OnSnapChanged;
    }

    public void ResumeFromOrtho()
    {
        // El canvas ja està actiu, simplement resetegem l'estat visual
        SetSnapped(false);
    }

    private void OnDestroy()
    {
        if (snapDetector != null)
            snapDetector.OnSnapChanged -= OnSnapChanged;
    }

    private void Update()
    {
        if (!hudCanvas.gameObject.activeSelf) return;

        UpdateStats();
        UpdateCompass();
        UpdateSnapRing();
        UpdateRecTimer();
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    private void UpdateStats()
    {
        if (droneMovement == null || droneCameraTransform == null) return;

        // Altura
        float alt = droneMovement.transform.position.y;
        if (altValue != null) altValue.text = Mathf.RoundToInt(alt) + " m";

        if (altBarFill != null)
        {
            float pct = Mathf.Clamp01(alt / maxAltitude);
            altBarFill.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                pct * altBarMaxHeight
            );
        }

        // Velocitat
        float vel = Vector3.Distance(droneMovement.transform.position, _lastDronePos) / Time.deltaTime;
        _lastDronePos = droneMovement.transform.position;
        if (velValue != null) velValue.text = vel.ToString("F1") + " m/s";

        // Pitch
        if (pitchValue != null)
        {
            float pitch = droneCameraTransform.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            pitchValue.text = Mathf.RoundToInt(pitch) + "°";
        }
    }

    // ── Compass ───────────────────────────────────────────────────────────────

    private void UpdateCompass()
    {
        if (compassLabels == null || compassLabels.Length == 0) return;
        if (droneCameraTransform == null) return;

        // Yaw del dron en graus 0-360
        float yaw = droneMovement.transform.eulerAngles.y;

        // Cada cardinal ocupa 45 graus — calculem quin és el central
        // i el desplaçament en píxels dins del 45
        float normalizedYaw = yaw / 45f;
        int centerIndex = Mathf.RoundToInt(normalizedYaw) % 8;
        float fractional = normalizedYaw - Mathf.Round(normalizedYaw); // -0.5 a 0.5

        int half = compassLabels.Length / 2; // 4

        for (int i = 0; i < compassLabels.Length; i++)
        {
            if (compassLabels[i] == null) continue;

            int cardinalIndex = ((centerIndex + (i - half)) % 8 + 8) % 8;
            compassLabels[i].text = _cardinals[cardinalIndex];

            // Desplaçament horitzontal per simular el moviment continu
            float xOffset = (i - half - fractional) * compassLabelSpacing;
            compassLabels[i].rectTransform.anchoredPosition =
                new Vector2(xOffset, compassLabels[i].rectTransform.anchoredPosition.y);

            // El label central és més brillant
            bool isCenter = (i == half);
            compassLabels[i].color = isCenter ? cyanColor : cyanDim;
            compassLabels[i].fontSize = isCenter ? 14f : 11f;
        }
    }

    // ── Snap Ring ─────────────────────────────────────────────────────────────

    private void UpdateSnapRing()
    {
        if (snapRingGroup == null) return;
        float target = isSnapped ? 1f : 0f;
        snapRingAlpha = Mathf.Lerp(snapRingAlpha, target, Time.deltaTime * 5f);
        snapRingGroup.alpha = snapRingAlpha;
    }

    // ── REC Timer ─────────────────────────────────────────────────────────────

    private void UpdateRecTimer()
    {
        if (!isRecording) return;
        recSeconds += Time.deltaTime;

        int h = Mathf.FloorToInt(recSeconds / 3600f);
        int m = Mathf.FloorToInt((recSeconds % 3600f) / 60f);
        int s = Mathf.FloorToInt(recSeconds % 60f);

        if (recTimerText != null)
        {
            recTimerText.text = h > 0
                ? $"{h:00}:{m:00}:{s:00}"
                : $"{m:00}:{s:00}";
        }

        // Parpelleig del punt vermell
        if (recDot != null)
        {
            float alpha = Mathf.PingPong(Time.time * 1.5f, 1f);
            Color c = recDot.color;
            c.a = alpha;
            recDot.color = c;
        }
    }

    // ── Snap ──────────────────────────────────────────────────────────────────

    private void OnSnapChanged(bool hasSnap)
    {
        SetSnapped(hasSnap);
    }

    private void SetSnapped(bool snapped)
    {
        isSnapped = snapped;

        if (reticleCircle != null)
            reticleCircle.color = snapped ? snapColor : cyanColor;

        if (snapValue != null)
        {
            snapValue.text = snapped ? "ALINEADO" : "—";
            snapValue.color = snapped ? snapColor : cyanColor;
        }

        if (snapLabel != null)
            snapLabel.color = snapped ? snapColor : cyanDim;

        if (modeText != null)
            modeText.text = snapped ? "FPV · ALINEADO" : "FPV · DRON";
    }

    // ── Flash foto ────────────────────────────────────────────────────────────

    public void PlayPhotoFlash(System.Action onFlashPeak = null)
    {
        StartCoroutine(FlashRoutine(onFlashPeak));
    }

    private IEnumerator FlashRoutine(System.Action onFlashPeak)
    {
        if (flashImage == null)
        {
            onFlashPeak?.Invoke();
            yield break;
        }

        flashImage.gameObject.SetActive(true);
        flashImage.color = new Color(1f, 1f, 1f, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.08f;
            flashImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t) * 0.85f);
            yield return null;
        }

        onFlashPeak?.Invoke();

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0f, Mathf.Clamp01(t)));
            yield return null;
        }

        flashImage.gameObject.SetActive(false);
    }

    public void ShowTutorialText(string text)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (tutorialText != null) tutorialText.text = text;
        if (tutorialContinuePrompt != null) tutorialContinuePrompt.SetActive(false);
    }

    public void ShowTutorialContinuePrompt()
    {
        if (tutorialContinuePrompt != null) tutorialContinuePrompt.SetActive(true);
    }

    public void HideTutorialPanel()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (tutorialContinuePrompt != null) tutorialContinuePrompt.SetActive(false);
    }
}