using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RecordManagerUI : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button buttonAvvia;
    public Button buttonStop;
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

        if (buttonStop == null)
        {
            Debug.LogWarning("RecordManagerUI: buttonStop non assegnato!");
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
        if (buttonStop != null) buttonStop.gameObject.SetActive(false);
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
    }

    void SetupButtonListeners()
    {
        if (buttonAvvia != null) buttonAvvia.onClick.AddListener(AvviaRegistrazione);
        if (buttonStop != null) buttonStop.onClick.AddListener(StoppaRegistrazione);
        if (buttonSalva != null) buttonSalva.onClick.AddListener(SalvaRegistrazione);
        if (buttonRipeti != null) buttonRipeti.onClick.AddListener(RipetiRegistrazione);
        if (buttonNuovaRegistrazione != null) buttonNuovaRegistrazione.onClick.AddListener(ResetUI);
    }

    void AvviaRegistrazione()
    {
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

        ToggleButtons(avvia: false, stop: true, salva: false, ripeti: false, modello: false, nuova: false);
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
        // Versione originale senza controlli null per mantenere la funzionalità
        buttonAvvia.gameObject.SetActive(avvia);
        buttonStop.gameObject.SetActive(stop);
        buttonSalva.gameObject.SetActive(salva);
        buttonRipeti.gameObject.SetActive(ripeti);
        viewportModello3D.SetActive(modello);
        buttonNuovaRegistrazione.gameObject.SetActive(nuova);
    }
}