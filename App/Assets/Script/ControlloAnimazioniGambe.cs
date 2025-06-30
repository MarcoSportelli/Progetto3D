using UnityEngine;

public class ControlloAnimazioniGambe : MonoBehaviour
{
    public Animator animator;

    public Camera cameraSX;
    public Camera cameraDX;

    public void AvviaAnimazione(string lato, string movimento)
    {
        string nomeTrigger = "";

        // 1. Determina il trigger
        switch (movimento.ToLower())
        {
            case "squat":
                nomeTrigger = "Trigger_Squat";
                break;

            case "flessione_avanti":
                nomeTrigger = $"Trigger_FlessAvanti_{lato.ToUpper()}";
                break;

            case "flessione_indietro":
                nomeTrigger = $"Trigger_FlessIndietro_{lato.ToUpper()}";
                break;

            case "estensione_gamba":
                nomeTrigger = $"Trigger_Estensione_{lato.ToUpper()}";
                break;

            default:
                Debug.LogError("Movimento non riconosciuto.");
                return;
        }

        // 2. Attiva/disattiva le camere se necessario
        cameraSX.gameObject.SetActive(false);
        cameraDX.gameObject.SetActive(false);

        
        if (lato == "sx")
            cameraSX.gameObject.SetActive(true);
        else if (lato == "dx")
            cameraDX.gameObject.SetActive(true);
        else
        {
            Debug.LogError("Lato non valido. Usa 'sx' o 'dx'.");
            return;
        }
        

        // 3. Attiva il trigger nell'Animator
        if (animator)
        {
            animator.SetTrigger(nomeTrigger);
            Debug.Log($"Animazione avviata: {nomeTrigger}");
        }
        else
        {
            Debug.LogError("Animator non assegnato.");
        }
    }
}
