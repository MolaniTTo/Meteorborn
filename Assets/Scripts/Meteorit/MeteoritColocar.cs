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
    [SerializeField] private bool firstTimePlacingPiece = true;
    [SerializeField] private TutorialEntry firstTimePlacingTutorial;
    [SerializeField] private bool lastPiecePlaced = false;
    [SerializeField] private TutorialEntry lastPiecePlacedTutorial;
    [SerializeField] private BlueprintMeteorit blueprintMeteorit;

    [Header("Audio")]
    [SerializeField] private AudioSource droneEnterAudio;
    [SerializeField] private AudioClip colocarPeça;

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
                blueprintMeteorit.ChangeColor(new Color(0.4f,0.7f,0.4f,0.7f), i);
                ColocarPeça(carry, objectes[i], posicions[i]);
                break;
            }
        }
    }

    private int piecesPlaced = 0;

    private void ColocarPeça(CarryObject carry, Transform objecte, Transform posicio)
    {
        if (firstTimePlacingPiece)
        {
            firstTimePlacingPiece = false;
            TutorialManager.Instance?.TriggerIfNew("hasPlacedPiece", () =>
                DroneSpeaker.Instance?.Speak(firstTimePlacingTutorial));
        }

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

        if (droneEnterAudio != null && colocarPeça != null)
            droneEnterAudio.PlayOneShot(colocarPeça);

        objecte.DOMove(posicio.position, placeDuration).SetEase(placeEase)
            .OnComplete(() =>
            {
                carry.OnDelivered();
                piecesPlaced++;
                Debug.Log($"[MeteoritColocar] Peça '{objecte.name}' col·locada. ({piecesPlaced}/{objectes.Length})");

                if (piecesPlaced >= objectes.Length)
                {
                    TutorialManager.Instance?.TriggerIfNew("hasPlacedAllPieces", () =>
                        DroneSpeaker.Instance?.Speak(lastPiecePlacedTutorial));
                }
            });

        objecte.DORotateQuaternion(posicio.rotation, placeDuration).SetEase(placeEase);
    }
}