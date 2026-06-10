using DG.Tweening;
using UnityEngine;

public class PedraAixecada : MonoBehaviour
{
    [SerializeField] private float alçada = 7f;
    [SerializeField] private float durada = 2f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    private bool aixecada = false;

    public void AixecarPedra()
    {
        if (aixecada) return;
        aixecada = true;
        transform.DOMoveY(alçada, durada).SetEase(ease).SetRelative()
       .OnComplete(() =>
       {
           UniqueID uid = GetComponent<UniqueID>();
           if (uid != null) { WorldManager.Instance?.RegisterMovedObject(uid.ID, transform.position, transform.rotation); }
           SaveManager.Instance?.Save();
       });

    }
}