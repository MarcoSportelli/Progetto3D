using UnityEngine;

public class UIController : MonoBehaviour
{
    public GameObject panelMainMenu;
    public GameObject panelRegistrazione;
    public GameObject panelStorico;

    public void VaiARegistrazione()
    {
        panelMainMenu.SetActive(false);
        panelRegistrazione.SetActive(true);
        panelStorico.SetActive(false);
    }

    public void VaiAStorico()
    {
        panelMainMenu.SetActive(false);
        panelRegistrazione.SetActive(false);
        panelStorico.SetActive(true);
    }

    public void TornaAlMenu()
    {
        panelMainMenu.SetActive(true);
        panelRegistrazione.SetActive(false);
        panelStorico.SetActive(false);
    }
}
