using UnityEngine;
using System.IO;
using System;

public class PredizioneReader : MonoBehaviour
{
    public ControlloAnimazioniGambe controlloAnimazioni;
    private string lastJson = "";


    void Update()
    {
        Debug.Log("=== INIZIO UPDATE ===");

        // Verifica riferimento a controlloAnimazioni
        if (controlloAnimazioni == null)
        {
            Debug.LogError("❌ ControlloAnimazioni non assegnato nell'Inspector!");
            return;
        }
        else
        {
            Debug.Log("✅ ControlloAnimazioni assegnato correttamente");
        }

        string appDir = Application.dataPath;
        string projectDir = Directory.GetParent(appDir).Parent.FullName;
        string predictionDir = Path.Combine(projectDir, "prediction");
        string path = Path.Combine(predictionDir, "prediction.json");
        Debug.Log($"Percorso del file: {Path.GetFullPath(path)}");

        if (File.Exists(path))
        {
            Debug.Log("📄 File JSON trovato");
            string json = File.ReadAllText(path);
            Debug.Log($"Contenuto letto: {json}");

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("⚠️ File JSON vuoto o contenuto non valido");
                return;
            }

            if (json != lastJson)
            {
                Debug.Log("🔄 Nuovo JSON rilevato");
                lastJson = json;

                try
                {
                    Predizione pred = JsonUtility.FromJson<Predizione>(json);
                    Debug.Log($"📊 Dati parsati - Predizione: {pred.predizione}, Gamba: {pred.gamba}, Angolo: {pred.angolo}");

                    // Verifica valori prima di avviare l'animazione
                    if (string.IsNullOrEmpty(pred.predizione) || string.IsNullOrEmpty(pred.gamba))
                    {
                        Debug.LogError("❌ Dati JSON incompleti (mancano predizione o gamba)");
                        return;
                    }

                    Debug.Log($"🎬 Tentativo di avviare animazione: {pred.predizione} per gamba {pred.gamba}");
                    controlloAnimazioni.AvviaAnimazione(pred.gamba, pred.predizione);
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Errore durante il parsing del JSON: {e.Message}");
                }
            }
            else
            {
                Debug.Log("🔄 Nessuna modifica nel file JSON rispetto all'ultima lettura");
            }
        }
        else
        {
            Debug.LogError($"❌ File non trovato: {path}");
        }

        Debug.Log("=== FINE UPDATE ===");
    }

    void LeggiEAvvia()
    {
        Debug.Log("=== INIZIO LEGGIAVVIA ===");
        string appDir = Application.dataPath;
        string projectDir = Directory.GetParent(appDir).Parent.FullName;
        string predictionDir = Path.Combine(projectDir, "prediction");
        string path = Path.Combine(predictionDir, "prediction.json");
        Debug.Log($"Percorso del file (LeggiEAvvia): {Path.GetFullPath(path)}");

        if (File.Exists(path))
        {
            Debug.Log("📄 File JSON trovato (LeggiEAvvia)");
            string json = File.ReadAllText(path);
            Debug.Log($"Contenuto letto (LeggiEAvvia): {json}");

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    Predizione pred = JsonUtility.FromJson<Predizione>(json);
                    Debug.Log($"📊 Dati parsati (LeggiEAvvia) - Predizione: {pred.predizione}, Gamba: {pred.gamba}");

                    if (controlloAnimazioni != null)
                    {
                        Debug.Log($"🎬 Chiamata ad AvviaAnimazione (LeggiEAvvia)");
                        controlloAnimazioni.AvviaAnimazione(pred.gamba, pred.predizione);
                    }
                    else
                    {
                        Debug.LogError("❌ ControlloAnimazioni è null in LeggiEAvvia!");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Errore parsing JSON (LeggiEAvvia): {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ File JSON vuoto (LeggiEAvvia)");
            }
        }
        else
        {
            Debug.LogError($"❌ File non trovato (LeggiEAvvia): {path}");
        }
        Debug.Log("=== FINE LEGGIAVVIA ===");
    }
}

[Serializable]
public class Predizione
{
    public string predizione;
    public float angolo;
    public string gamba;
}