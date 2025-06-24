using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class WebcamDisplay : MonoBehaviour
{

   public RawImage rawImage; // Riferimento all'oggetto per mostrare il video
   private WebCamTexture webcamTexture; // Oggetto per accedere alla webcam
   void Start()
   {
      // Assicuro che ci siano webcam disponibili
      if (WebCamTexture.devices.Length == 0)
      {
         Debug.LogError("Nessuna webcam trovata.");
         return;
      }
      else
      {
         // Inizializza la webcam utilizzando la prima webcam disponibile
         webcamTexture = new WebCamTexture(WebCamTexture.devices[0].name);
         // Avvia la webcam
         webcamTexture.Play();
         // Imposta la texture della RawImage con il flusso video della webcam
         rawImage.texture = webcamTexture;
         // Imposta le dimensioni del piano in base alle dimensioni della webcam
         rawImage.rectTransform.sizeDelta = new Vector2(webcamTexture.width,
        webcamTexture.height);
      }
   }
   void Update()
   {
      if (webcamTexture.isPlaying)
      {
         // Aggiorna la texture della RawImage con il frame corrente della webcam
         rawImage.texture = webcamTexture;
      }
   }
}