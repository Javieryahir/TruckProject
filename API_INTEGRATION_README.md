# Integración de API de Entrenamiento - TruckProject

## 📋 Descripción

Este sistema permite conectar tu proyecto Unity con el endpoint de entrenamiento (`http://127.0.0.1:8000/entrenar`) y adaptar automáticamente la respuesta JSON a tu escena de `PlaneGenerator`.

## 🔧 Componentes

### 1. **APIResponseHandler.cs**

- Realiza llamadas HTTP al endpoint de entrenamiento
- Deserializa la respuesta JSON
- Adapta los datos a la estructura de `PlaneGenerator`
- Maneja tractores activos como grupos de movimiento
- Procesa obstáculos automáticamente

### 2. **TrainingControlPanel.cs** (Opcional)

- Panel de control con botones para:
  - Iniciar entrenamiento
  - Actualizar estadísticas
- Muestra estadísticas formateadas

## 📦 Estructura JSON esperada

```json
{
  "status": "completed",
  "pasos_ejecutados": 107,
  "porcentaje_cosechado": 100,
  "porcentaje_solapamiento": 20.86,
  "eficiencia": 79.14,
  "tractores": [
    {
      "id": 0,
      "posicion_inicial": [x, y],
      "posicion_actual": [x, y],
      "movimientos": [[x, y], [x, y], ...],
      "energia_consumida": 101,
      "activo": true
    }
  ],
  "obstaculos": [[x, y], [x, y], ...],
  "grid_size": 15,
  "ambiente_stats": {}
}
```

## 🚀 Cómo usar

### Opción 1: Mediante código

```csharp
APIResponseHandler handler = GetComponent<APIResponseHandler>();
handler.CallTrainingEndpoint();
```

### Opción 2: Mediante botón en el Inspector

1. Crea un GameObject en la escena
2. Añade el componente `APIResponseHandler`
3. Asigna la referencia de `PlaneGenerator`
4. En otro GameObject, añade `TrainingControlPanel`
5. Asigna los botones y campos de texto en el inspector
6. Haz clic en "Train" para iniciar

### Opción 3: Mediante script personalizado

```csharp
public void TrainModel()
{
    GetComponent<APIResponseHandler>().CallTrainingEndpoint();
}
```

## 📊 Flujo de datos

```
Endpoint (/entrenar)
        ↓
    JSON Response
        ↓
    APIResponseHandler deserializa
        ↓
    Adapta a PlaneGenerator
        ├─ Actualiza grid_size (tilesX, tilesY)
        ├─ Convierte tractores → extraGroups
        └─ Convierte obstáculos → obstacleCoords
        ↓
    GeneratePlane() regenera la escena
```

## 🎯 Mapeo de datos

| API JSON                       | PlaneGenerator          | Descripción                        |
| ------------------------------ | ----------------------- | ---------------------------------- |
| `grid_size`                    | `tilesX`, `tilesY`      | Tamaño del grid                    |
| `tractores[].posicion_inicial` | `ExtraGroup.rootCoord`  | Posición inicial del prefab        |
| `tractores[].movimientos`      | `ExtraGroup.pathCoords` | Ruta a seguir                      |
| `obstaculos`                   | `obstacleCoords`        | Posiciones de obstáculos           |
| `tractores[].activo`           | Filtro                  | Solo se procesan tractores activos |

## 🔍 Debug

Activa la opción `debugOutput` en el inspector de `APIResponseHandler` para ver:

- Respuesta JSON completa
- Detalles de cada tractor
- Conteo de obstáculos
- Confirmación de adaptación de datos

## ⚙️ Configuración recomendada

1. Crea un GameObject vacío llamado "TrainingManager"
2. Asigna `APIResponseHandler` al mismo
3. Asigna la referencia a tu `PlaneGenerator` existente
4. Activa `debugOutput` para verificar que todo funciona
5. Llama a `CallTrainingEndpoint()` cuando sea necesario

## 🐛 Troubleshooting

### Error: "Cannot POST /entrenar"

- Verifica que el servidor Python está corriendo en `http://127.0.0.1:8000`
- Revisa la URL del endpoint en el inspector

### JSON no deserializa

- Verifica que la respuesta coincida con la estructura esperada
- Comprueba que no hay campos adicionales no documentados
- Activa `debugOutput` para ver la respuesta completa

### PlaneGenerator no se regenera

- Asegúrate de que la referencia está correctamente asignada
- Verifica que `extraPrefab` y `obstaclePrefab` están asignados
- Revisa la consola de errores

## 📝 Notas

- Solo se procesan **tractores con `activo: true`**
- Los obstáculos se colocan en las posiciones especificadas
- La escena se regenera completamente al adaptar nuevos datos
- El sistema preserva la configuración de velocidad y delays del PlaneGenerator
