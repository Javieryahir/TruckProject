using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneGenerator : MonoBehaviour
{
    [Header("Plano base (Prefab 0.5 x 0.5)")]
    public GameObject basePlane;

    [Header("Tamaño del plano grande")]
    public int tilesX = 10;
    public int tilesY = 10;

    // ========================
    // EXTRA PREFAB (nuevo formato)
    // ========================
    [Header("Prefab extra que se colocará en coordenadas específicas")]
    public GameObject extraPrefab;

    [Header("Offset local del extra (x, y, z)")]
    public Vector3 extraLocalOffset = new Vector3(-1.85f, 0.5f, 0f);

    [Header("Velocidad de movimiento del extra (m/s)")]
    public float moveSpeed = 2f;

    [Header("Tiempo de espera en cada punto (s)")]
    public float waitAtPoint = 0.2f;

    [Header("¿Repetir la ruta indefinidamente? (loop)")]
    public bool loopPath = true;

    // ========================
    // OBSTACLES
    // ========================
    [Header("Prefab para obstáculos")]
    public GameObject obstaclePrefab;

    [Header("Offset local del obstacle (x, y, z)")]
    public Vector3 obstacleLocalOffset = new Vector3(0f, 0.5f, 0f);

    // ========================
    // Estructuras
    // ========================
    [System.Serializable]
    public struct Coord
    {
        public int x;
        public int y;
    }

    [System.Serializable]
    public class ExtraGroup
    {
        public Coord rootCoord;        // Donde se instanciará el prefab
        public List<Coord> pathCoords; // Lista de puntos que pertenecen al objeto (se moverá por estos)
    }

    [Header("Grupos de coordenadas para extraPrefab")]
    public List<ExtraGroup> extraGroups = new List<ExtraGroup>();

    [Header("Coordenadas para obstacles")]
    public List<Coord> obstacleCoords = new List<Coord>();

    private bool isClearing = false;

    // ========================
    // Inicialización
    // ========================
    void Start()
    {
        
    }

    void OnDestroy()
    {
        // Limpiar coroutines cuando se destruya el objeto
        StopAllCoroutines();
    }

    // ========================
    // Generar el plano
    // ========================
    public void GeneratePlane()
    {
        // Limpiar hijos previos
        ClearPlane();

        if (basePlane == null)
        {
            Debug.LogError("⚠️ No se asignó el plano base.");
            return;
        }

        Vector3 size = basePlane.GetComponent<Renderer>().bounds.size;

        for (int x = 0; x < tilesX; x++)
        {
            for (int y = 0; y < tilesY; y++)
            {
                GameObject tile = Instantiate(basePlane);

                tile.transform.position = new Vector3(
                    x * size.x,
                    0,
                    y * size.z
                );

                tile.transform.parent = this.transform;
                tile.name = $"Tile_{x}_{y}";

                // ========================
                // Instanciar EXTRA PREFAB según grupos (y lanzar movimiento)
                // ========================
                foreach (ExtraGroup group in extraGroups)
                {
                    if (group == null) continue;
                    if (group.rootCoord.x == x && group.rootCoord.y == y && extraPrefab != null)
                    {
                        // Instanciar en la posición root (tile + offset)
                        Vector3 rootWorldPos = tile.transform.position + extraLocalOffset;
                        GameObject obj = Instantiate(extraPrefab, this.transform);
                        obj.transform.position = rootWorldPos;
                        obj.name = $"ExtraRoot_{x}_{y}";

                        // Preparar la lista de posiciones world a partir de pathCoords
                        List<Vector3> pathWorldPositions = new List<Vector3>();

                        if (group.pathCoords != null)
                        {
                            foreach (Coord p in group.pathCoords)
                            {
                                // Convertir coord de tile a posición world usando 'size' y el mismo offset
                                Vector3 pointWorld = new Vector3(p.x * size.x, 0f, p.y * size.z) + extraLocalOffset;
                                pathWorldPositions.Add(pointWorld);
                            }
                        }

                        // Si hay puntos en path, iniciar el movimiento
                        if (pathWorldPositions.Count > 0)
                        {
                            StartCoroutine(MoveAlongPath(obj.transform, pathWorldPositions, moveSpeed, waitAtPoint, loopPath));
                        }

                        break;
                    }
                }

                // ========================
                // Instanciar OBSTACLE PREFAB
                // ========================
                foreach (Coord c in obstacleCoords)
                {
                    if (c.x == x && c.y == y && obstaclePrefab != null)
                    {
                        GameObject obj = Instantiate(obstaclePrefab, this.transform);
                        obj.transform.position = tile.transform.position + obstacleLocalOffset;
                        obj.name = $"Obstacle_{x}_{y}";
                        break;
                    }
                }
            }
        }
    }

    // ========================
    // Coroutine: mover un transform por una lista de posiciones world
    // ========================
    private IEnumerator MoveAlongPath(Transform mover, List<Vector3> positions, float speed, float waitSeconds, bool loop)
    {
        if (mover == null || positions == null || positions.Count == 0)
            yield break;

        int index = 0;

        while (true)
        {
            Vector3 target = positions[index];

            // --- ROTACIÓN EXACTA ---
            if (index > 0)
            {
                Vector3 dir = (target - mover.position).normalized;

                // Ignorar eje Y
                dir.y = 0;

                if (dir != Vector3.zero)
                {
                    Quaternion rot = Quaternion.LookRotation(dir);
                    mover.rotation = rot; // rotación directa
                }
            }

            // --- MOVIMIENTO ---
            while (Vector3.Distance(mover.position, target) > 0.01f)
            {
                mover.position = Vector3.MoveTowards(
                    mover.position,
                    target,
                    speed * Time.deltaTime
                );
                yield return null;
            }

            // --- FIN DEL RECORRIDO ---
            if (index == positions.Count - 1)
                yield break;

            index++;
        }
    }

    // ========================
    // Limpiar el plano de forma segura
    // ========================
    private void ClearPlane()
    {
        if (isClearing)
            return;

        isClearing = true;

        try
        {
            // Detener todas las coroutines primero
            StopAllCoroutines();

            // Obtener lista de hijos
            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in this.transform)
            {
                childrenToDestroy.Add(child.gameObject);
            }

            // Destruir hijos
            foreach (GameObject child in childrenToDestroy)
            {
                if (child != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(child);
                    }
                    else
                    {
                        DestroyImmediate(child, false);
                    }
                }
            }

            if (debugClearPlane)
                Debug.Log("🧹 Plano limpiado correctamente");
        }
        finally
        {
            isClearing = false;
        }
    }

    [SerializeField] private bool debugClearPlane = true;
}

