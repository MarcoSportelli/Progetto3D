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
    [SerializeField] public Toggle leftLegToggle;

    [Header("External Tools Paths")]
    [SerializeField] private string pythonServerExe = @"C:\Users\markd\anaconda3\python.exe";
    private bool isProcessing = false;
    public string currentRecordingTimestamp;
    public string dataPath;
    private string dataPathnpy;
    private string pythonExtractPath;
    private string pythonExtractExe;
    private string serverScriptPath;
    public event Action OnRecordingFinished;
    public PredizioneReader predizioneReader; 

    [Header("UI Loading Spinner")]
    [SerializeField] private GameObject loadingSpinner;

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
        string appFolderPath = Directory.GetParent(Application.dataPath).FullName; // App/
        string projectRootPath = Directory.GetParent(appFolderPath).FullName;     // cartella che contiene "App" e "landmark_extraction" e "PoseDetector"

        dataPath = Path.Combine(appFolderPath, "data");  // resta dentro App/data
        dataPathnpy = Path.Combine(projectRootPath, "incoming_data"); 

        string landmarkPath = Path.Combine(projectRootPath, "landmark_extraction");
        string poseDetectorPath = Path.Combine(projectRootPath, "PoseDetector");

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

    public void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        UnityEngine.Debug.Log(message);
    }
/*
    public IEnumerator RecordOnly()
    {
        if (isProcessing) yield break;

        isLeftLeg = leftLegToggle.isOn;
        isProcessing = true;
        currentRecordingTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss") + (isLeftLeg ? "_left" : "_right");
        string videoFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.mp4");
        
        WebcamDisplay webcamDisplay = FindObjectOfType<WebcamDisplay>();
        WebCamTexture cam = null;

        if (webcamDisplay != null)
        {
            var webcamField = typeof(WebcamDisplay).GetField("webcamTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cam = (WebCamTexture)webcamField?.GetValue(webcamDisplay);
            if (cam != null && cam.isPlaying)
            {
                cam.Stop();
                UnityEngine.Debug.Log("[RecordOnly] Webcam Unity fermata temporaneamente per FFmpeg.");
            }
        }

        
        UpdateStatus("Registrazione webcam in corso...");
        UnityEngine.Debug.Log($"[RecordOnly] Inizio registrazione webcam - Salvataggio in: {videoFilePath}");

        string ffmpegPath = Path.Combine(Application.dataPath, "..", "ffmpeg", "bin", "ffmpeg.exe");
        ffmpegPath = Path.GetFullPath(ffmpegPath);

        string ffmpegArgs = $"-f dshow -i video=\"HP Wide Vision HD Camera\" -t {recordingDuration} -vcodec libx264 -pix_fmt yuv420p \"{videoFilePath}\"";
        UnityEngine.Debug.Log("Data path: " + dataPath + " | Exists? " + Directory.Exists(dataPath));
        UnityEngine.Debug.Log("Video path: " + videoFilePath);
        UnityEngine.Debug.Log("FFmpeg path: " + ffmpegPath + " | Exists? " + File.Exists(ffmpegPath));
        UnityEngine.Debug.Log($"FFmpeg Args: {ffmpegArgs}");
        UnityEngine.Debug.Log($"[FFmpeg CMD] \"{ffmpegPath}\" {ffmpegArgs}");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = ffmpegArgs,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (recordingProcess = new Process { StartInfo = psi })
        {
            recordingProcess.OutputDataReceived += (s, e) => UnityEngine.Debug.Log($"[FFmpeg STDOUT] {e.Data}");
            recordingProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    if (args.Data.ToLower().Contains("error"))
                        UnityEngine.Debug.LogError($"[FFmpeg STDERR] {args.Data}");
                    else
                        UnityEngine.Debug.Log($"[FFmpeg STDERR] {args.Data}");
                }
            };

            recordingProcess.Start();
            recordingProcess.BeginOutputReadLine();
            recordingProcess.BeginErrorReadLine();

            float elapsed = 0f;
            while (!recordingProcess.HasExited && elapsed < recordingDuration + 2f)
            {
                     yield return null;
                elapsed += Time.deltaTime;
            }
           
            if (!recordingProcess.HasExited)
            {
                recordingProcess.Kill();
                UnityEngine.Debug.LogWarning("[RecordOnly] Registrazione interrotta per timeout.");
            }

            UpdateStatus("Registrazione completata.");
        }
        
        // ✅ Step 2: Riattiva la webcam Unity
        if (cam != null && !cam.isPlaying)
        {
            cam.Play();
            UnityEngine.Debug.Log("[RecordOnly] Webcam Unity riavviata.");
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

*/
    public IEnumerator ExtractAndInfer()
    {
        if (loadingSpinner != null)
            loadingSpinner.SetActive(true);

        if (leftLegToggle != null)
            leftLegToggle.gameObject.SetActive(false);

        yield return StartCoroutine(RunLandmarkExtraction());
        yield return StartCoroutine(RunServerInference());
        if (loadingSpinner != null)
            loadingSpinner.SetActive(false);

        LeggiPredizioneEAggiornaUI();
        if (predizioneReader != null)
            predizioneReader.LeggiEAvvia();
        // Elimina il file .bag dopo estrazione e inferenza
        string videoFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.bag");
        if (File.Exists(videoFilePath))
        {
            try
            {
                File.Delete(videoFilePath);
                UnityEngine.Debug.Log($"🗑️ File .bag cancellato: {videoFilePath}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"❌ Errore nella cancellazione del file .bag: {e.Message}");
            }
        }
    }
    IEnumerator RunLandmarkExtraction()
    {
        UpdateStatus("Estrazione landmark...");
        string videoFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.bag");
        string npyFilePath = Path.Combine(dataPathnpy, $"{currentRecordingTimestamp}.npy");
        string legSide = isLeftLeg ? "left" : "right";

        UnityEngine.Debug.Log($"[RunLandmarkExtraction] Avvio estrazione: {videoFilePath} -> {npyFilePath} (lato: {legSide})");

        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = pythonExtractExe,
            Arguments = $"\"{pythonExtractPath}\" \"{videoFilePath}\" \"{npyFilePath}\" {legSide}",
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
    private void LeggiPredizioneEAggiornaUI()
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
                    UpdateStatus($"Predizione: {pred.predizione} | Angolo: {pred.angolo:F1}°");
                }
                catch (Exception e)
                {
                    UpdateStatus("Errore lettura predizione: " + e.Message);
                }
            }
            
        }
    }

    IEnumerator RunServerInference()
    {
        UpdateStatus("Inferenza in corso...");
        string npyFilePath = Path.Combine(dataPathnpy, $"{currentRecordingTimestamp}.npy");

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
        start.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";


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
