using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

public class MeteoritColocar : MonoBehaviour
{
    [Tooltip("Posicions objectiu")]
    [SerializeField] Transform[] posicions;
    [Tooltip("Objectes que s'han de posicionar")]
    [SerializeField] Transform[] objectes;
    [Tooltip("Duració de l'animació de col·locació")]
    [SerializeField] float placeDuration = 1f;
    [SerializeField] Ease placeEase = Ease.OutBack;

    private void OnTriggerEnter(Collider other)
    {
        // Detecta si és un CarryObject
        CarryObject carry = other.GetComponent<CarryObject>();
        if (carry == null) return;

        // Busca quin objecte de la llista coincideix
        for (int i = 0; i < objectes.Length; i++)
        {
            if (objectes[i] == other.transform && i < posicions.Length)
            {
                ColocarPeça(carry, objectes[i], posicions[i]);
                break;
            }
        }
    }

    private void ColocarPeça(CarryObject carry, Transform objecte, Transform posicio)
    {
        carry.ReleaseMinions();

        NavMeshAgent agent = objecte.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        Rigidbody rb = objecte.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (carry.carryObject != null)
        {
            carry.carryObject.transform.DOLocalMove(Vector3.zero, placeDuration * 0.5f).SetEase(Ease.OutSine);
            carry.carryObject.transform.DOLocalRotate(Vector3.zero, placeDuration * 0.5f).SetEase(Ease.OutSine);
        }

        // Una sola crida amb OnComplete
        objecte.DOMove(posicio.position, placeDuration).SetEase(placeEase)
            .OnComplete(() =>
            {
                carry.OnDelivered();
                Debug.Log($"[MeteoritColocar] Peça '{objecte.name}' col·locada a '{posicio.name}'.");
            });

        objecte.DORotateQuaternion(posicio.rotation, placeDuration).SetEase(placeEase);
    }
}