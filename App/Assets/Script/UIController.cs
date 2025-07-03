using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    [Header("Pannelli UI")]
    public GameObject panelMainMenu;
    public GameObject panelRegistrazione;
    public GameObject panelStorico;

    [Header("Bottoni Registrazione")]
    public GameObject buttonStart;
    public GameObject buttonSave;
    public GameObject buttonToggleLeg;
    public GameObject viewportModello3D;

    void Start()
    {
        // Controlla in quale scena ti trovi e imposta l'UI di conseguenza
        string currentScene = SceneManager.GetActiveScene().name;
        
        switch (currentScene)
        {
            case "Main":
                SetupMainMenu();
                break;
            case "Registrazione":
                SetupRegistrazione();
                break;
            case "Storico":
                SetupStorico();
                break;
        }
    }

    private void SetupMainMenu()
    {
        Debug.Log("Setup: Menu Principale");
        
        // Attiva solo il pannello del menu principale
        if (panelMainMenu != null) panelMainMenu.SetActive(true);
        if (panelRegistrazione != null) panelRegistrazione.SetActive(false);
        if (panelStorico != null) panelStorico.SetActive(false);
        
        // Nascondi tutti i bottoni di registrazione
        SetRegistrazioneButtonsVisible(false);
        if (viewportModello3D != null) viewportModello3D.SetActive(false);
    }

    private void SetupRegistrazione()
    {
        Debug.Log("Setup: Registrazione");
        
        // Attiva solo il pannello di registrazione
        if (panelMainMenu != null) panelMainMenu.SetActive(false);
        if (panelRegistrazione != null) panelRegistrazione.SetActive(true);
        if (panelStorico != null) panelStorico.SetActive(false);
        
        // Mostra bottoni di registrazione nello stato iniziale
        if (buttonStart != null) buttonStart.SetActive(true);
        if (buttonSave != null) buttonSave.SetActive(false);
        if (buttonToggleLeg != null) buttonToggleLeg.SetActive(true);
        if (viewportModello3D != null) viewportModello3D.SetActive(false);
    }

    private void SetupStorico()
    {
        Debug.Log("Setup: Storico");
        
        // Attiva solo il pannello dello storico
        if (panelMainMenu != null) panelMainMenu.SetActive(false);
        if (panelRegistrazione != null) panelRegistrazione.SetActive(false);
        if (panelStorico != null) panelStorico.SetActive(true);
        
        // Nascondi bottoni di registrazione
        SetRegistrazioneButtonsVisible(false);
        if (viewportModello3D != null) viewportModello3D.SetActive(false);
    }

    public void VaiARegistrazione()
    {
        Debug.Log("Navigazione: Vai a Registrazione");
        
        // Controlla se la scena esiste nelle build settings
        if (Application.CanStreamedLevelBeLoaded("Registrazione"))
        {
            SceneManager.LoadScene("Registrazione");
        }
        else
        {
            Debug.LogError("Scena 'Registrazione' non trovata! Aggiungila alle Build Settings.");
        }
    }

    public void VaiAStorico()
    {
        Debug.Log("Navigazione: Vai a Storico");
        
        // Controlla se la scena esiste nelle build settings
        if (Application.CanStreamedLevelBeLoaded("Storico"))
        {
            SceneManager.LoadScene("Storico");
        }
        else
        {
            Debug.LogError("Scena 'Storico' non trovata! Aggiungila alle Build Settings.");
        }
    }

    public void TornaAlMenu()
    {
        Debug.Log("Navigazione: Torna al Menu Principale");
        SceneManager.LoadScene("Main");
        // Non cercare di modificare gli oggetti dopo LoadScene!
    }

    private void SetRegistrazioneButtonsVisible(bool visible)
    {
        // Controlla se gli oggetti esistono prima di usarli
        if (buttonStart != null) buttonStart.SetActive(!visible);
        if (buttonSave != null) buttonSave.SetActive(visible);
        if (buttonToggleLeg != null) buttonToggleLeg.SetActive(visible);
    }

    // Metodi per gestire gli stati durante la registrazione
    public void StartRecording()
    {
        if (buttonStart != null) buttonStart.SetActive(false);
        if (buttonSave != null) buttonSave.SetActive(false);
    }

    public void StopRecording()
    {
        if (buttonStart != null) buttonStart.SetActive(true);
        if (buttonSave != null) buttonSave.SetActive(true);
    }

    public void SaveRecording()
    {
        if (buttonStart != null) buttonStart.SetActive(true);
        if (buttonSave != null) buttonSave.SetActive(false);
    }    
}