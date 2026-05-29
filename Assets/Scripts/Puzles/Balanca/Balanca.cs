using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Balanca : MonoBehaviour
{
    [Header("TPs")]
    [SerializeField] private Transform tp1;
    [SerializeField] private Transform tp2;

    [Header("TP Visual Indicators (MeshRenderer)")]
    [SerializeField] private ParticleSystem tp1Indicator;
    [SerializeField] private ParticleSystem tp2Indicator;

    [Header("Pesos")]
    [SerializeField] private GameObject[] totsPesos;

    [Header("Plataformas")]
    [SerializeField] private PlataformaBalanca plataformaBalanca1;
    [SerializeField] private PlataformaBalanca plataformaBalanca2;

    [Header("Rotació")]
    [SerializeField] private Transform rotadorBalanca;

    [Header("Events")]
    [SerializeField] private UnityEvent consequencia;

    [Header("Audio")]
    [SerializeField] private AudioClip stoneSlideSound;

    [Header("Camara")]
    [SerializeField] private Transform posicioCamara;
    private Transform camaraPlayer;

    public bool activada = false;

    
    // BALANÇA
    
    private float rotacioObjectiu = 0f;
    private bool actualitzant = false;

    
    // SELECCIÓN
    
    private int pesSeleccionat = 0;
    private int tpSeleccionat = -1; // ❗ empieza sin TP

    private Highlightable[] highlights;

    private void Start()
    {
        highlights = new Highlightable[totsPesos.Length];

        for (int i = 0; i < totsPesos.Length; i++)
        {
            highlights[i] = totsPesos[i].GetComponent<Highlightable>();
        }

        ActualitzarHighlights();
        ActualitzarTPIndicators();

        camaraPlayer = GameObject.FindWithTag("MainCamera").GetComponent<Transform>();
    }

    private void Update()
    {
        if (activada) 
        {
            GestionarInputs();
        }
        ActualitzarRotacio();
    }

    //Posicionar la camara segons si el puzzle esta activat o no

    private void LateUpdate() {
        if (activada)
        {
            camaraPlayer.position = posicioCamara.position;
            camaraPlayer.rotation = posicioCamara.rotation;
        }
    }

    
    // INPUTS
    
    private void GestionarInputs()
    {
        // PESO

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            pesSeleccionat--;

            if (pesSeleccionat < 0)
                pesSeleccionat = totsPesos.Length - 1;

            ActualitzarHighlights();
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            pesSeleccionat++;

            if (pesSeleccionat >= totsPesos.Length)
                pesSeleccionat = 0;

            ActualitzarHighlights();
        }

        
        // TP SELECCIÓN
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            tpSeleccionat = 0;
            ActualitzarTPIndicators();
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            tpSeleccionat = 1;
            ActualitzarTPIndicators();
        }

        // COLOCAR

        if ((Keyboard.current.eKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame)
             && tpSeleccionat != -1)
        {
            ColocarPes();
        }
    }

    
    // TP INDICATORS
    
    private void ActualitzarTPIndicators()
    {
        if (tpSeleccionat == 0)
        {
            tp1Indicator.Play();
            tp2Indicator.Stop();
        }
        else if (tpSeleccionat == 1)
        {
            tp1Indicator.Stop();
            tp2Indicator.Play();
        }
    }

    
    // COLOCAR
    
    private void ColocarPes()
    {
        Transform tpActual = tpSeleccionat == 0 ? tp1 : tp2;
        GameObject pes = totsPesos[pesSeleccionat];

        pes.transform.position = tpActual.position;

        Actualitzar();
    }

    
    // HIGHLIGHT PESOS
    
    private void ActualitzarHighlights()
    {
        for (int i = 0; i < highlights.Length; i++)
        {
            highlights[i].UnHighlight();
        }

        highlights[pesSeleccionat].Highlight();
    }

    
    // BALANÇA
    
    private void ActualitzarRotacio()
    {
        if (!actualitzant) return;

        Quaternion rotacioFinal = Quaternion.Euler(0f, 0f, rotacioObjectiu);

        rotadorBalanca.localRotation = Quaternion.RotateTowards(
            rotadorBalanca.localRotation,
            rotacioFinal,
            3f * Time.deltaTime
        );

        

        if (Quaternion.Angle(rotadorBalanca.rotation, rotacioFinal) < 0.1f)
        {
            actualitzant = false;
        }
    }

    
    public void Actualitzar()
    {
        float pesResult = plataformaBalanca1.pess - plataformaBalanca2.pess;

        rotacioObjectiu = pesResult * 3f;
        actualitzant = true;

        if (plataformaBalanca1.pess == 10f &&
            plataformaBalanca2.pess == 10f)
        {
            consequencia.Invoke();
            Destroy(this);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            activada = true;
        }
    }
}