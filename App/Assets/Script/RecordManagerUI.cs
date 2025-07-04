using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RecordManagerUI : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button buttonAvvia;
    public Button buttonSalva;
    public Button buttonRipeti;
    public Button buttonNuovaRegistrazione; 
    public GameObject viewportModello3D;

    [Header("UI Canvas")]
    public GameObject canvasCamera;

    [Header("Controllo Registrazione")]
    public RecordPose recordPose;

    [Header("RealSense Device")]
    public RsDevice rsDevice;

    [SerializeField] private GameObject loadingSpinner;

    [Header("Timer UI")]
    public Text preRecordTimerText;
    public Text recordTimerText;
    void Start()
    {
        // Controlla se siamo nella scena corretta
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene != "Registrazione")
        {
            Debug.Log($"RecordManagerUI non necessario nella scena {currentScene}");
            gameObject.SetActive(false);
            return;
        }

        // Controlli di sicurezza per tutti i componenti
        if (!ValidateComponents())
        {
            Debug.LogError("RecordManagerUI: Alcuni componenti non sono assegnati!");
            return;
        }


        // Setup iniziale dei bottoni
        SetupInitialState();
        
        // Assegna i listener ai bottoni
        SetupButtonListeners();
    }

    bool ValidateComponents()
    {
        bool isValid = true;

        if (buttonAvvia == null)
        {
            Debug.LogWarning("RecordManagerUI: buttonAvvia non assegnato!");
            isValid = false;
        }

        if (buttonSalva == null)
        {
            Debug.LogWarning("RecordManagerUI: buttonSalva non assegnato!");
            isValid = false;
        }

        if (buttonRipeti == null)
        {
            Debug.LogWarning("RecordManagerUI: buttonRipeti non assegnato!");
            isValid = false;
        }

        if (buttonNuovaRegistrazione == null)
        {
            Debug.LogWarning("RecordManagerUI: buttonNuovaRegistrazione non assegnato!");
            isValid = false;
        }

        if (viewportModello3D == null)
        {
            Debug.LogWarning("RecordManagerUI: viewportModello3D non assegnato!");
            isValid = false;
        }

        if (recordPose == null)
        {
            Debug.LogWarning("RecordManagerUI: recordPose non assegnato!");
            isValid = false;
        }

        if (rsDevice == null)
        {
            Debug.LogWarning("RecordManagerUI: rsDevice non assegnato!");
            isValid = false;
        }

        return isValid;
    }

    void SetupInitialState()
    {
        if (buttonAvvia != null) buttonAvvia.gameObject.SetActive(true);
        if (buttonSalva != null) buttonSalva.gameObject.SetActive(false);
        if (buttonRipeti != null) buttonRipeti.gameObject.SetActive(false);
        if (buttonNuovaRegistrazione != null) buttonNuovaRegistrazione.gameObject.SetActive(false);
        if (viewportModello3D != null) viewportModello3D.SetActive(false);

        if (loadingSpinner != null)
            loadingSpinner.SetActive(false);

        if (recordPose != null)
            recordPose.leftLegToggle.gameObject.SetActive(true);

        if (canvasCamera != null)
            canvasCamera.SetActive(true);

        // Nascondi i timer all'avvio
        if (preRecordTimerText != null)
            preRecordTimerText.gameObject.SetActive(false);
        if (recordTimerText != null)
            recordTimerText.gameObject.SetActive(false);
    }

    void SetupButtonListeners()
    {
        if (buttonAvvia != null) buttonAvvia.onClick.AddListener(AvviaRegistrazione);
        if (buttonSalva != null) buttonSalva.onClick.AddListener(SalvaRegistrazione);
        if (buttonRipeti != null) buttonRipeti.onClick.AddListener(RipetiRegistrazione);
        if (buttonNuovaRegistrazione != null) buttonNuovaRegistrazione.onClick.AddListener(ResetUI);
    }
    void AvviaRegistrazione()
    {
        StartCoroutine(AvviaRegistrazioneCoroutine());
    }

    IEnumerator AvviaRegistrazioneCoroutine()
    {
        // Mostra pre-record timer
        if (preRecordTimerText != null)
            preRecordTimerText.gameObject.SetActive(true);
        if (recordTimerText != null)
            recordTimerText.gameObject.SetActive(false);

        // Countdown di 3 secondi
        float preRecordTime = 3f;
        float t = preRecordTime;
        while (t > 0)
        {
            if (preRecordTimerText != null)
                preRecordTimerText.text = $"Inizio tra {Mathf.CeilToInt(t)}...";
            yield return new WaitForSeconds(1f);
            t -= 1f;
        }
        if (preRecordTimerText != null)
        {
            preRecordTimerText.text = "Via!";
            yield return new WaitForSeconds(0.5f);
            preRecordTimerText.gameObject.SetActive(false);
        }

         // Avvia la registrazione vera e propria
        if (rsDevice != null && recordPose != null)
        {
            recordPose.isLeftLeg = recordPose.leftLegToggle != null ? recordPose.leftLegToggle.isOn : true;
            recordPose.currentRecordingTimestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + (recordPose.isLeftLeg ? "_left" : "_right");
            string videoFilePath = System.IO.Path.Combine(recordPose.dataPath, $"{recordPose.currentRecordingTimestamp}.bag");

            rsDevice.DeviceConfiguration.RecordPath = videoFilePath;
            rsDevice.DeviceConfiguration.mode = RsConfiguration.Mode.Record;
            rsDevice.RestartPipeline();
            Debug.Log("[RecordManagerUI] Modalità RsDevice: " + rsDevice.DeviceConfiguration.mode + " | Path: " + videoFilePath);
        }

        if (canvasCamera != null)
            canvasCamera.SetActive(true);

        ToggleButtons(avvia: false, stop: false, salva: false, ripeti: false, modello: false, nuova: false);

        // Mostra record timer
        if (recordTimerText != null)
        {
            recordTimerText.gameObject.SetActive(true);
            float recordTime = 7f;
            float elapsed = 0f;
            while (elapsed < recordTime)
            {
                recordTimerText.text = $"Registrazione: {Mathf.CeilToInt(recordTime - elapsed)}s";
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }
            recordTimerText.gameObject.SetActive(false);
        }

        // Stoppa la registrazione automaticamente dopo 5 secondi
        StoppaRegistrazione();
    }
    void StoppaRegistrazione()
    {
        if (rsDevice != null)
        {
            rsDevice.DeviceConfiguration.mode = RsConfiguration.Mode.Live;
            rsDevice.RestartPipeline(); 
            Debug.Log("[RecordManagerUI] Modalità RsDevice: " + rsDevice.DeviceConfiguration.mode);
        }
        
        if (canvasCamera != null)
            canvasCamera.SetActive(true);

        ToggleButtons(avvia: false, stop: false, salva: true, ripeti: true, modello: false, nuova: false);
    }

    void SalvaRegistrazione()
    {
        StartCoroutine(SalvaRegistrazioneCoroutine());
        // canvasCamera disattivo quando modello attivo
        if (canvasCamera != null)
            canvasCamera.SetActive(false);

        ToggleButtons(avvia: false, stop: false, salva: false, ripeti: false, modello: true, nuova: false);
    }

    IEnumerator SalvaRegistrazioneCoroutine()
    {
        if (recordPose != null)
        {
            yield return StartCoroutine(recordPose.ExtractAndInfer());
        }
        // canvasCamera resta disattivo finché modello attivo
        ToggleButtons(avvia: false, stop: false, salva: false, ripeti: false, modello: true, nuova: true);
    }

    void RipetiRegistrazione()
    {
        AvviaRegistrazione();
    }

    void ResetUI()
    {
        // Stato di default: solo Avvia attivo, modello nascosto, canvasCamera attivo
        if (canvasCamera != null)
            canvasCamera.SetActive(true);

        if (recordPose != null)
            recordPose.leftLegToggle.gameObject.SetActive(true);

        if (recordPose != null)
            recordPose.UpdateStatus("Pronto");

        ToggleButtons(avvia: true, stop: false, salva: false, ripeti: false, modello: false, nuova: false);
    }

    void ToggleButtons(bool avvia, bool stop, bool salva, bool ripeti, bool modello, bool nuova)
    {
        buttonAvvia.gameObject.SetActive(avvia);
        buttonSalva.gameObject.SetActive(salva);
        buttonRipeti.gameObject.SetActive(ripeti);
        viewportModello3D.SetActive(modello);
        buttonNuovaRegistrazione.gameObject.SetActive(nuova);
    }
}