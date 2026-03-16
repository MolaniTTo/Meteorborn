using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class PlayerStateMachine : MonoBehaviour
{
    //Vistes del jugador
    public enum PlayerViewMode      
    {
        ThirdPerson,
        OrthographicView,
    }

    //Estats del player
    public enum PlayerState     
    {
        Idle,
        Walking,
        OrthoIdle,
        OrthoMoving,
    }

    //Input System Actions
    private InputSystem_Actions inputActions;           //Input System Actions reference (la classe de C# generada a partir de l'Input System Actions asset)
    private InputAction moveAction;                     //Input Action reference (la acció de moure del Input System Actions asset)
    private InputAction lookAction;                     //Input Action reference (la acció de mirar del Input System Actions asset)
    private InputAction confirmAction;                  //Input Action reference (la acció de confirmar del Input System Actions asset)


    //Variables per als inputs del player
    private Vector2 moveInput;                          //Vector3 per emmagatzemar la direcció del moviment del player (x, y, z)
    private Vector2 lookInput;


    //Variables per a la gestió de l'estat del player
    private PlayerViewMode playerViewMode;
    private PlayerState currentState;

    //Components
    [Header("Components")]
    [SerializeField] private NavMeshAgent agent;        //Referència al component NavMeshAgent del player (assignat des de l'inspector)

    //Camera
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform; //Aqui posem la main camera del player (assignada des de l'inspector)

    //Moviment
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.5f;    //Velocitat de moviment del player (assignada des de l'inspector)    
    [SerializeField] private float rotationSpeed = 10f; //velocitat del Slerp amb que el player gira cap a la direcció del moviment

    //Ortho
    [Header("Ortho Settings")]
    [SerializeField] private OrthoCursor orthoCursor;       // Referencia al cursor 3D de la escena
    [SerializeField] private float linkSpeed = 8f;          // Velocitat al travessar un OffMeshLink
    [SerializeField] private float arrivalDistance = 0.3f;  // Distancia per considerar que el player ha arribat al desti

    //NavMesh Link Traversal
    private bool traversingLink = false;



    private void Awake()
    {
        inputActions = new InputSystem_Actions();       //Inicialitzem la classe de C# generada a partir de l'Input System Actions asset
        moveAction = inputActions.Player.Move;          //Obtenim la referència a l'acció de moure del Input System Actions asset
        lookAction = inputActions.Player.Look;          //Obtenim la referència a l'acció de mirar del Input System Actions asset
        confirmAction = inputActions.Player.Confirm;    //Obtenim la referència a l'acció de confirmar del Input System Actions asset

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;                   //Deshabilitem que el NavMeshAgent actualitzi la rotació del player, ja que nosaltres gestionarem la rotació manualment al nostre script de moviment
        agent.speed = moveSpeed;  
        agent.autoTraverseOffMeshLink = false;          //Deshabilitem que el NavMeshAgent travessi automàticament els OffMeshLinks, ja que nosaltres gestionarem el travessament manualment al nostre script de moviment
        agent.acceleration = 99999f;
        agent.angularSpeed = 99999f;
    }


    private void OnEnable()
    {
        inputActions.Player.Enable();
        confirmAction.performed += OnConfirmPerformed;
    }

    private void OnDisable()
    {
        confirmAction.performed -= OnConfirmPerformed;
        inputActions.Player.Disable();
    }

    private void OnConfirmPerformed(CallbackContext ctx)
    {
        //Nomes actua si estem en mode ortografic i no estem ja travessant un link
        if (playerViewMode != PlayerViewMode.OrthographicView) return;
        if (traversingLink) return;

        switch (orthoCursor.CurrentMode)
        {
            case OrthoCursor.CursorMode.NavMesh:
                //Mou el player al punt del cursor
                agent.SetDestination(orthoCursor.WorldPosition);
                currentState = PlayerState.OrthoMoving;
                break;

            case OrthoCursor.CursorMode.Free:
                // Comprova si hi ha un objecte interactuable a la posicio del cursor
                TryInteract();
                break;
        }
    }

    private void TryInteract()
    {
        //Raycast des de la posicio del cursor cap avall per detectar objectes interactuables
        Ray ray = new Ray(orthoCursor.WorldPosition + Vector3.up * 5f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            if (hit.collider.CompareTag("Interactable"))
            {
                Debug.Log($"Interactuant amb: {hit.collider.gameObject.name}");
                //Aqui cridarem al component Interactuable quan el tinguem implementat
            }
            else
            {
                Debug.Log("No hi ha cap objecte interactuable aqui");
            }
        }
    }

    private void Start()
    {
        playerViewMode = PlayerViewMode.ThirdPerson;    //Inicialitzem la vista del player a Third Person
        currentState = PlayerState.Idle;                //Inicialitzem l'estat del player a Idle

        if(orthoCursor != null)
        {
            orthoCursor.SetActive(false);               //Assegurem que el cursor 3D està desactivat al iniciar el joc, ja que comencem en mode Third Person
        }
    }

    public void SetViewMode(PlayerViewMode mode)    // Cridat des del CameraSwitcher quan canvia de camara
    {
        playerViewMode = mode;

        if (mode == PlayerViewMode.OrthographicView)    //Si la vista es canvia a mode ortografic, canviem l'estat del player a OrthoIdle i activem el cursor 3D
        {
            currentState = PlayerState.OrthoIdle;
            agent.ResetPath();
            agent.speed = moveSpeed;

            if (orthoCursor != null)
            {
                //Posicionem el cursor a sobre del player al activar el mode ortografic
                orthoCursor.transform.position = transform.position;
                orthoCursor.SetActive(true);
            }
        }
        else    //Si la vista es canvia a mode Third Person, canviem l'estat del player a Idle i desactivem el cursor 3D
        {
            currentState = PlayerState.Idle;

            if (orthoCursor != null)
                orthoCursor.SetActive(false);
        }
    }

    void Update()
    {
        ProcessInputActions();
        
        switch (currentState)
        {
            case PlayerState.Idle:
                HandleIdle();                           //Lògica per a l'estat Idle (per exemple, animació d'estar parat, etc.)
                break;

            case PlayerState.Walking:
                HandleWalking();                        //Lògica per a l'estat Walking (per exemple, animació de caminar, moviment del player, etc.)
                break;

            case PlayerState.OrthoIdle:
                HandleOrthoIdle();                       //Podem reutilitzar la mateixa lògica d'Idle per a OrthoIdle, ja que el player està parat en ambdós casos
                break;

            case PlayerState.OrthoMoving:
                HandleOrthoMoving();
                break;
        }

        if (agent.isOnOffMeshLink && !agent.pathPending && !traversingLink) { StartCoroutine(TraverseLink()); } //Si el player està a sobre d'un OffMeshLink, no està calculant un nou camí, i no està ja travessant un link, iniciem la coroutine per a travessar el link
    }

    //------------------------ INPUT ------------------------//

    private void ProcessInputActions()
    {
        moveInput = moveAction.ReadValue<Vector2>();    // x = horitzontal, y = vertical del joistick  
        
        if(playerViewMode == PlayerViewMode.ThirdPerson)
        {
            currentState = moveInput.sqrMagnitude > 0.01f ? PlayerState.Walking : PlayerState.Idle;   //Si hi ha input de moviment, canviem l'estat del player a Walking, sino a Idle (operador ternari per a simplificar el codi)
        }
        if (playerViewMode == PlayerViewMode.OrthographicView && orthoCursor != null)   //Si esta en ortografic actualitza la posicio del cursor 3D segons el input de moviment, per a que el jugador pugui moure el cursor amb el joistick abans de confirmar la posició amb el botó de confirmació i començar a moure's cap a la posició del cursor
        {
            orthoCursor.SetMoveInput(moveInput);
        }
    }

    //------------------------ STATES ------------------------//

    private void HandleIdle()
    {
        agent.SetDestination(transform.position);       //Passem la posicio actual com a desti
    }

    private void HandleWalking()
    {
        //Convertim el input 2D del joistick en una direccio 3D relativa a la càmera, per a que el moviment del player sigui en la direcció en què està mirant la càmera

        Vector3 camForward = cameraTransform.forward;   //Obtenim la direcció forward de la càmera (que és la direcció en què està mirant la càmera)
        Vector3 camRight = cameraTransform.right;       //Obtenim la direcció right de la càmera (que és la direcció perpendicular a la dirección forward de la càmera, cap a la dreta)

        camForward.y = 0f;                              //Anulem la componente vertical de la direccio forward de la càmera, per a que el moviment del player sigui nomes en el pla horizontal
        camRight.y = 0f;                                //Anulem la componente vertical de la direccio right de la càmera, per a que el movimient del player sigui nomes en el pla horizontal
        
        camForward.Normalize();                         //Normalitzem la direcció forward de la càmera, per a que tingui una magnitud de 1 i només representi la direcció
        camRight.Normalize();                           //Normalitzem la direcció right de la càmera, per a que tingui una magnitud de 1 i només representi la direcció

        //Direccio de moviment en espai mon, relatiu a la camara
        Vector3 moveDirection = (camForward * moveInput.y + camRight * moveInput.x);   

        //Moure al agent cal a la direccio calculada
        Vector3 targetPosition = transform.position + moveDirection * moveSpeed;
        agent.SetDestination(targetPosition); 

        if(moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);   //Calculem la rotació que ha de tenir el player per a mirar cap a la direcció de moviment
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);   //Girem el player cap a la direcció de moviment amb un Slerp per a que el gir sigui suau
        }
    }

    private void HandleOrthoIdle()
    {
        agent.SetDestination(transform.position);   //Passem la posicio actual com a desti
    }

    private void HandleOrthoMoving()
    {
        //Gira el player cap al desti mentre es mou
        Vector3 directionToDestination = (agent.destination - transform.position);
        directionToDestination.y = 0f;

        if (directionToDestination.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToDestination);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        //Comprovem si el player ha arribat al desti
        if (!agent.pathPending && agent.remainingDistance <= arrivalDistance)
        {
            currentState = PlayerState.OrthoIdle;
            agent.ResetPath();
        }
    }


    //------------------------ OFFMESHLINK TRAVERSAL ------------------------//

    private IEnumerator TraverseLink()
    {
        traversingLink = true;

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 finalDestination = agent.destination;

        float linkDistance = Vector3.Distance(data.startPos, data.endPos);
        float traverseTime = linkDistance / linkSpeed;
        float speed = linkDistance / traverseTime;

        agent.speed = speed;
        agent.autoTraverseOffMeshLink = true;

        while (agent.isOnOffMeshLink)
            yield return null;

        agent.velocity = Vector3.zero;
        agent.speed = moveSpeed;
        agent.autoTraverseOffMeshLink = false;
        agent.SetDestination(finalDestination);

        traversingLink = false;
    }


    //------------------------ CALLBACKS DE INPUTS ------------------------//

    private void OnMove(CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    private void OnLook(CallbackContext context) => lookInput = context.ReadValue<Vector2>();

}
