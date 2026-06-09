// FontParticulesScript.cs
using UnityEngine;
using System.Collections.Generic;

public class FontParticulesScript : MonoBehaviour
{
    [Header("Configuració")]
    [SerializeField] private GameObject particulaPrefab;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private int maxParticulesEnArea = 5;
    [SerializeField] private float spawnRadius = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip spawnPop;
    private AudioSource audioSource;

    // Pool
    private List<ParticulaScript> pool = new List<ParticulaScript>();
    private List<ParticulaScript> activeParticules = new List<ParticulaScript>();

    private bool playerADins = false;
    private float contador = 0f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Pre-instancia el pool
        for (int i = 0; i < maxParticulesEnArea; i++)
        {
            GameObject go = Instantiate(particulaPrefab, transform);
            go.SetActive(false);
            ParticulaScript ps = go.GetComponent<ParticulaScript>();
            pool.Add(ps);
        }
    }

    private void Update()
    {
        //if (!playerADins) return;
        if (activeParticules.Count >= maxParticulesEnArea) return;

        contador += Time.deltaTime;
        if (contador >= spawnInterval)
        {
            contador = 0f;
            SpawnParticula();
        }
    }

    private void SpawnParticula()
    {
        ParticulaScript p = GetFromPool();
        if (p == null) return;

        // Posició aleatoria al voltant de la font, a l'alçada de la font
        Vector2 rand = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = transform.position + new Vector3(rand.x, 0f, rand.y);
        p.transform.position = pos;
        p.gameObject.SetActive(true);
        p.Init(this);
        activeParticules.Add(p);

        if (audioSource != null && spawnPop != null)
            audioSource.PlayOneShot(spawnPop);
    }

    public void ReturnToPool(ParticulaScript p)
    {
        p.gameObject.SetActive(false);
        activeParticules.Remove(p);
        pool.Add(p);
    }

    private ParticulaScript GetFromPool()
    {
        if (pool.Count == 0) return null;
        ParticulaScript p = pool[pool.Count - 1];
        pool.RemoveAt(pool.Count - 1);
        return p;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerADins = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerADins = false;
    }
}