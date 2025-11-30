using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de control para interactuar con el API de entrenamiento
/// </summary>
public class TrainingControlPanel : MonoBehaviour
{
    [SerializeField] private APIResponseHandler apiHandler;
    [SerializeField] private Text statsDisplay;
    [SerializeField] private Button trainButton;
    [SerializeField] private Button refreshStatsButton;

    void Start()
    {
        if (apiHandler == null)
        {
            apiHandler = FindObjectOfType<APIResponseHandler>();
        }

        if (trainButton != null)
        {
            trainButton.onClick.AddListener(OnTrainButtonClicked);
        }

        if (refreshStatsButton != null)
        {
            refreshStatsButton.onClick.AddListener(OnRefreshStatsClicked);
        }

        UpdateStatsDisplay();
    }

    private void OnTrainButtonClicked()
    {
        if (apiHandler != null)
        {
            Debug.Log("🚀 Iniciando llamada al endpoint de entrenamiento...");
            apiHandler.CallTrainingEndpoint();
            
            // Actualizar estadísticas después de un pequeño delay
            Invoke(nameof(UpdateStatsDisplay), 1f);
        }
    }

    private void OnRefreshStatsClicked()
    {
        UpdateStatsDisplay();
    }

    private void UpdateStatsDisplay()
    {
        if (statsDisplay != null && apiHandler != null)
        {
            statsDisplay.text = apiHandler.GetFormattedStats();
        }
    }
}
