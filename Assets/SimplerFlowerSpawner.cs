using UnityEngine;

public class SimpleFlowerSpawner : MonoBehaviour
{
    public GameObject flowerPrefab;     // Prefab de la flor
    public MeshRenderer planeRenderer;  // El MeshRenderer del plano

    void Start()
    {
        if (flowerPrefab == null || planeRenderer == null)
        {
            Debug.LogError("Asigna flowerPrefab y planeRenderer en el inspector.");
            return;
        }

        SpawnFlowers();
    }

    void SpawnFlowers()
    {
        // obtener tamaño del plano
        Vector3 size = planeRenderer.bounds.size;

        // obtener centro del plano
        Vector3 center = planeRenderer.bounds.center;

        // cuántas flores (1 a 3)
        int count = Random.Range(1, 3);

        for (int i = 0; i < count; i++)
        {
            // posición aleatoria dentro del rectángulo del plano
            float x = Random.Range(center.x - size.x / 2f, center.x + size.x / 2f);
            float z = Random.Range(center.z - size.z / 2f, center.z + size.z / 2f);
            float y = center.y; // altura del plano

            Vector3 spawnPos = new Vector3(x, y, z);

            // crear la flor
            Instantiate(flowerPrefab, spawnPos, Quaternion.identity, transform);
        }
    }
}
