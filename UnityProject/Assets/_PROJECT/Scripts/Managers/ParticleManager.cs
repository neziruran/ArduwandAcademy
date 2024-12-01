using UnityEngine;
using System.Collections;
using System.Threading;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("Particle Prefabs")]
    public GameObject correctIngredientEffect;

    [Header("Settings")]
    public float particleLifetime = 5f; // Time in seconds before particles are destroyed

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SpawnEffect(GameObject effect, Vector3 position)
    {
        if (effect == null)
        {
            Debug.LogWarning("No particle effect assigned!");
            return;
        }

        GameObject spawnedEffect = Instantiate(effect, position, Quaternion.identity);

        // Schedule destruction of the particle effect
        Destroy(spawnedEffect, particleLifetime);
    }

    public void PlayCorrectIngredientEffect(Vector3 position)
    {
        SpawnEffect(correctIngredientEffect, position);
    }
}