using UnityEngine;
using System.IO;
using System;

public class PredizioneReader : MonoBehaviour
{
    public ControlloAnimazioniGambe controlloAnimazioni;
    public RecordPose recordPose; // Assegna dall'Inspector!
    private string lastJson = "";

    public void LeggiEAvvia()
    {
        if (controlloAnimazioni == null)
            return;

        string appDir = Application.dataPath;
        string projectDir = Directory.GetParent(appDir).Parent.FullName;
        string predictionDir = Path.Combine(projectDir, "prediction");
        string path = Path.Combine(predictionDir, "prediction.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return;

            if (json != lastJson)
            {
                lastJson = json;
                try
                {
                    Predizione pred = JsonUtility.FromJson<Predizione>(json);
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Predizione predConTime = new Predizione
                    {
                        predizione = pred.predizione,
                        angolo = pred.angolo,
                        gamba = pred.gamba,
                        timestamp = timestamp,
                        feedback = pred.feedback // Aggiungi il campo feedback
                    };

                    try
                    {
                        string storicoDir = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "storico");
                        Directory.CreateDirectory(storicoDir);
                        string fileName = $"prediction_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                        string storicoPath = Path.Combine(storicoDir, fileName);
                        string jsonWithTimestamp = JsonUtility.ToJson(predConTime, true);
                        File.WriteAllText(storicoPath, jsonWithTimestamp);

                        // Cancella prediction.json dopo averlo salvato nello storico
                        File.Delete(path);
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(pred.predizione) && !string.IsNullOrEmpty(pred.gamba))
                    {
                        controlloAnimazioni.AvviaAnimazione(pred.gamba, pred.predizione);
                        // Aggiorna lo status in RecordPose
                        if (recordPose != null)
                            recordPose.UpdateStatus($"Movimento: {pred.predizione} \n Angolo: {pred.angolo:F1}° \n Feedback: {pred.feedback}");
                    }
                }
                catch (Exception e)
                {
                    if (recordPose != null)
                        recordPose.UpdateStatus("Errore lettura predizione: " + e.Message);
                }
            }
        }
    }
}

[Serializable]
public class Predizione
{
    public string predizione;
    public float angolo;
    public string gamba;
    public string feedback; // <--- aggiungi questo campo
    public string timestamp; // <--- aggiungi questo campo
}