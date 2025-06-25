using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WebcamDisplay : MonoBehaviour
{
    public RawImage rawImage;
    
    [SerializeField] private string preferredCameraName = "HP Wide Vision HD Camera"; // Cambia con il nome della tua cam fisica
    private WebCamTexture webcamTexture;

    void Start()
    {
        var devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("Nessuna webcam trovata.");
            return;
        }

        string selectedDeviceName = null;

        foreach (var device in devices)
        {
            Debug.Log("Dispositivo trovato: " + device.name);
            if (device.name.Contains(preferredCameraName))
            {
                selectedDeviceName = device.name;
                break;
            }
        }

        if (string.IsNullOrEmpty(selectedDeviceName))
        {
            Debug.LogError($"Nessuna webcam trovata con nome contenente: \"{preferredCameraName}\"");
            return;
        }

        webcamTexture = new WebCamTexture(selectedDeviceName);
        webcamTexture.Play();
        rawImage.texture = webcamTexture;
        rawImage.rectTransform.sizeDelta = new Vector2(webcamTexture.width, webcamTexture.height);

        Debug.Log($"✅ Webcam in uso da Unity: {selectedDeviceName}");
    }

    void Update()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            rawImage.texture = webcamTexture;
        }
    }
}
