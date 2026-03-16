using UnityEngine;
using UnityEngine.AI;

public class OrthoCursor : MonoBehaviour
{
    public enum CursorMode
    {
        NavMesh,    //Cursor sobre NavMesh -> mou el player
        Free        //Cursor lliure -> interactua amb objectes
    }

    [Header("Cursor Settings")]
    [SerializeField] private float cursorScreenSpeed = 800f;   // Velocitat en pixels per segon
    [SerializeField] private float navMeshSampleRadius = 1f;    //Radi de cerca de NavMesh valid al voltant del cursor

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask geometryMask;            // Layer de tota la geometria (sol, parets, plataformes, objectes)

    [Header("Visuals")]
    [SerializeField] private Renderer cursorRenderer;           // Renderer del cursor per canviar el color
    [SerializeField] private Color navMeshColor = Color.green;  // Color quan esta sobre NavMesh
    [SerializeField] private Color freeColor = Color.red;       // Color quan esta en mode lliure

    //STATE
    private CursorMode currentMode = CursorMode.NavMesh;
    private Vector2 moveInput;
    private Camera mainCamera;
    private bool isActive = false;

    public CursorMode CurrentMode => currentMode;
    public Vector3 WorldPosition => transform.position;

    private Vector3 currentScreenPos;   //Variable per guardar la posicio de la pantalla independent al snap

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        currentScreenPos = mainCamera.WorldToScreenPoint(transform.position);
    }

    public void SetActive(bool active)
    {
        isActive = active;
        gameObject.SetActive(active);

        if (active) { RecalculateScreenPosition(); } // Recalculem la posicio de pantalla quan s'activa el cursor, per evitar salts visuals si la camara ha canviat mentre estava inactiu
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    private void Update()
    {
        if (!isActive) return;
        MoveCursor();
        UpdateVisuals();
    }

    private void MoveCursor()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        // Calculem la nova posicio de pantalla candidata
        Vector3 candidateScreenPos = currentScreenPos;
        candidateScreenPos.x += moveInput.x * cursorScreenSpeed * Time.deltaTime;
        candidateScreenPos.y += moveInput.y * cursorScreenSpeed * Time.deltaTime;

        candidateScreenPos.x = Mathf.Clamp(candidateScreenPos.x, 0f, Screen.width);
        candidateScreenPos.y = Mathf.Clamp(candidateScreenPos.y, 0f, Screen.height);

        Ray ray = mainCamera.ScreenPointToRay(candidateScreenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, geometryMask))
        {
            Vector3 candidatePosition = hit.point;

            if (NavMesh.SamplePosition(candidatePosition, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                transform.position = navHit.position;
                currentMode = CursorMode.NavMesh;
            }
            else
            {
                transform.position = candidatePosition;
                currentMode = CursorMode.Free;
            }

            // Només actualitzem currentScreenPos si hem trobat geometria valida
            currentScreenPos = candidateScreenPos;
        }
        // Si no hi ha geometria, currentScreenPos no s'actualitza i el cursor no es mou
    }

    private void UpdateVisuals()
    {
        if (cursorRenderer == null) return;

        cursorRenderer.material.color = currentMode == CursorMode.NavMesh ? navMeshColor : freeColor;
    }

    public void RecalculateScreenPosition()
    {
        // Recalculem la posicio de pantalla a partir de la posicio 3D actual del cursor
        // Aixo cal fer-ho quan canvia la camara o la mida de la pantalla
        currentScreenPos = mainCamera.WorldToScreenPoint(transform.position);
    }

    public Vector3 GetWorldPosition() => transform.position;

}
