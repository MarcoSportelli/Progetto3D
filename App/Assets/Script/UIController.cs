using UnityEngine;

public class UIController : MonoBehaviour
{
    [Header("Pannelli UI")]
    public GameObject panelMainMenu;
    public GameObject panelRegistrazione;
    public GameObject panelStorico;

    [Header("Bottoni Registrazione")]
    public GameObject buttonStart;
    public GameObject buttonStop;
    public GameObject buttonSave;
    public GameObject buttonToggleLeg;
    public GameObject viewportModello3D;
    void Start()
    {
        TornaAlMenu(); // Pannello iniziale
    }

    public void VaiARegistrazione()
    {
        Debug.Log("Navigazione: Vai a Registrazione");
        panelMainMenu.SetActive(false);
        panelRegistrazione.SetActive(true);
        panelStorico.SetActive(false);

        // Mostra bottoni registrazione
        //SetRegistrazioneButtonsVisible(true);
    }

    public void VaiAStorico()
    {
        Debug.Log("Navigazione: Vai a Storico");
        panelMainMenu.SetActive(false);
        panelRegistrazione.SetActive(false);
        panelStorico.SetActive(true);

        // Nascondi bottoni registrazione
        //SetRegistrazioneButtonsVisible(false);
    }

    public void TornaAlMenu()
    {
        Debug.Log("Navigazione: Torna al Menu Principale");
        panelMainMenu.SetActive(true);
        panelRegistrazione.SetActive(false);
        panelStorico.SetActive(false);
        buttonStart.SetActive(true);
        buttonStop.SetActive(false);
        buttonSave.SetActive(false);
        buttonToggleLeg.SetActive(true);
        viewportModello3D.SetActive(false);
        // Nascondi bottoni registrazione
        //SetRegistrazioneButtonsVisible(false);
    }

    private void SetRegistrazioneButtonsVisible(bool visible)
    {
        buttonStart.SetActive(!visible);
        buttonStop.SetActive(visible);
        buttonSave.SetActive(visible);
        buttonToggleLeg.SetActive(visible);
    }
}
