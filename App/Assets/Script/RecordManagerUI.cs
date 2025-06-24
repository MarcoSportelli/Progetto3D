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
    public GameObject viewportModello3D;

    [Header("Controllo Registrazione")]
    public RecordPose recordPose;

    private Coroutine recordingCoroutine;

    void Start()
    {
        // Inizializza lo stato dei pulsanti
        buttonAvvia.gameObject.SetActive(true);
        buttonStop.gameObject.SetActive(false);
        buttonSalva.gameObject.SetActive(false);
        buttonRipeti.gameObject.SetActive(false);
        viewportModello3D.SetActive(false);

        // Assegna funzioni ai pulsanti
        buttonAvvia.onClick.AddListener(AvviaRegistrazione);
        buttonStop.onClick.AddListener(StoppaRegistrazione);
        buttonSalva.onClick.AddListener(SalvaRegistrazione);
        buttonRipeti.onClick.AddListener(RipetiRegistrazione);
    }

    void AvviaRegistrazione()
    {
        // Avvia registrazione
        recordingCoroutine = StartCoroutine(recordPose.RecordOnly());
        ToggleButtons(avvia: false, stop: true, salva: false, ripeti: false, modello: false);
    }

    void StoppaRegistrazione()
    {
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
            recordPose.ForceStopRecording();
            recordingCoroutine = null;
        }

        ToggleButtons(avvia: false, stop: false, salva: true, ripeti: true, modello: false);
    }

    void SalvaRegistrazione()
    {
        StartCoroutine(recordPose.ExtractAndInfer());
        ToggleButtons(avvia: true, stop: false, salva: false, ripeti: false, modello:true);
    }

    void RipetiRegistrazione()
    {
        AvviaRegistrazione(); // Riavvia da capo
    }

    void ToggleButtons(bool avvia, bool stop, bool salva, bool ripeti, bool modello)
    {
        buttonAvvia.gameObject.SetActive(avvia);
        buttonStop.gameObject.SetActive(stop);
        buttonSalva.gameObject.SetActive(salva);
        buttonRipeti.gameObject.SetActive(ripeti);
        viewportModello3D.SetActive(modello);
    }
}
