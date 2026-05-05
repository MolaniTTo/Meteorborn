using UnityEngine;

public class DAPSpeakGenerator : MonoBehaviour
{
    public string[] cosasQueDecir;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Drone"))
        {
            DapMissions speak = other.GetComponent<DapMissions>();
            speak.ShowText(cosasQueDecir);
        }
    }
}
