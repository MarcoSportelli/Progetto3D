// /Application/RecordPose.cs
using UnityEngine;
using System.Diagnostics;
using System.Collections;
using System.IO;

public class RecordPose : MonoBehaviour
{
    public bool isLeftLeg = true;  // flag da UI utente
    private bool recording = false;

    private string bagFilePath;
    private string npyFilePath;
    private string pythonScriptPath;

    void Start()
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        string dataPath = Path.Combine(projectPath, "data");
        string landmarkPath = Path.Combine(projectPath, "landmark");
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Es: data/20250603_150015.bag
        bagFilePath = Path.Combine(dataPath, $"{timestamp}.bag");
        npyFilePath = Path.Combine(dataPath, $"{timestamp}.npy");
        pythonScriptPath = Path.Combine(landmarkPath, "extract_landmarks.py");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !recording)
        {
            StartCoroutine(RecordAndExtract());
        }
    }

    IEnumerator RecordAndExtract()
    {
        recording = true;
        UnityEngine.Debug.Log("🎥 Avvio registrazione Realsense...");

        yield return StartCoroutine(RecordRealsenseBag());

        UnityEngine.Debug.Log("✅ Registrazione completata.");
        UnityEngine.Debug.Log("📤 Lancio script Python per landmark...");

        RunPythonScript(bagFilePath, npyFilePath, isLeftLeg ? "left" : "right");

        recording = false;
    }

    IEnumerator RecordRealsenseBag()
    {
        // Simulazione: qui dovresti usare `rs-record.exe` o un plugin Unity RealSense
        // Esempio se hai rs-record.exe installato:
        string rsRecordPath = @"C:\Program Files\Intel RealSense SDK 2.0\tools\rs-record.exe";  // cambia path
        string arguments = $"-o \"{bagFilePath}\"";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = rsRecordPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        Process p = Process.Start(psi);
        UnityEngine.Debug.Log("Registrazione Realsense in corso...");
        yield return new WaitForSeconds(5); // oppure attendi fine del processo p.WaitForExit()
        p.Kill();
    }

    void RunPythonScript(string bagPath, string npyPath, string legSide)
    {
        string pythonPath = "python"; // oppure path assoluto a python

        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{pythonScriptPath}\" \"{bagPath}\" \"{npyPath}\" {legSide}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Process process = new Process();
        process.StartInfo = start;

        process.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log(args.Data);
        process.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError(args.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        UnityEngine.Debug.Log($"✅ Landmark extraction completata. Salvato in {npyPath}");
    }
}
