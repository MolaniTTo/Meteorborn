using UnityEngine;

public class MeteoritColocar : MonoBehaviour
{
    [SerializeField] Transform[] posicions;
    [SerializeField] bool[] posicionats;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ColocarObjecte(int identificador)
    {
        posicionats[identificador] = true;
    }
}
