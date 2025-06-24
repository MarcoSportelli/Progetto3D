using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System;

public class RecordPose : MonoBehaviour
{
    [Header("Recording Settings")]
    public bool isLeftLeg = true;
    public float recordingDuration = 5f;

    [Header("UI References")]
    [SerializeField] private Text statusText;

    [Header("UI Toggles")]
    [SerializeField] private Toggle leftLegToggle;

    [Header("External Tools Paths")]
    [SerializeField] private string pythonServerExe = @"C:\Users\markd\anaconda3\python.exe";
    [SerializeField] private string rsRecordPath = @"C:\Program Files\Intel RealSense SDK 2.0\tools\rs-record.exe";

    private bool isProcessing = false;
    private string currentRecordingTimestamp;
    private string dataPath;
    private string pythonExtractPath;
    private string pythonExtractExe;
    private string serverScriptPath;
    private Process recordingProcess;
    void Start()
    {
        InitializePaths();

        if (leftLegToggle != null)
        {
            isLeftLeg = leftLegToggle.isOn;
            UnityEngine.Debug.Log($"[Start] Valore iniziale Toggle -> isLeftLeg: {isLeftLeg}");
        }

        UpdateStatus("Pronto");
    }


    void InitializePaths()
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        dataPath = Path.Combine(projectPath, "data");
        Directory.CreateDirectory(dataPath);

        string landmarkPath = Path.Combine(projectPath, "landmark_extraction");
        string poseDetectorPath = Path.Combine(projectPath, "PoseDetector");

        pythonExtractPath = Path.Combine(landmarkPath, "extract_landmarks.py");
        pythonExtractExe = Path.Combine(landmarkPath, "mp_env", "Scripts", "python.exe");
        serverScriptPath = Path.Combine(poseDetectorPath, "server.py");
    }

    public void ToggleLegSide()
    {
        if (isProcessing) return;

        isLeftLeg = !isLeftLeg;
        UnityEngine.Debug.Log($"[ToggleLegSide] Nuovo valore -> isLeftLeg: {isLeftLeg}");
    }



    void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        UnityEngine.Debug.Log(message);
    }

    public IEnumerator RecordOnly()
    {
        if (isProcessing) yield break;

        isLeftLeg = leftLegToggle.isOn;
        isProcessing = true;
        currentRecordingTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string bagFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.bag");

        UnityEngine.Debug.Log($"[RecordOnly] Inizio registrazione - Salvataggio in: {bagFilePath}");
        UpdateStatus("Registrazione in corso...");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = rsRecordPath,
            Arguments = $"-o \"{bagFilePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        recordingProcess = Process.Start(psi);
        yield return new WaitForSeconds(recordingDuration);

        if (!recordingProcess.HasExited)
        {
            recordingProcess.Kill();
            UnityEngine.Debug.Log("[RecordOnly] Registrazione completata (timeout).");
        }

        recordingProcess = null;
        isProcessing = false;
        UnityEngine.Debug.Log("[RecordOnly] Fine registrazione.");
    }

    public void ForceStopRecording()
    {
        if (recordingProcess != null && !recordingProcess.HasExited)
        {
            recordingProcess.Kill();
            UnityEngine.Debug.Log("[ForceStopRecording] Registrazione interrotta manualmente.");
            recordingProcess = null;
            isProcessing = false;
        }
    }


    public IEnumerator ExtractAndInfer()
    {
        yield return StartCoroutine(RunLandmarkExtraction());
        yield return StartCoroutine(RunServerInference());
        UpdateStatus("Analisi completata!");
    }

    IEnumerator RunLandmarkExtraction()
    {
        UpdateStatus("Estrazione landmark...");
        string bagFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.bag");
        string npyFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.npy");
        string legSide = isLeftLeg ? "left" : "right";

        UnityEngine.Debug.Log($"[RunLandmarkExtraction] Avvio estrazione: {bagFilePath} -> {npyFilePath} (lato: {legSide})");

        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = pythonExtractExe,
            Arguments = $"\"{pythonExtractPath}\" \"{bagFilePath}\" \"{npyFilePath}\" {legSide}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = start })
        {
            process.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log($"[Extractor STDOUT] {args.Data}");
            process.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError($"[Extractor STDERR] {args.Data}");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.HasExited)
                yield return null;

            UnityEngine.Debug.Log($"[RunLandmarkExtraction] Estrazione completata con codice: {process.ExitCode}");

            if (process.ExitCode != 0)
                UnityEngine.Debug.LogError($"[RunLandmarkExtraction] Errore durante l'estrazione (codice {process.ExitCode})");
        }
    }
    IEnumerator RunServerInference()
    {
        UpdateStatus("Inferenza in corso...");
        string npyFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.npy");

        UnityEngine.Debug.Log($"[RunServerInference] Avvio inferenza su: {npyFilePath}");

        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = pythonServerExe,
            Arguments = $"\"{serverScriptPath}\" \"{npyFilePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = start })
        {
            process.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log($"[Inference STDOUT] {args.Data}");
            process.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError($"[Inference STDERR] {args.Data}");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.HasExited)
                yield return null;

            UnityEngine.Debug.Log($"[RunServerInference] Inferenza completata con codice: {process.ExitCode}");

            if (process.ExitCode != 0)
                UnityEngine.Debug.LogError($"[RunServerInference] Errore durante l'inferenza (codice {process.ExitCode})");
        }
    }

}
