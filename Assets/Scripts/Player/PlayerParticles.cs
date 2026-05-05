using UnityEngine;


public class PlayerParticles : MonoBehaviour
{
    public static PlayerParticles Instance { get; private set; }

    [Header("Particules de llum")]
    [SerializeField] private int numberOfParticles = 20;  
    [SerializeField] private int maxParticles = 200;

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
        numberOfParticles = Mathf.Min(numberOfParticles + amount, maxParticles);
        OnParticlesChanged();
    }

    private void OnParticlesChanged()
    {
        //Aqui dispararem un event que actualitzi la UI
        //UIManager.Instance?.UpdateParticleCount(numberOfParticles);
        Debug.Log($"[Partícules] {numberOfParticles}/{maxParticles}");
    }
}