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
    private string pythonExtractPath;
    private string pythonExtractExe;
    private string pythonServerExe;
    private string serverScriptPath;

    void Start()
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        string dataPath = Path.Combine(projectPath, "data");
        string landmarkPath = Path.Combine(projectPath, "landmark_extraction");
        string poseDetectorPath = Path.Combine(projectPath, "PoseDetector");
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        bagFilePath = Path.Combine(dataPath, $"{timestamp}.bag");
        npyFilePath = Path.Combine(dataPath, $"{timestamp}.npy");
        pythonExtractPath = Path.Combine(landmarkPath, "extract_landmarks.py");
        pythonExtractExe = Path.Combine(landmarkPath, "mp_env", "Scripts", "python.exe");
        pythonServerExe = @"C:\Users\markd\anaconda3\python.exe";
        serverScriptPath = Path.Combine(poseDetectorPath, "server.py");
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

        RunPythonScript(pythonExtractExe, pythonExtractPath, bagFilePath, npyFilePath, isLeftLeg ? "left" : "right");

        UnityEngine.Debug.Log("📈 Lancio server di inferenza...");
        RunServerScript(pythonServerExe, serverScriptPath, npyFilePath);

        recording = false;
    }

    IEnumerator RecordRealsenseBag()
    {
        string rsRecordPath = @"C:\Program Files\Intel RealSense SDK 2.0\tools\rs-record.exe";  // cambia path se serve
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
        yield return new WaitForSeconds(5); // oppure attendi fine del processo con p.WaitForExit()
        if (!p.HasExited) p.Kill();
    }

    void RunPythonScript(string pythonExe, string scriptPath, string bagPath, string npyPath, string legSide)
    {
        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{scriptPath}\" \"{bagPath}\" \"{npyPath}\" {legSide}",
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

    void RunServerScript(string pythonExe, string scriptPath, string npyPath)
    {
        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{scriptPath}\" \"{npyPath}\"",
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

        UnityEngine.Debug.Log($"✅ Inference completata su {npyPath}");
    }
}