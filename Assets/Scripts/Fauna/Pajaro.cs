using UnityEngine;

public class Pajaro : MonoBehaviour
{
    public enum EstadoPajaro { Idle, Caminando, Volando, Aterrizando }

    [Header("Referencias")]
    private Animator animator;
    private CharacterController controller;

    [Header("Movimiento")]
    [SerializeField] private float velocidadCaminar = 1.5f;
    [SerializeField] private float velocidadVolar = 5f;
    [SerializeField] private float velocidadAterrizaje = 2.5f;
    [SerializeField] private float velocidadRotacion = 5f;
    [SerializeField] private float gravedad = -9.81f;

    [Header("Vuelo")]
    [SerializeField] private float alturaVueloMin = 3f;
    [SerializeField] private float alturaVueloMax = 8f;
    [SerializeField] private float radioVuelo = 15f;

    [Header("Aterrizaje")]
    [SerializeField] private float alturaCambioEstado = 0.3f; // A qué altura del suelo cambia a idle/walk
    [SerializeField] private float anguloDescenso = 30f; // Grados respecto a la horizontal

    [Header("Caminar")]
    [SerializeField] private float radioCaminar = 5f;
    [SerializeField] private float distanciaLlegada = 0.3f;

    [Header("Tiempos de Estado")]
    [SerializeField] private float tiempoIdleMin = 2f;
    [SerializeField] private float tiempoIdleMax = 5f;
    [SerializeField] private float tiempoCaminarMax = 8f;
    [SerializeField] private float tiempoVuelaMax = 12f;

    [Header("Detección de Peligro")]
    [SerializeField] private Transform jugador;
    [SerializeField] private float distanciaHuida = 4f;
    [SerializeField] private LayerMask capaSuelo = ~0;

    // Estado interno
    private EstadoPajaro estadoActual;
    private Vector3 puntoOrigen;
    private Vector3 destino;
    private Vector3 direccionAterrizaje;
    private float temporizadorEstado;
    private float duracionEstadoActual;
    private float velocidadVerticalActual;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();

        puntoOrigen = transform.position;

        if (jugador == null)
        {
            GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
            if (jugadorObj != null) jugador = jugadorObj.transform;
        }

        CambiarEstado(EstadoPajaro.Idle);
    }

    void Update()
    {
        temporizadorEstado += Time.deltaTime;

        if (DetectarPeligro() && estadoActual != EstadoPajaro.Volando)
        {
            HuirVolando();
            return;
        }

        switch (estadoActual)
        {
            case EstadoPajaro.Idle:
                ActualizarIdle();
                break;
            case EstadoPajaro.Caminando:
                ActualizarCaminar();
                break;
            case EstadoPajaro.Volando:
                ActualizarVolar();
                break;
            case EstadoPajaro.Aterrizando:
                ActualizarAterrizar();
                break;
        }

        AplicarGravedad();
    }

    // ---------- LÓGICA DE ESTADOS ----------

    private void ActualizarIdle()
    {
        if (temporizadorEstado >= duracionEstadoActual)
        {
            if (Random.value < 0.7f)
                EmpezarCaminar();
            else
                EmpezarVolar();
        }
    }

    private void ActualizarCaminar()
    {
        Vector3 direccion = destino - transform.position;
        direccion.y = 0;

        if (direccion.magnitude < distanciaLlegada || temporizadorEstado >= duracionEstadoActual)
        {
            CambiarEstado(EstadoPajaro.Idle);
            return;
        }

        Vector3 movimiento = direccion.normalized * velocidadCaminar;
        controller.Move(movimiento * Time.deltaTime);
        RotarHacia(direccion);
    }

    private void ActualizarVolar()
    {
        Vector3 direccion = destino - transform.position;

        if (direccion.magnitude < 1f || temporizadorEstado >= duracionEstadoActual)
        {
            EmpezarAterrizar();
            return;
        }

        Vector3 movimiento = direccion.normalized * velocidadVolar;
        controller.Move(movimiento * Time.deltaTime);
        RotarHacia(direccion);
        velocidadVerticalActual = 0;
    }

    private void ActualizarAterrizar()
    {
        // Comprobar la altura sobre el suelo con un raycast
        float alturaSobreSuelo = ObtenerAlturaSobreSuelo();

        // Si está muy cerca del suelo, transicionar a idle o walk
        if (alturaSobreSuelo <= alturaCambioEstado)
        {
            // Decisión natural: si lleva velocidad, sigue caminando un poco; si no, idle
            if (Random.value < 0.5f)
                EmpezarCaminar();
            else
                CambiarEstado(EstadoPajaro.Idle);
            return;
        }

        // Descenso en diagonal: avanza hacia adelante y baja a la vez
        Vector3 movimiento = direccionAterrizaje * velocidadAterrizaje;
        controller.Move(movimiento * Time.deltaTime);

        // Rotar mirando en la dirección horizontal del descenso
        Vector3 direccionHorizontal = new Vector3(direccionAterrizaje.x, 0, direccionAterrizaje.z);
        RotarHacia(direccionHorizontal);

        velocidadVerticalActual = 0;
    }

    // ---------- TRANSICIONES ----------

    private void EmpezarCaminar()
    {
        destino = ObtenerPuntoAleatorio(puntoOrigen, radioCaminar, transform.position.y);
        duracionEstadoActual = tiempoCaminarMax;
        CambiarEstado(EstadoPajaro.Caminando);
    }

    private void EmpezarVolar()
    {
        Vector3 puntoXZ = ObtenerPuntoAleatorio(puntoOrigen, radioVuelo, 0);
        float altura = puntoOrigen.y + Random.Range(alturaVueloMin, alturaVueloMax);
        destino = new Vector3(puntoXZ.x, altura, puntoXZ.z);
        duracionEstadoActual = tiempoVuelaMax;
        CambiarEstado(EstadoPajaro.Volando);
    }

    private void EmpezarAterrizar()
    {
        // Calcular dirección de descenso en diagonal
        // Combina la dirección frontal del pájaro con un componente hacia abajo
        Vector3 frente = transform.forward;
        frente.y = 0;
        frente.Normalize();

        float radianes = anguloDescenso * Mathf.Deg2Rad;
        // Componente horizontal y vertical del descenso
        direccionAterrizaje = (frente * Mathf.Cos(radianes) + Vector3.down * Mathf.Sin(radianes)).normalized;

        CambiarEstado(EstadoPajaro.Aterrizando);
    }

    private void HuirVolando()
    {
        Vector3 dirHuida = (transform.position - jugador.position).normalized;
        Vector3 puntoHuida = transform.position + dirHuida * radioVuelo;
        puntoHuida.y = puntoOrigen.y + Random.Range(alturaVueloMin, alturaVueloMax);

        destino = puntoHuida;
        duracionEstadoActual = tiempoVuelaMax;
        CambiarEstado(EstadoPajaro.Volando);
    }

    // ---------- UTILIDADES ----------

    private void CambiarEstado(EstadoPajaro nuevoEstado)
    {
        estadoActual = nuevoEstado;
        temporizadorEstado = 0f;

        // El estado Aterrizando comparte animación con Volando (fly = true)
        bool fly = (nuevoEstado == EstadoPajaro.Volando || nuevoEstado == EstadoPajaro.Aterrizando);
        bool walk = (nuevoEstado == EstadoPajaro.Caminando);

        animator.SetBool("fly", fly);
        animator.SetBool("walk", walk);

        if (nuevoEstado == EstadoPajaro.Idle)
        {
            duracionEstadoActual = Random.Range(tiempoIdleMin, tiempoIdleMax);
        }
    }

    private float ObtenerAlturaSobreSuelo()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f, capaSuelo))
        {
            return hit.distance;
        }
        return Mathf.Infinity;
    }

    private Vector3 ObtenerPuntoAleatorio(Vector3 centro, float radio, float alturaY)
    {
        Vector2 puntoCirculo = Random.insideUnitCircle * radio;
        return new Vector3(centro.x + puntoCirculo.x, alturaY, centro.z + puntoCirculo.y);
    }

    private void RotarHacia(Vector3 direccion)
    {
        if (direccion.sqrMagnitude < 0.01f) return;

        Quaternion rotObjetivo = Quaternion.LookRotation(direccion);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotObjetivo, velocidadRotacion * Time.deltaTime);
    }

    private bool DetectarPeligro()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < distanciaHuida;
    }

    private void AplicarGravedad()
    {
        // Solo aplicar gravedad cuando está en el suelo (Idle/Caminando)
        if (estadoActual == EstadoPajaro.Idle || estadoActual == EstadoPajaro.Caminando)
        {
            if (controller.isGrounded && velocidadVerticalActual < 0)
                velocidadVerticalActual = -2f;
            else
                velocidadVerticalActual += gravedad * Time.deltaTime;

            controller.Move(new Vector3(0, velocidadVerticalActual, 0) * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centro = Application.isPlaying ? puntoOrigen : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(centro, radioCaminar);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centro, radioVuelo);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaHuida);
    }
}