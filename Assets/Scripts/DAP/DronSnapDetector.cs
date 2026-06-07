using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class DroneSnapDetector : MonoBehaviour
{
    public static DroneSnapDetector Instance { get; private set; }

    [Header("Detecció")]
    [SerializeField] private float snapRotationThreshold = 15f;
    [SerializeField] private LayerMask visibilityMask;

    [Header("Referència càmera dron")]
    [SerializeField] private Camera droneCamera; // la Main Camera quan estem en mode dron
    [SerializeField] private Transform droneCameraTransform;

    public bool HasSnap { get; private set; } = false;
    public OrthoSnapPoint BestSnap { get; private set; } = null;
    public System.Action<bool> OnSnapChanged;

    private OrthoSnapPoint[] _allSnapPoints;
    private bool _wasSnap = false;
    private NavMeshLinkMarkers _currentAlignedMarkers = null;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        // Cache quan s'activa — evitem FindObjectsByType cada frame
        _allSnapPoints = FindObjectsByType<OrthoSnapPoint>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        DetectSnap();

        if (HasSnap != _wasSnap)
        {
            _wasSnap = HasSnap;
            // Notifica els marcadors anteriors que ja no estan alineats
            if (_currentAlignedMarkers != null)
            {
                _currentAlignedMarkers.SetAligned(false);
                _currentAlignedMarkers = null;
            }

            // Si hi ha snap nou, notifica els marcadors corresponents
            if (HasSnap && BestSnap != null)
            {
                _currentAlignedMarkers = BestSnap.navMeshLink.GetComponent<NavMeshLinkMarkers>();
                if (_currentAlignedMarkers != null)
                    _currentAlignedMarkers.SetAligned(true);
            }
            OnSnapChanged?.Invoke(HasSnap);
        }
    }

    private void DetectSnap()
    {
        OrthoSnapPoint best = null;
        float bestAngleDiff = float.MaxValue;

        if (_allSnapPoints == null) return;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(droneCamera);

        foreach (OrthoSnapPoint snap in _allSnapPoints)
        {
            if (snap == null || snap.navMeshLink == null) continue;

            Vector3 linkStart = snap.navMeshLink.transform.TransformPoint(snap.navMeshLink.startPoint);
            Vector3 linkEnd = snap.navMeshLink.transform.TransformPoint(snap.navMeshLink.endPoint);
            Vector3 linkMid = (linkStart + linkEnd) * 0.5f;

            // Filtre 1: el punt mig del link ha d'estar dins del frustum de la càmera
            Bounds linkBounds = new Bounds(linkMid, Vector3.one * 0.5f);
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, linkBounds)) continue;

            // Filtre 2: rotació X i Y del dron vs el OrthoSnapPoint
            float diffX = Mathf.Abs(Mathf.DeltaAngle(
                droneCameraTransform.eulerAngles.x,
                snap.transform.eulerAngles.x
            ));
            float diffY = Mathf.Abs(Mathf.DeltaAngle(
                droneCameraTransform.eulerAngles.y,
                snap.transform.eulerAngles.y
            ));

            if (diffX > snapRotationThreshold) continue;
            if (diffY > snapRotationThreshold) continue;

            // Filtre 3: linecast al punt mig del link — comprova que no hi hagi
            // geometria bloquejant entre el dron i el link
            if (Physics.Linecast(droneCameraTransform.position, linkMid, visibilityMask)) continue;

            float totalDiff = diffX + diffY;
            if (totalDiff < bestAngleDiff)
            {
                bestAngleDiff = totalDiff;
                best = snap;
            }
        }

        BestSnap = best;
        HasSnap = best != null;
    }

    public Quaternion GetSnappedRotation()
    {
        if (BestSnap == null) return droneCameraTransform.rotation;
        return Quaternion.Euler(
            BestSnap.transform.eulerAngles.x,
            BestSnap.transform.eulerAngles.y,
            0f
        );
    }

    private void OnDrawGizmos()
    {
        if (droneCameraTransform == null || !HasSnap) return;
        if (BestSnap?.navMeshLink == null) return;

        Vector3 linkStart = BestSnap.navMeshLink.transform.TransformPoint(BestSnap.navMeshLink.startPoint);
        Vector3 linkEnd = BestSnap.navMeshLink.transform.TransformPoint(BestSnap.navMeshLink.endPoint);
        Vector3 linkMid = (linkStart + linkEnd) * 0.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(droneCameraTransform.position, linkMid);
        Gizmos.DrawWireSphere(linkMid, 1f);
    }
}