using System.Collections;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraSwitcher : MonoBehaviour
{
    //Player
    [Header("Player")]
    [SerializeField] private PlayerStateMachine playerStateMachine;
    [SerializeField] private OrthoCursor orthoCursor; 

    //Cinemachine
    [Header("Cinemachine")]
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineCamera perspectiveCam;
    [SerializeField] private CinemachineCamera orthoCam;

    //Main camera
    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera playerOverlayCamera; //Overlay del player en el mode orthographic

    //Transition settings
    [Header("Transition Settings")]
    [SerializeField] private float fovTransitionSpeed = 3f; //Velocitat a la que es fa la transició del FOV quan canvia de càmera, per suavitzar el canvi i evitar canvis bruscos que puguin molestar al jugador
    [SerializeField] private float maxFOV = 115f;           //FOV maxim que s'aplica a la camara en perspectiva per fer la transicio mes dramatica i que no sembli un canvi brusc
    [SerializeField] private float blendDuration = 1.5f;    //Duració del blend entre càmeres, per suavitzar el canvi i evitar canvis bruscos que puguin molestar al jugador
    [SerializeField] private float perspectiveFOV = 60f;    //FOV default de la camara en perspectiva, per tornar a aquest valor quan es torna a la camara en perspectiva després d'haver passat por una zona de camara ortografica
    [SerializeField] private float orthoMoveSpeed = 2f;     //Velocitat del desplaçament entre posicions ortho
    
    //Drone
    [Header("Drone")]
    [SerializeField] private CinemachineCamera droneCam;
    [SerializeField] private dapMovementScript droneMovement;
    [SerializeField] private GameObject droneCameraPosition;
    [SerializeField] private float droneMoveSpeed = 10f;
    [SerializeField] private float droneRotateSpeed = 80f;
    [SerializeField] private float droneMinHeight = 1f;
    [SerializeField] private float droneMaxHeight = 50f;
    private float dronePitch = 0f;
    [Header("Drone FX")]
    [SerializeField] private Volume droneVolume;
    [Header("Drone Snap & Preview")]
    [SerializeField] private DroneSnapDetector snapDetector;
    [SerializeField] private float previewOrthoSize = 10f;      // ortho size de la preview
    [SerializeField] private float orthoFlattenZOffset = 50f; // ajusta segons el teu món
    private bool _droneBlendStarted = false;
    private Vector3 _dronePositionBeforeFlatten;
    [SerializeField] private float orthoSizeDistanceMultiplier = 0.15f; 
    [SerializeField] private float orthoSizeMin = 5f;
    [SerializeField] private float orthoSizeMax = 30f;
    [SerializeField] private float droneFlattenZOffset = 55f;
    [SerializeField] private GameObject droneGameObject;

    [Header("HUD")]
    [SerializeField] private DroneHUD droneHUD;

    //Input
    private InputSystem_Actions inputActions;

    //States
    private enum TransitionState 
    { 
        Idle, 
        ExpandingFOV, 
        BlendingPosition, 
        MovingOrthoCamera,
        ShrinkingFOV,
        EnteringDrone,
        DroneActive,
        ExitingDrone,
        ReturningToDrone
    }

    private TransitionState transitionState = TransitionState.Idle;
    [SerializeField] private bool isOrthoMode = false;           //Default esta en perspectiva tercera persona
    public static bool IsOrthoMode { get; private set; } //propiuetat estatica per que els altres scripts puguin saber si estem en ortho o no.


    //Zona de la camara
    private CameraZone currentZone;     //Zona activa actual (null si no hi ha cap)
    private int currentOrthoIndex = 0;          //Index de la posicio ortografica actual


    //Properties
    private bool CanSwitchToOrtho => currentZone != null && currentZone.ZoneData.allowOrthoMode;     //Propietat que indica si es pot canviar a mode ortografic, depenent de si la zona actual ho permet o no (si no hi ha zona, no es pot canviar a ortho)
    private bool HasMultipleOrthoPositions => currentZone != null                               //Per saber si hi ha mes de una posicio ortografica disponible en a zona actual
                                           && currentZone.OrthoPositions != null
                                           && currentZone.OrthoPositions.Length > 1;


    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.DroneFlatten.performed += OnDroneFlattenPerformed;
        inputActions.Player.DroneExit.performed += OnDroneExitPerformed;
        inputActions.Player.CycleCameraPosition.performed += OnCycleCameraPositionPerformed;
        if (snapDetector != null)
            snapDetector.OnSnapChanged += OnSnapChanged;
    }

    private void OnDisable()
    {
        inputActions.Player.DroneFlatten.performed -= OnDroneFlattenPerformed;
        inputActions.Player.DroneExit.performed -= OnDroneExitPerformed;
        inputActions.Player.CycleCameraPosition.performed -= OnCycleCameraPositionPerformed;
        if (snapDetector != null)
            snapDetector.OnSnapChanged -= OnSnapChanged;

        inputActions.Player.Disable();
    }

    private void Start()
    {
        if (playerOverlayCamera != null)    //Si hi ha camara overlay del player, la desactivem al iniciar el joc ja que default estem en perspectiva tercera persona
        {
            playerOverlayCamera.gameObject.SetActive(false); 
        }
    }

    private void Update()
    {
        HandleTransition();
    }

    private void OnDroneFlattenPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (!playerStateMachine.canUseDrone) return;

        bool speakerBlocks = DroneSpeaker.Instance != null
            && DroneSpeaker.Instance.IsSpeaking
            && (TutorialInicial.Instance == null || !playerStateMachine.canUseDrone);
        if (speakerBlocks) return;

        switch (transitionState)
        {
            case TransitionState.Idle:
                if (!isOrthoMode)
                {
                    if (!playerStateMachine.canUseDrone) return; 
                    StartSwitchToDrone();
                }
                break;

            case TransitionState.DroneActive:
                if (!playerStateMachine.canUseOrtho) return;
                if (droneHUD != null)
                    droneHUD.PlayPhotoFlash(() => StartSwitchToOrthoFromDrone());
                else
                    StartSwitchToOrthoFromDrone();
                break;
        }
    }

    private void OnDroneExitPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        switch (transitionState)
        {
            case TransitionState.DroneActive:
                if (!playerStateMachine.canExitDrone) return;
                StartExitDrone();
                HandleDroneMovement();
                break;

            case TransitionState.Idle:
                if (isOrthoMode)
                {
                    if (!playerStateMachine.canExitDrone) return;
                    StartReturnToDrone();
                }
                break;
        }
    }

    private void OnCycleCameraPositionPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => OnCycleCameraPosition();

    private void OnSnapChanged(bool hasSnap)
    {

    }

    private void StartSwitchToDrone()
    {
        droneCam.transform.SetPositionAndRotation(
            droneCameraPosition.transform.position,
            droneCameraPosition.transform.rotation
        );
        float rawPitch = droneCameraPosition.transform.localEulerAngles.x;
        dronePitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;

        brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut,
            blendDuration
        );

        droneCam.Priority = 20;
        perspectiveCam.Priority = 10;

        // Activem el detector
        if (snapDetector != null)
            snapDetector.enabled = true;

        if (droneHUD != null)
        {
            droneHUD.Initialize(droneCameraPosition.transform, droneMovement, snapDetector);
        }

        droneMovement.isControlledByPlayer = true;

        transitionState = TransitionState.EnteringDrone;
    }
    private void StartExitDrone()
    {
        if (droneHUD != null) droneHUD.HideCompletely();

        if (snapDetector != null)
            snapDetector.enabled = false;

        brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut,
            blendDuration
        );

        perspectiveCam.Priority = 20;
        droneCam.Priority = 10;

        droneMovement.isControlledByPlayer = false;
        playerStateMachine.SetViewMode(PlayerStateMachine.PlayerViewMode.ThirdPerson);

        transitionState = TransitionState.ExitingDrone;
    }

    private void StartSwitchToOrthoFromDrone()
    {
        if (!playerStateMachine.canUseOrtho) return;
        //if (droneHUD != null) droneHUD.Hide();

        // Determinem la rotació final — snappejada si hi ha snap, la del dron si no
        Quaternion finalRotation = snapDetector != null && snapDetector.HasSnap
            ? snapDetector.GetSnappedRotation()
            : droneCameraPosition.transform.rotation;

        Vector3 flattenPos = droneCameraPosition.transform.position
                   - finalRotation * Vector3.forward * orthoFlattenZOffset;

        Vector3 droneFlattenPos = droneCameraPosition.transform.position
           - finalRotation * Vector3.forward * droneFlattenZOffset;

        _dronePositionBeforeFlatten = droneCameraPosition.transform.position;
        droneMovement.transform.position = droneFlattenPos;
        if (droneGameObject != null) droneGameObject.SetActive(false);

        orthoCam.transform.SetPositionAndRotation(flattenPos, finalRotation);

        var orthoLens = orthoCam.Lens;
        orthoLens.OrthographicSize = CalculateOrthoSizeFromDrone();
        orthoCam.Lens = orthoLens;
        orthoCam.Lens = orthoLens;

        mainCamera.orthographic = true;

        brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut,
            blendDuration
        );

        orthoCam.Priority = 20;
        droneCam.Priority = 10;

        playerStateMachine.SetViewMode(PlayerStateMachine.PlayerViewMode.OrthographicView);
        isOrthoMode = true;
        IsOrthoMode = true;

        if (orthoCursor != null)
            orthoCursor.RecalculateScreenPosition();


        transitionState = TransitionState.BlendingPosition;
    }

    private float CalculateOrthoSizeFromDrone()
    {
        if (snapDetector == null || snapDetector.BestSnap == null)
            return currentZone != null ? currentZone.ZoneData.orthographicSize : 10f;

        NavMeshLink link = snapDetector.BestSnap.navMeshLink;
        Vector3 linkStart = link.transform.TransformPoint(link.startPoint);
        Vector3 linkEnd = link.transform.TransformPoint(link.endPoint);
        Vector3 linkMid = (linkStart + linkEnd) * 0.5f;

        float distToLink = Vector3.Distance(droneCameraPosition.transform.position, linkMid);
        float size = distToLink * orthoSizeDistanceMultiplier;

        return Mathf.Clamp(size, orthoSizeMin, orthoSizeMax);
    }

    private void StartReturnToDrone()
    {
        if (droneHUD != null) droneHUD.ResumeFromOrtho();

        droneMovement.transform.position = _dronePositionBeforeFlatten;
        droneCam.transform.SetPositionAndRotation(
            droneCameraPosition.transform.position,
            droneCameraPosition.transform.rotation
        );

        // Sortim d'ortogràfic i tornem al dron on estava
        if (playerOverlayCamera != null)
            playerOverlayCamera.gameObject.SetActive(false);

        mainCamera.orthographic = false;

        brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut,
            blendDuration
        );

        droneCam.Priority = 20;
        orthoCam.Priority = 10;

        playerStateMachine.SetViewMode(PlayerStateMachine.PlayerViewMode.DroneView);
        isOrthoMode = false;
        IsOrthoMode = false;

        transitionState = TransitionState.ReturningToDrone;
    }

    private void HandleDroneMovement()
    {
        Vector2 move = playerStateMachine.canMove ? inputActions.Player.DroneMove.ReadValue<Vector2>() : Vector2.zero;
        Vector2 look = playerStateMachine.canMove ? inputActions.Player.DroneLook.ReadValue<Vector2>() : Vector2.zero;

        // Rotem el dron sencer (no la cam)
        float yaw = droneMovement.transform.eulerAngles.y + look.x * droneRotateSpeed * Time.deltaTime;
        dronePitch -= look.y * droneRotateSpeed * Time.deltaTime;
        dronePitch = Mathf.Clamp(dronePitch, -80f, 80f);
        droneMovement.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        droneCameraPosition.transform.localRotation = Quaternion.Euler(dronePitch, 0f, 0f);

        // Moviment basat en la direcció de la càmera
        Transform camT = droneCameraPosition.transform;
        Vector3 moveDir = camT.forward * move.y + camT.right * move.x;

        // ── Col·lisió per eixos separats ──────────────────────────────────────
        Vector3 currentPos = droneMovement.transform.position;
        float sphereRadius = 0.1f;

        Vector3 horizontalMove = new Vector3(moveDir.x, 0f, moveDir.z) * droneMoveSpeed * Time.deltaTime;
        if (horizontalMove.sqrMagnitude > 0.001f)
            if (!Physics.SphereCast(currentPos, sphereRadius, horizontalMove.normalized, out _, horizontalMove.magnitude))
                currentPos += horizontalMove;

        Vector3 verticalMove = new Vector3(0f, moveDir.y, 0f) * droneMoveSpeed * Time.deltaTime;
        if (verticalMove.sqrMagnitude > 0.001f)
            if (!Physics.SphereCast(currentPos, sphereRadius, verticalMove.normalized, out _, verticalMove.magnitude))
                currentPos += verticalMove;

        currentPos.y = Mathf.Clamp(currentPos.y, droneMinHeight, droneMaxHeight);
        droneMovement.transform.position = currentPos;
        // ─────────────────────────────────────────────────────────────────────

        droneCam.transform.SetPositionAndRotation(
            droneCameraPosition.transform.position,
            droneCameraPosition.transform.rotation
        );
    }

    //MANAGE ZONES

    public void EnterZone(CameraZone zone)  //Es crida des del CameraZone quan el player entra en una zona de camara ortho
    {
        currentZone = zone;
        currentOrthoIndex = 0;                      //Reseteja el index en aquesta nova zona

        //Si estem en ortho, hem mogut al player desde ortho a un lloc, i la nova zona no permet ortho, retorna a perspectiva 3a persona
        if (isOrthoMode && !CanSwitchToOrtho)
        {
            StartSwitchToPerspective();
        }
    }

    public void ExitZone(CameraZone zone)
    {
        //Nomes netejem a null si sortim de la zona que estava activa, per evitar que si tenim zones superposades i sortim d'una de les zones, es netegin les dades encara que estiguem en una altra zona que si permet ortho. D'aquesta manera, només quan sortim de la zona activa actual, es netegen les dades i es torna a perspectiva si estàvem en ortho. Si sortim d'una zona que no és l'actual, no fem res perquè encara estem dins de la zona activa i les dades segueixen sent vàlides. Això permet tenir zones superposades amb diferents configuracions de càmera sense que es interfereixin entre elles quan el jugador entra o surt de les zones.
        if (currentZone != zone) { return; }

        currentZone = null;

        //Si estavem en ortho i sortim de la zona que permetia ortho, tornem a perspectiva 3a persona
        if (isOrthoMode)
        {
            StartSwitchToPerspective();
        }
    }


    //INPUT CALLBACKS

    private void OnCycleCameraPosition()
    {
        if (!isOrthoMode) { return; }                             //Nomes permet ciclar posicions si estem en mode ortho
        if (!HasMultipleOrthoPositions) { return; }               //Nomes si hi ha mes de una posicio
        if (transitionState != TransitionState.Idle) { return; }  //Ignora si hi ha una transicio en proces

        //Va al seguent index de posicio ortografica i aplica la nova posicio
        currentOrthoIndex = (currentOrthoIndex + 1) % currentZone.OrthoPositions.Length;
        StartCoroutine(MoveOrthoCamera(currentOrthoIndex));
    }

    private IEnumerator MoveOrthoCamera(int index)
    {
        if (currentZone.OrthoPositions == null || currentZone.OrthoPositions.Length == 0) { yield break; }

        transitionState = TransitionState.MovingOrthoCamera;  // Bloquejem altres transicions mentre es mou

        Transform targetPos = currentZone.OrthoPositions[index];
        Vector3 startPos = orthoCam.transform.position;
        Quaternion startRot = orthoCam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * orthoMoveSpeed;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);  //SmoothStep perque el moviment sigui mes suau

            orthoCam.transform.position = Vector3.Lerp(startPos, targetPos.position, smoothT);
            orthoCam.transform.rotation = Quaternion.Slerp(startRot, targetPos.rotation, smoothT);

            yield return null;
        }

        //Assegurem la posicio exacta al final
        orthoCam.transform.SetPositionAndRotation(targetPos.position, targetPos.rotation);
        if (orthoCursor != null)
        {
            orthoCursor.RecalculateScreenPosition();
        }
        transitionState = TransitionState.Idle;
        GameManager.instance.EvaluateConditions(); //Reavaluem condicions al final del moviment, per si alguna condicio depenia de la posicio de la camara ortografica
    }

    //SWITCH LOGIC

    private void StartSwitchToOrtho()
    {
        if (LockOnSystem.Instance != null && LockOnSystem.Instance.IsLockedOn) { LockOnSystem.Instance.LoseLockOn(); }

        //Configura a la camara ortografica les dades de la zona
        var orthoLens = orthoCam.Lens;
        orthoLens.OrthographicSize = currentZone.ZoneData.orthographicSize;
        orthoCam.Lens = orthoLens;

        //Coloca la camara ortho a posicio inicial (index = 0) de la zona
        ApplyOrthoPosition(currentOrthoIndex);

        transitionState = TransitionState.ExpandingFOV;
    }

    private void StartSwitchToPerspective()
    {
        if (playerOverlayCamera != null) { playerOverlayCamera.gameObject.SetActive(false); }

        //Fa el blend entre càmeres, configurant el blend per que sigui mes dramatic i no sembli un canvi brusco, i torna al FOV default de la camara en perspectiva
        brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut,
            blendDuration
        );

        mainCamera.orthographic = false;

        var lens = perspectiveCam.Lens;
        lens.FieldOfView = maxFOV;
        perspectiveCam.Lens = lens;

        perspectiveCam.Priority = 20;
        orthoCam.Priority = 10;

        transitionState = TransitionState.BlendingPosition;
        playerStateMachine.SetViewMode(PlayerStateMachine.PlayerViewMode.ThirdPerson); //Notifiquem al PST que estem en perspectiva 3a persona
        isOrthoMode = false;
        IsOrthoMode = false;
    }

    private void ApplyOrthoPosition(int index)
    {
        if (currentZone.OrthoPositions == null || currentZone.OrthoPositions.Length == 0) { return; } //Si no hi ha posicions ortho definides, no fa res

        Transform targetPos = currentZone.OrthoPositions[index];
        orthoCam.transform.SetPositionAndRotation(targetPos.position, targetPos.rotation);
        if(orthoCursor != null)
        {
            orthoCursor.RecalculateScreenPosition();
        }
    }


    //FSM DE LA CAMARA
    private void HandleTransition()
    {
        switch (transitionState)
        {
            case TransitionState.ExpandingFOV:
                ExpandFOVToMax();
                break;

            case TransitionState.BlendingPosition:
                WaitForBlend();
                break;

            case TransitionState.MovingOrthoCamera:
                break;

            case TransitionState.ShrinkingFOV:
                ShrinkFOVToNormal();
                break;

            case TransitionState.EnteringDrone:
                if (brain.ActiveBlend != null)
                {
                    _droneBlendStarted = true; // flag privat: private bool _droneBlendStarted = false;
                }
                if (_droneBlendStarted && brain.ActiveBlend == null)
                {
                    _droneBlendStarted = false;
                    if (droneHUD != null) droneHUD.Show();
                    playerStateMachine.SetViewMode(PlayerStateMachine.PlayerViewMode.DroneView);
                    transitionState = TransitionState.DroneActive;
                }
                break;

            case TransitionState.DroneActive:
                HandleDroneMovement();
                break;

            case TransitionState.ExitingDrone:
                if (brain.ActiveBlend == null)
                    transitionState = TransitionState.Idle;
                break;

            case TransitionState.ReturningToDrone:
                if (brain.ActiveBlend == null)
                {
                    float distCamToDrone = Vector3.Distance(
                        mainCamera.transform.position,
                        droneCameraPosition.transform.position
                    );

                    if (distCamToDrone < 0.5f)
                    {
                        if (droneGameObject != null) droneGameObject.SetActive(true);
                        transitionState = TransitionState.DroneActive;
                    }
                }
                break;
        }
    }
    private void ExpandFOVToMax() //De la camara perspectiva a la ortografica
    {
        var lens = perspectiveCam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, maxFOV, Time.deltaTime * fovTransitionSpeed);
        perspectiveCam.Lens = lens;

        if (lens.FieldOfView >= maxFOV - 1f)
        {
            mainCamera.orthographic = true;
            orthoCam.Priority = 20;
            perspectiveCam.Priority = 10;

            var resetLens = perspectiveCam.Lens;
            resetLens.FieldOfView = perspectiveFOV;
            perspectiveCam.Lens = resetLens;


            playerStateMachine.SetViewMode(PlayerStateMachine.PlayerViewMode.OrthographicView); //Notifiquem al PST que estem en orthographic
            isOrthoMode = true;
            IsOrthoMode = true;
            transitionState = TransitionState.BlendingPosition;
        }
    }

    private void WaitForBlend() //Fase d'espera mentre es fa el blend entre camares, per evitar que redueixi el FOV abans de que el blend acabi
    {
        if (brain.ActiveBlend == null)
        {
            if (!isOrthoMode) // viene de ortho → perspectiva
            {
                transitionState = TransitionState.ShrinkingFOV;
            }

            else
            {
                if (playerOverlayCamera != null)
                {
                    playerOverlayCamera.gameObject.SetActive(true);
                }

                transitionState = TransitionState.Idle;

                GameManager.instance.EvaluateConditions();
            }
        }
      

    }

    private void ShrinkFOVToNormal() //De ortografica a perspectiva
    {
        var lens = perspectiveCam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, perspectiveFOV, Time.deltaTime * fovTransitionSpeed);
        perspectiveCam.Lens = lens;

        if (lens.FieldOfView <= perspectiveFOV + 0.5f)
        {
            lens.FieldOfView = perspectiveFOV;
            perspectiveCam.Lens = lens;
            transitionState = TransitionState.Idle;

            GameManager.instance.EvaluateConditions();
        }
    }
}
