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
    [SerializeField] private float cursorSpeed = 8f;            //Velocitat de moviment del cursor
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

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        gameObject.SetActive(active);
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

        // Per a camara isometrica: right de la camara per a X, i forward aplanat per a Y
        Vector3 camRight = mainCamera.transform.right;

        // Aplanem el forward de la camara al pla XZ per a que el moviment sigui horitzontal
        Vector3 camForward = mainCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        // moveInput.y positiu = endavant (cap a on mira la camara en el pla XZ)
        // moveInput.y negatiu = cap enrere
        Vector3 candidatePosition = transform.position
            + camRight * moveInput.x * cursorSpeed * Time.deltaTime
            + camForward * moveInput.y * cursorSpeed * Time.deltaTime;

        // Raycast des de la camara cap al candidat per adherir-nos a la geometria real
        Ray ray = mainCamera.ScreenPointToRay(
            mainCamera.WorldToScreenPoint(candidatePosition)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, geometryMask))
        {
            candidatePosition = hit.point;
        }

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
    }

    private void UpdateVisuals()
    {
        if (cursorRenderer == null) return;

        cursorRenderer.material.color = currentMode == CursorMode.NavMesh ? navMeshColor : freeColor;
    }

    public Vector3 GetWorldPosition() => transform.position;

}
