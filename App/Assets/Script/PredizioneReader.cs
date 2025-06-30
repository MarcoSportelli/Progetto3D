using UnityEngine;
using System.IO;
using System;

public class PredizioneReader : MonoBehaviour
{
    public ControlloAnimazioniGambe controlloAnimazioni;
    private string lastJson = "";

    void Update()
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
                        timestamp = timestamp
                    };

                    try
                    {
                        string storicoDir = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "storico");
                        Directory.CreateDirectory(storicoDir);
                        string fileName = $"prediction_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                        string storicoPath = Path.Combine(storicoDir, fileName);
                        string jsonWithTimestamp = JsonUtility.ToJson(predConTime, true);
                        File.WriteAllText(storicoPath, jsonWithTimestamp);
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(pred.predizione) && !string.IsNullOrEmpty(pred.gamba))
                        controlloAnimazioni.AvviaAnimazione(pred.gamba, pred.predizione);
                }
                catch { }
            }
        }
    }

    public void LeggiEAvvia()
    {
        string appDir = Application.dataPath;
        string projectDir = Directory.GetParent(appDir).Parent.FullName;
        string predictionDir = Path.Combine(projectDir, "prediction");
        string path = Path.Combine(predictionDir, "prediction.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    Predizione pred = JsonUtility.FromJson<Predizione>(json);
                    if (controlloAnimazioni != null)
                        controlloAnimazioni.AvviaAnimazione(pred.gamba, pred.predizione);
                }
                catch { }
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
    public string timestamp;
}