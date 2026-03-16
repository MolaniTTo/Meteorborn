using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    //Player
    [Header("Player")]
    [SerializeField] private PlayerStateMachine playerStateMachine; 

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

    //Input
    private InputSystem_Actions inputActions;

    //States
    private enum TransitionState 
    { 
        Idle, 
        ExpandingFOV, 
        BlendingPosition, 
        MovingOrthoCamera,
        ShrinkingFOV 
    }

    private TransitionState transitionState = TransitionState.Idle;
    private bool isOrthoMode = false;           //Default esta en perspectiva tercera persona


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
        inputActions.Player.SwitchCamera.performed += OnSwitchCameraPerformed;                  //Quan es prem el boto de canvi de camara esta subcrit al metode OnSwitchCamera
        inputActions.Player.CycleCameraPosition.performed += OnCycleCameraPositionPerformed;    //Quan es prem el boto de ciclar posicio de camara esta subcrit al metode OnCycleCameraPosition
    }

    private void OnDisable()
    {
        inputActions.Player.SwitchCamera.performed -= OnSwitchCameraPerformed;
        inputActions.Player.CycleCameraPosition.performed -= OnCycleCameraPositionPerformed;
        inputActions.Player.Disable();
    }

    private void OnSwitchCameraPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => OnSwitchCamera();                  //Subscrivim el event del input system al metode OnSwitchCamera (cada cop que es prem el boto, es cridara a la funcio)
    private void OnCycleCameraPositionPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => OnCycleCameraPosition();    //Subscrivim el event del input system al metode OnCycleCameraPosition (cada cop que es prem el boto, es cridara a la funcio)

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

    private void OnSwitchCamera()
    {
        Debug.Log("Switch camera input received. Current mode: " + (isOrthoMode ? "Orthographic" : "Perspective"));

        if (transitionState != TransitionState.Idle) { Debug.LogWarning("Ja hi ha una transicio en proces"); return; } //Ignora si ja tenim una transicio en proces

        if (!isOrthoMode)
        {
            if (!CanSwitchToOrtho) { Debug.LogWarning("La zona no permet ortho"); return; }    //Es bloqueja si la zona no permet ortho
            StartSwitchToOrtho();
        }
        else
        {
            StartSwitchToPerspective();
        }
    }

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
        transitionState = TransitionState.Idle;
    }

    //SWITCH LOGIC

    private void StartSwitchToOrtho()
    {
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
    }

    private void ApplyOrthoPosition(int index)
    {
        if (currentZone.OrthoPositions == null || currentZone.OrthoPositions.Length == 0) { return; } //Si no hi ha posicions ortho definides, no fa res

        Transform targetPos = currentZone.OrthoPositions[index];
        orthoCam.transform.SetPositionAndRotation(targetPos.position, targetPos.rotation);
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
                //No fa res, el moviment es gestiona per la corrutina MoveOrthoCamera, i quan acaba la corrutina, canvia l'estat a Idle
                break;

            case TransitionState.ShrinkingFOV:
                ShrinkFOVToNormal();
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

            if (playerOverlayCamera != null)
            {
                playerOverlayCamera.gameObject.SetActive(true);
            }

            playerStateMachine.SetViewMode(PlayerStateMachine.PlayerViewMode.OrthographicView); //Notifiquem al PST que estem en orthographic
            isOrthoMode = true;
            transitionState = TransitionState.Idle;
        }
    }

    private void WaitForBlend() //Fase d'espera mentre es fa el blend entre camares, per evitar que redueixi el FOV abans de que el blend acabi
    {
        if (brain.ActiveBlend == null)
        {
            transitionState = TransitionState.ShrinkingFOV;
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
        }
    }
}
