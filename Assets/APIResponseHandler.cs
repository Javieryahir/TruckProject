using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// ========================
// Estructuras para deserializar el JSON
// ========================
[System.Serializable]
public class Tractor
{
    public int id;
    public int[] posicion_inicial;
    public int[] posicion_actual;
    public int[][] movimientos;
    public int energia_consumida;
    public bool activo;
}

[System.Serializable]
public class AmbienteStats
{
    public int total_cosechable;
    public int celdas_cosechadas;
    public int max_visitas_celda;
}

[System.Serializable]
public class ObstaculoArray
{
    public int[] data;
}

[System.Serializable]
public class ObstaculosWrapper
{
    public ObstaculoArray[] obstaculos;
}

[System.Serializable]
public class APIResponse
{
    public string status;
    public int pasos_ejecutados;
    public float porcentaje_cosechado;
    public float porcentaje_solapamiento;
    public float eficiencia;
    public Tractor[] tractores;
    public int[][] obstaculos;
    public int grid_size;
    public AmbienteStats ambiente_stats;
}

/// <summary>
/// Maneja las llamadas al endpoint de entrenamiento y adapta la respuesta JSON
/// a la estructura de PlaneGenerator
/// </summary>
public class APIResponseHandler : MonoBehaviour
{
    [Header("Configuración API")]
    [SerializeField] private string apiUrl = "http://127.0.0.1:8000/entrenar";
    
    [Header("Referencia a PlaneGenerator")]
    [SerializeField] private PlaneGenerator planeGenerator;

    [Header("Opciones de visualización")]
    [SerializeField] private bool debugOutput = true;

    private APIResponse lastResponse;

    void Awake()
    {
        if (planeGenerator == null)
        {
            planeGenerator = GetComponent<PlaneGenerator>();
        }

        CallTrainingEndpoint();
    }

    /// <summary>
    /// Realiza la llamada al endpoint y adapta los datos recibidos
    /// </summary>
    public void CallTrainingEndpoint()
    {
        StartCoroutine(FetchTrainingData());
    }

    private IEnumerator FetchTrainingData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error en la llamada a {apiUrl}: {request.error}");
                yield break;
            }

            // Parsear el JSON
            string jsonResponse = request.downloadHandler.text;
            
            if (debugOutput)
            {
                Debug.Log($"📊 Respuesta JSON recibida:\n{jsonResponse}");
            }

            try
            {
                lastResponse = JsonUtility.FromJson<APIResponse>(jsonResponse);
                
                // Parsear manualmente los obstáculos y movimientos ya que JsonUtility no maneja bien int[][]
                ParseObstaculos(jsonResponse);
                ParseMovimientos(jsonResponse);
                
                

                // Adaptar los datos a PlaneGenerator
                AdaptResponseToPlaneGenerator();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error al parsear JSON:");
                Debug.LogError($"   Tipo de error: {e.GetType().Name}");
                Debug.LogError($"   Mensaje: {e.Message}");
                Debug.LogError($"   Stack trace: {e.StackTrace}");
                Debug.LogError($"\n📋 JSON recibido (primeros 500 caracteres):\n{jsonResponse.Substring(0, System.Math.Min(500, jsonResponse.Length))}...");
                
        
            }
        }
    }

    /// <summary>
    /// Adapta la respuesta del API a la estructura de PlaneGenerator
    /// </summary>
    private void AdaptResponseToPlaneGenerator()
    {
        if (planeGenerator == null || lastResponse == null)
        {
            Debug.LogError("❌ PlaneGenerator o respuesta no están disponibles");
            return;
        }

        // Ejecutar la adaptación en la siguiente coroutine para evitar conflictos de editor
        StartCoroutine(AdaptResponseCoroutine());
    }

    private IEnumerator AdaptResponseCoroutine()
    {
        // Esperar un frame para que se sincronice el inspector
        yield return new WaitForEndOfFrame();

        if (planeGenerator == null || lastResponse == null)
            yield break;

        // 1. Ajustar el tamaño del grid
        planeGenerator.tilesX = lastResponse.grid_size;
        planeGenerator.tilesY = lastResponse.grid_size;

        // 2. Procesar tractores como grupos de movimiento (extraGroups)
        planeGenerator.extraGroups = new List<PlaneGenerator.ExtraGroup>();

        if (lastResponse.tractores != null)
        {
            Debug.Log($"Procesando {lastResponse.tractores.Length} tractores...");
            foreach (Tractor tractor in lastResponse.tractores)
            {
                
                PlaneGenerator.ExtraGroup group = new PlaneGenerator.ExtraGroup();

                // Posición inicial del tractor (usar primer movimiento si está vacía)
                if (tractor.posicion_inicial != null && tractor.posicion_inicial.Length >= 2)
                {
                    group.rootCoord = new PlaneGenerator.Coord
                    {
                        x = tractor.posicion_inicial[0],
                        y = tractor.posicion_inicial[1]
                    };
                }
                else if (tractor.movimientos != null && tractor.movimientos.Length > 0 && tractor.movimientos[0].Length >= 2)
                {
                    // Si posicion_inicial está vacía, usar el primer movimiento
                    group.rootCoord = new PlaneGenerator.Coord
                    {
                        x = tractor.movimientos[0][0],
                        y = tractor.movimientos[0][1]
                    };
                }

                // Ruta de movimientos
                group.pathCoords = new List<PlaneGenerator.Coord>();

                if (tractor.movimientos != null && tractor.movimientos.Length > 0)
                {
                    foreach (int[] movimiento in tractor.movimientos)
                    {
                        if (movimiento != null && movimiento.Length >= 2)
                        {

                            group.pathCoords.Add(new PlaneGenerator.Coord
                            {
                                x = movimiento[0],
                                y = movimiento[1]
                            });
                        }
                    }
                }

                planeGenerator.extraGroups.Add(group);
            }
        }

        // 3. Procesar obstáculos
        planeGenerator.obstacleCoords = new List<PlaneGenerator.Coord>();

        if (lastResponse.obstaculos != null)
        {
            foreach (int[] obstaculo in lastResponse.obstaculos)
            {
                if (obstaculo != null && obstaculo.Length >= 2)
                {
                    planeGenerator.obstacleCoords.Add(new PlaneGenerator.Coord
                    {
                        x = obstaculo[0],
                        y = obstaculo[1]
                    });
                }
            }
        }

        if (debugOutput)
        {
            Debug.Log($"✅ Datos adaptados correctamente para PlaneGenerator");
            Debug.Log($"   - Grid: {lastResponse.grid_size}x{lastResponse.grid_size}");
            Debug.Log($"   - Tractores activos: {planeGenerator.extraGroups.Count}");
            Debug.Log($"   - Obstáculos: {planeGenerator.obstacleCoords.Count}");
        }

        // 4. Regenerar el plano con los nuevos datos
        planeGenerator.GeneratePlane();
    }

    /// <summary>
    /// Imprime detalles de la respuesta en la consola
    /// </summary>
   

    /// <summary>
    /// Retorna la última respuesta recibida
    /// </summary>
    public APIResponse GetLastResponse()
    {
        return lastResponse;
    }

    /// <summary>
    /// Retorna estadísticas formateadas
    /// </summary>
    public string GetFormattedStats()
    {
        if (lastResponse == null)
            return "No hay datos disponibles";

        return $@"
═══════════════════════════════════
   ESTADÍSTICAS DE ENTRENAMIENTO
═══════════════════════════════════
Status: {lastResponse.status}
Pasos: {lastResponse.pasos_ejecutados}
Cosechado: {lastResponse.porcentaje_cosechado}%
Eficiencia: {lastResponse.eficiencia}%
Solapamiento: {lastResponse.porcentaje_solapamiento}%
Grid: {lastResponse.grid_size}x{lastResponse.grid_size}
Tractores activos: {GetActiveTractorsCount()}
Obstáculos: {(lastResponse.obstaculos != null ? lastResponse.obstaculos.Length : 0)}
═══════════════════════════════════";
    }

    private int GetActiveTractorsCount()
    {
        int count = 0;
        if (lastResponse.tractores != null)
        {
            foreach (Tractor t in lastResponse.tractores)
            {
                if (t != null && t.activo)
                    count++;
            }
        }
        return count;
    }

   

    /// <summary>
    /// Parsea manualmente los obstáculos del JSON
    /// </summary>
    private void ParseObstaculos(string json)
    {
        try
        {
            // Buscar el array de obstáculos en el JSON
            int startIndex = json.IndexOf("\"obstaculos\":");
            if (startIndex == -1)
            {
                Debug.LogWarning("⚠️ No se encontró el campo 'obstaculos' en el JSON");
                lastResponse.obstaculos = new int[0][];
                return;
            }

            startIndex = json.IndexOf("[", startIndex);
            int endIndex = json.IndexOf("]", startIndex);

            // Encontrar el fin correcto del array considerando arrays anidados
            int bracketCount = 0;
            for (int i = startIndex; i < json.Length; i++)
            {
                if (json[i] == '[')
                    bracketCount++;
                else if (json[i] == ']')
                {
                    bracketCount--;
                    if (bracketCount == 0)
                    {
                        endIndex = i;
                        break;
                    }
                }
            }

            string obstaculosJson = json.Substring(startIndex, endIndex - startIndex + 1);

            // Parsear cada obstáculo como un array de enteros
            List<int[]> obstaculos = new List<int[]>();

            // Dividir por arrays individuales
            int depth = 0;
            int arrayStart = 0;

            for (int i = 0; i < obstaculosJson.Length; i++)
            {
                if (obstaculosJson[i] == '[')
                {
                    if (depth == 1)
                        arrayStart = i;
                    depth++;
                }
                else if (obstaculosJson[i] == ']')
                {
                    depth--;
                    if (depth == 1)
                    {
                        string arrayStr = obstaculosJson.Substring(arrayStart, i - arrayStart + 1);
                        int[] values = ParseIntArray(arrayStr);
                        if (values != null && values.Length > 0)
                            obstaculos.Add(values);
                    }
                }
            }

            lastResponse.obstaculos = obstaculos.ToArray();

            if (debugOutput)
                Debug.Log($"✅ Obstáculos parseados: {lastResponse.obstaculos.Length} encontrados");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al parsear obstáculos: {e.Message}");
            lastResponse.obstaculos = new int[0][];
        }
    }

    /// <summary>
    /// Parsea un string como array de enteros
    /// </summary>
    private int[] ParseIntArray(string arrayStr)
    {
        try
        {
            // Limpiar caracteres
            arrayStr = arrayStr.Trim();
            if (arrayStr.StartsWith("["))
                arrayStr = arrayStr.Substring(1);
            if (arrayStr.EndsWith("]"))
                arrayStr = arrayStr.Substring(0, arrayStr.Length - 1);

            string[] values = arrayStr.Split(',');
            List<int> result = new List<int>();

            foreach (string val in values)
            {
                if (int.TryParse(val.Trim(), out int num))
                    result.Add(num);
            }

            return result.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parsea manualmente los movimientos de cada tractor del JSON
    /// </summary>
    private void ParseMovimientos(string json)
    {
        try
        {
            if (lastResponse.tractores == null || lastResponse.tractores.Length == 0)
                return;

            // Para cada tractor, parsear sus movimientos
            for (int i = 0; i < lastResponse.tractores.Length; i++)
            {
                Tractor tractor = lastResponse.tractores[i];
                if (tractor == null)
                    continue;

                // Buscar el patrón "\"movimientos\":" para este tractor
                // Lo hacemos de forma simple buscando en el JSON por el ID del tractor
                string tractorPattern = $"\"id\":{tractor.id}";
                int tractorStartIndex = json.IndexOf(tractorPattern);

                if (tractorStartIndex == -1)
                    continue;

                // Buscar "movimientos" después de este tractor
                int movimientosIndex = json.IndexOf("\"movimientos\":", tractorStartIndex);
                if (movimientosIndex == -1)
                    continue;

                // Encontrar el inicio del array
                int arrayStart = json.IndexOf("[", movimientosIndex);
                if (arrayStart == -1)
                    continue;

                // Encontrar el fin del array (considerando arrays anidados)
                int bracketCount = 0;
                int arrayEnd = arrayStart;

                for (int j = arrayStart; j < json.Length; j++)
                {
                    if (json[j] == '[')
                        bracketCount++;
                    else if (json[j] == ']')
                    {
                        bracketCount--;
                        if (bracketCount == 0)
                        {
                            arrayEnd = j;
                            break;
                        }
                    }
                }

                string movimientosJson = json.Substring(arrayStart, arrayEnd - arrayStart + 1);

                // Parsear cada movimiento
                List<int[]> movimientos = new List<int[]>();
                depth = 0;
                arrayStart = 0;

                for (int j = 0; j < movimientosJson.Length; j++)
                {
                    if (movimientosJson[j] == '[')
                    {
                        if (depth == 1)
                            arrayStart = j;
                        depth++;
                    }
                    else if (movimientosJson[j] == ']')
                    {
                        depth--;
                        if (depth == 1)
                        {
                            string coordStr = movimientosJson.Substring(arrayStart, j - arrayStart + 1);
                            int[] coord = ParseIntArray(coordStr);
                            if (coord != null && coord.Length >= 2)
                                movimientos.Add(coord);
                        }
                    }
                }

                tractor.movimientos = movimientos.ToArray();

                if (debugOutput)
                    Debug.Log($"   ✅ Tractor {tractor.id}: {tractor.movimientos.Length} movimientos parseados");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al parsear movimientos: {e.Message}");
        }
    }

    private int depth; // Variable temporal para ParseMovimientos
}
