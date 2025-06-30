using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RecordManagerUI : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button buttonAvvia;
    public Button buttonSalva;
    public Button buttonRipeti;
    public GameObject viewportModello3D;

    [Header("Controllo Registrazione")]
    public RecordPose recordPose;

    [Header("UI Timer")]
    public Text textTimer; // AGGIUNGI QUESTO

    private Coroutine recordingCoroutine;

    void Start()
    {
        if (recordPose != null && textTimer != null)
            recordPose.timerText = textTimer;

        // Sottoscrivi l'evento
        if (recordPose != null)
            recordPose.OnRecordingFinished += OnRecordingFinished;

        buttonAvvia.gameObject.SetActive(true);
        buttonSalva.gameObject.SetActive(false);
        buttonRipeti.gameObject.SetActive(false);
        viewportModello3D.SetActive(false);
        if (textTimer != null)
            textTimer.gameObject.SetActive(false);

        buttonAvvia.onClick.AddListener(AvviaRegistrazione);
        buttonSalva.onClick.AddListener(SalvaRegistrazione);
        buttonRipeti.onClick.AddListener(RipetiRegistrazione);
    }

    private void OnRecordingFinished()
    {
        ToggleButtons(avvia: false, timer: false, salva: true, ripeti: true, modello: false);
    }
    void AvviaRegistrazione()
    {
        recordingCoroutine = StartCoroutine(recordPose.RecordRealSenseBag());
        ToggleButtons(avvia: false, timer: true, salva: false, ripeti: false, modello: false);
    }

    void StoppaRegistrazione()
    {
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
            recordPose.ForceStopRecording();
            recordingCoroutine = null;
        }

        ToggleButtons(avvia: false, timer: false, salva: true, ripeti: true, modello: false);
    }

    void SalvaRegistrazione()
    {
        StartCoroutine(recordPose.ExtractAndInfer());
        ToggleButtons(avvia: true, timer: false, salva: false, ripeti: false, modello: true);
    }

    void RipetiRegistrazione()
    {
        AvviaRegistrazione();
    }

    void ToggleButtons(bool avvia, bool timer, bool salva, bool ripeti, bool modello)
    {
        buttonAvvia.gameObject.SetActive(avvia);
        buttonSalva.gameObject.SetActive(salva);
        buttonRipeti.gameObject.SetActive(ripeti);
        viewportModello3D.SetActive(modello);
        if (textTimer != null)
            textTimer.gameObject.SetActive(timer);
    }
}