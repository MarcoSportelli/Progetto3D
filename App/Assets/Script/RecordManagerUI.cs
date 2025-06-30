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
    public GameObject canvasCamera; // AGGIUNGI QUESTO

    [Header("Controllo Registrazione")]
    public RecordPose recordPose;

    [Header("RealSense Device")]
    public RsDevice rsDevice;

    void Start()
    {
        buttonAvvia.gameObject.SetActive(true);
        buttonStop.gameObject.SetActive(false);
        buttonSalva.gameObject.SetActive(false);
        buttonRipeti.gameObject.SetActive(false);
        buttonNuovaRegistrazione.gameObject.SetActive(false);
        viewportModello3D.SetActive(false);

        if (canvasCamera != null)
            canvasCamera.SetActive(true);

        buttonAvvia.onClick.AddListener(AvviaRegistrazione);
        buttonStop.onClick.AddListener(StoppaRegistrazione);
        buttonSalva.onClick.AddListener(SalvaRegistrazione);
        buttonRipeti.onClick.AddListener(RipetiRegistrazione);
        buttonNuovaRegistrazione.onClick.AddListener(ResetUI);
    }

    void AvviaRegistrazione()
    {
        if (rsDevice != null && recordPose != null)
        {
            // Usa lo stesso timestamp di RecordPose per coerenza
            recordPose.isLeftLeg = recordPose.leftLegToggle != null ? recordPose.leftLegToggle.isOn : true;
            recordPose.currentRecordingTimestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + (recordPose.isLeftLeg ? "_left" : "_right");
            string videoFilePath = System.IO.Path.Combine(recordPose.dataPath, $"{recordPose.currentRecordingTimestamp}.bag");

            rsDevice.DeviceConfiguration.RecordPath = videoFilePath;
            rsDevice.DeviceConfiguration.mode = RsConfiguration.Mode.Record;
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
        yield return StartCoroutine(recordPose.ExtractAndInfer());
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

        ToggleButtons(avvia: true, stop: false, salva: false, ripeti: false, modello: false, nuova: false);
    }

    void ToggleButtons(bool avvia, bool stop, bool salva, bool ripeti, bool modello, bool nuova)
    {
        buttonAvvia.gameObject.SetActive(avvia);
        buttonStop.gameObject.SetActive(stop);
        buttonSalva.gameObject.SetActive(salva);
        buttonRipeti.gameObject.SetActive(ripeti);
        viewportModello3D.SetActive(modello);
        buttonNuovaRegistrazione.gameObject.SetActive(nuova);
    }
}