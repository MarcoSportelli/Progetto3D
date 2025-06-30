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
    [SerializeField] private string rsRecordPath = @"C:\Users\markd\Documents\Intel RealSense SDK 2.0\tools\rs-record.exe";
    [SerializeField] public Text timerText;
    private bool isProcessing = false;
    private string currentRecordingTimestamp;
    private string dataPath;
    private string dataPathnpy;
    private string pythonExtractPath;
    private string pythonExtractExe;
    private string serverScriptPath;
    private Process recordingProcess;
    public event Action OnRecordingFinished;
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


    public IEnumerator RecordRealSenseBag()
    {
        if (isProcessing) yield break;

        isLeftLeg = leftLegToggle.isOn;
        isProcessing = true;
        currentRecordingTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss") + (isLeftLeg ? "_left" : "_right");
        string bagFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.bag");

        UpdateStatus("Registrazione RealSense in corso...");
        UnityEngine.Debug.Log($"[RecordRealSenseBag] Inizio registrazione RealSense - Salvataggio in: {bagFilePath}");

        // Esempio: rs-record.exe -o output.bag
        string args = $"-o \"{bagFilePath}\"";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = rsRecordPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (recordingProcess = new Process { StartInfo = psi })
        {
            recordingProcess.OutputDataReceived += (s, e) => UnityEngine.Debug.Log($"[RealSense STDOUT] {e.Data}");
            recordingProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    if (args.Data.ToLower().Contains("error"))
                        UnityEngine.Debug.LogError($"[RealSense STDERR] {args.Data}");
                    else
                        UnityEngine.Debug.Log($"[RealSense STDERR] {args.Data}");
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
                UnityEngine.Debug.LogWarning("[RecordRealSenseBag] Registrazione interrotta per timeout.");
            }

            UpdateStatus("Registrazione RealSense completata.");
        }

        recordingProcess = null;
        isProcessing = false;
        if (OnRecordingFinished != null)
            OnRecordingFinished();
        UnityEngine.Debug.Log("[RecordRealSenseBag] Fine registrazione.");
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
                if (timerText != null)
                    timerText.text = $"Timer: {Mathf.Clamp(recordingDuration - elapsed, 0, recordingDuration):F1}s";
                yield return null;
                elapsed += Time.deltaTime;
            }
            if (timerText != null)
                timerText.text = "";


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


    public IEnumerator ExtractAndInfer()
    {
        yield return StartCoroutine(RunLandmarkExtraction());
        yield return StartCoroutine(RunServerInference());
        UpdateStatus("Analisi completata!");
    }
    IEnumerator RunLandmarkExtraction()
    {
        UpdateStatus("Estrazione landmark...");
        string videoFilePath = Path.Combine(dataPath, $"{currentRecordingTimestamp}.mp4");
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
