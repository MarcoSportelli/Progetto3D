using UnityEngine;
using System.IO;
using System;

public class PredizioneReader : MonoBehaviour
{
    public ControlloAnimazioniGambe controlloAnimazioni;
    public RecordPose recordPose; // Assegna dall'Inspector!
    private string lastJson = "";

    // Dizionario per traduzione in inglese
    private static readonly System.Collections.Generic.Dictionary<string, string> CLASS_MAP = new System.Collections.Generic.Dictionary<string, string>
    {
        {"flessione_indietro", "Backward knee flexion"},
        {"flessione_avanti", "Standing hip flexion"},
        {"estensione_gamba", "Seated leg extension"},
        {"squat", "Squat"}
    };

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

                    // Traduci il nome della classe in inglese per il salvataggio
                    string predizioneEn = CLASS_MAP.ContainsKey(pred.predizione) ? CLASS_MAP[pred.predizione] : pred.predizione;

                    Predizione predConTime = new Predizione
                    {
                        predizione = pred.predizione,
                        angolo = pred.angolo,
                        gamba = pred.gamba,
                        timestamp = timestamp,
                        feedback = pred.feedback
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
                        // Aggiorna lo status in RecordPose SOLO con nome inglese
                        if (recordPose != null)
                        {
                            string predEn = CLASS_MAP.ContainsKey(pred.predizione) ? CLASS_MAP[pred.predizione] : pred.predizione;
                            recordPose.UpdateStatus($"Movement: {predEn} \n Angle: {pred.angolo:F1}° \n {pred.feedback}");
                        }
                    }
                }
                catch (Exception e)
                {
                    if (recordPose != null)
                        recordPose.UpdateStatus("Prediction read error: " + e.Message);
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
    public string feedback;
    public string timestamp;
}