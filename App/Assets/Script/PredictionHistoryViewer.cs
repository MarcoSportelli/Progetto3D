using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SinglePrediction
{
    public string predizione;
    public float angolo;
    public string gamba;
    public string timestamp;
    public string feedback; 

}

public class PredictionHistoryViewer : MonoBehaviour
{
    private List<SinglePrediction> predictions = new List<SinglePrediction>();

    public Button btnSquat;
    public Button btnFlessioneAvanti;
    public Button btnFlessioneIndietro;
    public Button btnEstensione;
    public Button btnToggleOrder;

    public GameObject scrollViewContent;
    public GameObject predictionItemPrefab;

    private string currentMovement = "squat";
    private bool ascendingOrder = false;

    void Start()
    {
        Debug.Log("🟢 Avvio PredictionHistoryViewer.Start()");
        LoadAllPredictionFiles();
        SetupUI();
    }

    private void LoadAllPredictionFiles()
    {
        Debug.Log("📂 Inizio caricamento predizioni dai file JSON");

        predictions.Clear();

        string storicoDir = System.IO.Path.Combine(
            System.IO.Directory.GetParent(Application.dataPath).Parent.FullName,
            "storico"
        );
        Debug.Log($"📁 Cartella storico: {storicoDir}");

        if (!System.IO.Directory.Exists(storicoDir))
        {
            Debug.LogWarning($"❌ Cartella storico non trovata: {storicoDir}");
            return;
        }

        string[] files = System.IO.Directory.GetFiles(storicoDir, "*.json");
        Debug.Log($"🔎 Trovati {files.Length} file .json");

        foreach (string filePath in files)
        {
            try
            {
                Debug.Log($"➡️ Leggo file: {filePath}");
                string json = System.IO.File.ReadAllText(filePath);
                SinglePrediction pred = JsonUtility.FromJson<SinglePrediction>(json);

                if (pred != null)
                {
                    predictions.Add(pred);
                    Debug.Log($"✅ Aggiunta predizione: {pred.predizione} ({pred.gamba}) @ {pred.timestamp} - Angolo: {pred.angolo}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Predizione nulla nel file {filePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Errore leggendo file {filePath}: {ex.Message}");
            }
        }

        Debug.Log($"🏁 Completato caricamento predizioni: totali {predictions.Count} predizioni caricate");
        UpdateUI();
    }

    private void SetupUI()
    {
        Debug.Log("🛠️ Setup dei listener per i bottoni movimento e ordinamento");

        btnSquat.onClick.AddListener(() =>
        {
            Debug.Log("🟡 Bottone Squat premuto");
            currentMovement = "squat";
            UpdateUI();
        });

        btnFlessioneAvanti.onClick.AddListener(() =>
        {
            Debug.Log("🟡 Bottone Flessione Avanti premuto");
            currentMovement = "flessione_avanti";
            UpdateUI();
        });

        btnFlessioneIndietro.onClick.AddListener(() =>
        {
            Debug.Log("🟡 Bottone Flessione Indietro premuto");
            currentMovement = "flessione_indietro";
            UpdateUI();
        });

        btnEstensione.onClick.AddListener(() =>
        {
            Debug.Log("🟡 Bottone Estensione premuto");
            currentMovement = "estensione";
            UpdateUI();
        });

        btnToggleOrder.onClick.AddListener(() =>
        {
            ascendingOrder = !ascendingOrder;
            Debug.Log($"🔀 Bottone Toggle Ordinamento premuto: ascendingOrder={ascendingOrder}");
            UpdateUI();
        });
    }

    private void UpdateUI()
    {
        Debug.Log($"🔄 UpdateUI chiamata - Movimento selezionato: {currentMovement}, Ordinamento: {(ascendingOrder ? "Crescente" : "Decrescente")}");

        if (btnToggleOrder == null ||
            scrollViewContent == null ||
            predictionItemPrefab == null)
        {
            Debug.LogError("❌ Mancano riferimenti UI! Controlla nell'Inspector:");
            Debug.LogError($"btnToggleOrder: {btnToggleOrder != null}");
            Debug.LogError($"scrollViewContent: {scrollViewContent != null}");
            Debug.LogError($"predictionItemPrefab: {predictionItemPrefab != null}");
            return;
        }

        ResetButtonColors();
    
        // Evidenzia il bottone attivo
        switch(currentMovement)
        {
            case "squat":
                SetActiveButton(btnSquat);
                break;
            case "flessione_avanti":
                SetActiveButton(btnFlessioneAvanti);
                break;
            case "flessione_indietro":
                SetActiveButton(btnFlessioneIndietro);
                break;
            case "estensione":
                SetActiveButton(btnEstensione);
                break;
        }

        Text toggleText = btnToggleOrder.GetComponentInChildren<Text>();
        if (toggleText != null)
        {
            toggleText.text = ascendingOrder ? "↑ Crescente" : "↓ Decrescente";
        }
        else
        {
            Debug.LogError("❌ Text component mancante in btnToggleOrder!");
        }

        var filtered = predictions
            .Where(p => p.predizione == currentMovement)
            .OrderBy(p => DateTime.Parse(p.timestamp));

        if (!ascendingOrder)
        {
            filtered = filtered.OrderByDescending(p => DateTime.Parse(p.timestamp));
        }

        var filteredList = filtered.ToList();
        Debug.Log($"📊 Trovate {filteredList.Count} predizioni filtrate per {currentMovement}");
        UpdateScrollView(filteredList);
    }

   [Header("UI Settings")]
    public int contentFontSize = 18; 

    private void UpdateScrollView(List<SinglePrediction> predictionsToShow)
    {
        Debug.Log("📜 Aggiornamento della scroll view con i dati filtrati");

        foreach (Transform child in scrollViewContent.transform)
        {
            Destroy(child.gameObject);
        }

        if (predictionsToShow.Count == 0)
        {
            GameObject emptyMessage = new GameObject("EmptyMessage");
            emptyMessage.transform.SetParent(scrollViewContent.transform);
            Text textComponent = emptyMessage.AddComponent<Text>();
            textComponent.text = "Nessun dato disponibile";
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = contentFontSize; 
            textComponent.color = Color.gray;
            textComponent.alignment = TextAnchor.MiddleCenter;
            return;
        }

        foreach (var pred in predictionsToShow)
        {
            Debug.Log($"📌 Creo elemento UI per predizione @ {pred.timestamp}: {pred.predizione} - Angolo {pred.angolo}");

            GameObject newItem = Instantiate(predictionItemPrefab, scrollViewContent.transform);

            Text[] texts = newItem.GetComponentsInChildren<Text>();

            if (texts.Length >= 4)
            {
                // Applica il font size configurabile a tutti i testi
                foreach (Text text in texts)
                {
                    text.fontSize = contentFontSize;
                }
                
                texts[0].text = FormatTimestamp(pred.timestamp);
                texts[1].text = $"{FormatMovementName(pred.predizione)} ({pred.gamba.ToUpper()})";
                texts[2].text = $"Angle: {pred.angolo:F1}°";
                texts[3].text = $"Feedback: {pred.feedback}";
            }
            else
            {
                Debug.LogError("❌ Il prefab dell'elemento della lista non ha almeno 4 componenti Text!");
            }
        }

        Canvas.ForceUpdateCanvases();
        Debug.Log("✅ Scroll view aggiornata");
    }
    private string FormatTimestamp(string originalTimestamp)
    {
        try
        {
            DateTime dt = DateTime.Parse(originalTimestamp);
            return dt.ToString("dd/MM/yyyy HH:mm:ss");
        }
        catch
        {
            Debug.LogWarning($"⚠️ Timestamp non valido: {originalTimestamp}");
            return originalTimestamp;
        }
    }

    private string FormatMovementName(string movement)
    {
        switch (movement)
        {
            case "squat": return "Squat";
            case "flessione_avanti": return "Standing hip flexion";
            case "flessione_indietro": return "Backward knee flexion";
            case "estensione": return "Seated leg extension";
            default: return movement;
        }
    }
    
    
private void ResetButtonColors()
{
    ColorBlock normalColors = new ColorBlock
    {
        normalColor = Color.white,
        highlightedColor = new Color(0.9f, 0.9f, 0.9f),
        pressedColor = new Color(0.8f, 0.8f, 0.8f),
        disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
        colorMultiplier = 1,
        fadeDuration = 0.1f
    };

    btnSquat.colors = normalColors;
    btnFlessioneAvanti.colors = normalColors;
    btnFlessioneIndietro.colors = normalColors;
    btnEstensione.colors = normalColors;
}

private void SetActiveButton(Button activeButton)
{
    ColorBlock activeColors = new ColorBlock
    {
        normalColor = new Color(0.7f, 0.9f, 1f), // Azzurro chiaro
        highlightedColor = new Color(0.6f, 0.8f, 0.9f),
        pressedColor = new Color(0.5f, 0.7f, 0.8f),
        disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
        colorMultiplier = 1,
        fadeDuration = 0.1f
    };

    activeButton.colors = activeColors;
}
}
