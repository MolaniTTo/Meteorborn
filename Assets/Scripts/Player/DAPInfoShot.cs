using UnityEngine;
using UnityEngine.InputSystem;

public class DAPInfoShot : MonoBehaviour
{

    private float rayDistance = 100f;
    public InputAction shootAction;

    private void Update() {
        if (shootAction.WasPressedThisFrame())
        {
            ShootRay();
            Debug.Log("Mamacita");
        }
    }

    void ShootRay()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            Debug.Log("Has impactat amb: " + hit.collider.name);
        }

        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red, 2f);
    }
}
