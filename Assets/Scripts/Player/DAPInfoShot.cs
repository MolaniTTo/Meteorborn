using UnityEngine;
using UnityEngine.InputSystem;

public class DAPInfoShot : MonoBehaviour
{
    private float rayDistance = 100f;
    [SerializeField] private InputActionReference jumpAction;
    private DapMissions dapMissions;

    private void Start() {
        dapMissions = GameObject.FindWithTag("Drone").GetComponent<DapMissions>();
    }

    private void OnEnable()
    {
        if (jumpAction != null)
            jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        if (jumpAction != null)
            jumpAction.action.Disable();
    }

    private void Update()
    {
        if (jumpAction != null && jumpAction.action.WasPressedThisFrame())
        {
            ShootRay();
        }
    }

    void ShootRay()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            Debug.Log("Has impactat amb: " + hit.collider.name);

            GameObject tempObject = hit.collider.gameObject;

            DAPDescriptionText descTemp = tempObject.GetComponent<DAPDescriptionText>();
            
            if (descTemp != null)
            {
                dapMissions.ShowText(descTemp.cosasQueDecir);
            }
        }

        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red, 2f);
    }
}