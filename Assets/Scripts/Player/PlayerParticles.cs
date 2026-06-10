using TMPro;
using UnityEngine;
using UnityEngine.Rendering;


public class PlayerParticles : MonoBehaviour
{
    public static PlayerParticles Instance { get; private set; }

    [Header("Particules de llum")]
    public int numberOfParticles = 0;  
    [SerializeField] private int maxParticles = 30;
    public int numberOfRedParticles = 0; //Partícules vermelles, encara no implementades
    [SerializeField] private TutorialEntry FirstParticlesPicked; 

    private bool firstTimeGettingParticles = true;

    [Header("UI")]
    [SerializeField] private TMP_Text particlesText;



    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    // ── Consultes ─────────────────────────────────────────────────────────────

    public int Current => numberOfParticles;
    public bool HasEnough(int cost) => numberOfParticles >= cost; //Retorna sempre true o false si te suficients o no

    // ── Gastar / guanyar ──────────────────────────────────────────────────────

    public bool Spend(int cost)
    {
        if (cost <= 0) return true;
        if (numberOfParticles < cost) return false;
        numberOfParticles -= cost;
        OnParticlesChanged();
        return true;
    }

    public void Add(int amount) //No implementa encara
    {
        if (firstTimeGettingParticles)
        {
            firstTimeGettingParticles = false;
            TutorialManager.Instance?.TriggerIfNew("hasGetParticles", () =>
            {
                DroneSpeaker.Instance?.Speak(FirstParticlesPicked);
            });
        }

        numberOfParticles = Mathf.Min(numberOfParticles + amount, maxParticles);
        OnParticlesChanged();
    }


    private void OnParticlesChanged()
    {
        if (particlesText != null)
            particlesText.text = $"{numberOfParticles}/{maxParticles}";
    }
}