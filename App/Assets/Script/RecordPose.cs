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
    [Tooltip("Duration in seconds for recording")]
    public float recordingDuration = 5f;

    [Header("UI References")]
    [SerializeField] private Button recordButton;
    [SerializeField] private Button toggleLegButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text legSideText;

    [Header("External Tools Paths")]
    [SerializeField] private string pythonServerExe = @"C:\Users\markd\anaconda3\python.exe";
    [SerializeField] private string rsRecordPath = @"C:\Program Files\Intel RealSense SDK 2.0\tools\rs-record.exe";

    private bool isProcessing = false;
    private string currentRecordingTimestamp;
    private string dataPath;
    private string pythonExtractPath;
    private string pythonExtractExe;
    private string serverScriptPath;

    void Start()
    {
        InitializePaths();
        SetupUI();
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

    void SetupUI()
    {
        if (recordButton == null || toggleLegButton == null || statusText == null || legSideText == null)
        {
            UnityEngine.Debug.LogError("UI references not assigned in inspector!");
            return;
        }

        recordButton.onClick.AddListener(StartRecording);
        toggleLegButton.onClick.AddListener(ToggleLegSide);
        UpdateLegDisplay();
        UpdateStatus("Pronto");
    }

    public void ToggleLegSide()
    {
        if (isProcessing) return;
        
        isLeftLeg = !isLeftLeg;
        UpdateLegDisplay();
    }

    void UpdateLegDisplay()
    {
        legSideText.text = isLeftLeg ? "Lato: Sinistro" : "Lato: Destro";
    }

    void UpdateStatus(string message)
    {
        statusText.text = message;
        UnityEngine.Debug.Log(message);
    }

    public void StartRecording()
    {
        if (isProcessing) return;
        
        currentRecordingTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        StartCoroutine(FullProcessingPipeline());
    }

    IEnumerator FullProcessingPipeline()
    {
        isProcessing = true;
        recordButton.interactable = false;
        toggleLegButton.interactable = false;

        // 1. Recording
        UpdateStatus("Registrazione in corso...");
        yield return StartCoroutine(RecordRealsenseData());
        
        // 2. Landmark Extraction
        UpdateStatus("Estrazione landmark...");
        yield return StartCoroutine(RunLandmarkExtraction());
        
        // 3. Server Inference
        UpdateStatus("Analisi in corso...");
        yield return StartCoroutine(RunServerInference());
        
        // Clean up
        isProcessing = false;
        recordButton.interactable = true;
        toggleLegButton.interactable = true;
        UpdateStatus("Completato!");
        yield return new WaitForSeconds(2);
        UpdateStatus("Pronto");
    }

    IEnumerator RecordRealsenseData()
    {
        string bagFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.bag");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = rsRecordPath,
            Arguments = $"-o \"{bagFilePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using (Process p = Process.Start(psi))
        {
            yield return new WaitForSeconds(recordingDuration);
            
            if (!p.HasExited)
            {
                p.Kill();
                UnityEngine.Debug.Log("Registrazione interrotta dopo il timeout");
            }
        }
    }

    IEnumerator RunLandmarkExtraction()
    {
        string bagFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.bag");
        string npyFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.npy");
        string legSide = isLeftLeg ? "left" : "right";

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
            process.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log(args.Data);
            process.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError(args.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.HasExited)
            {
                yield return null;
            }

            if (process.ExitCode != 0)
            {
                UnityEngine.Debug.LogError($"Errore durante l'estrazione (codice {process.ExitCode})");
            }
        }
    }

    IEnumerator RunServerInference()
    {
        string npyFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.npy");

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
            process.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log(args.Data);
            process.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError(args.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.HasExited)
            {
                yield return null;
            }

            if (process.ExitCode != 0)
            {
                UnityEngine.Debug.LogError($"Errore durante l'inferenza (codice {process.ExitCode})");
            }
        }
    }
}