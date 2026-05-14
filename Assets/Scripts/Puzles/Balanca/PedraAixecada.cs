using UnityEngine;

public class PedraAixecada : MonoBehaviour
{
    public bool aixecar = false;

    Vector3 objective;

    public HighlightObject highlightObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objective = new Vector3(transform.position.x, transform.position.y + 7f, transform.position.z);

        highlightObject.Highlight(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (aixecar & Vector3.Distance(transform.position, objective) > 0.2f)
        {
            transform.position = Vector3.MoveTowards(transform.position, objective, 1f*Time.deltaTime);
            
        }
    }

    public void AixecarPedra()
    {
        aixecar = true;
        highlightObject.Highlight(true);
        highlightObject.intensity = 600;
    }
}
