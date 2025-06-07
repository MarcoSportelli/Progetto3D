using UnityEngine;
using System.Collections.Generic;

public class LegAnimator : MonoBehaviour
{
    [Header("Landmark Transforms")]
    public Transform hip;
    public Transform knee;
    public Transform ankle;
    public Transform heel;
    public Transform footIndex;

    [Header("Segment Transforms")]
    public Transform quadricipite; // segmento tra hip e knee
    public Transform polpaccio;    // segmento tra knee e ankle
    public Transform piede;        // segmento tra ankle e footIndex

    [Header("CSV Data")]
    public TextAsset csvFile;

    public float scale = 100f;
    public float frameRate = 30f;

    private List<Vector3[]> frames = new List<Vector3[]>();
    private int currentFrame = 0;

    void Start()
    {
        LoadCSV();
        InvokeRepeating(nameof(UpdateFrame), 0f, 1f / frameRate);
    }

    void LoadCSV()
    {
        var lines = csvFile.text.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var tokens = line.Split(',');
            if (tokens.Length < 15) continue;

            Vector3[] frameLandmarks = new Vector3[5];
            for (int i = 0; i < 5; i++)
            {
                float x = float.Parse(tokens[i * 3]);
                float y = float.Parse(tokens[i * 3 + 1]);
                float z = float.Parse(tokens[i * 3 + 2]);
                frameLandmarks[i] = new Vector3(x, y, z) * scale;
            }
            frames.Add(frameLandmarks);
        }
    }

    void UpdateFrame()
    {
        if (currentFrame >= frames.Count)
        {
            currentFrame = 0; // Loop
        }

        var f = frames[currentFrame];

        // Aggiorna posizioni landmark
        hip.position = f[0];
        knee.position = f[1];
        ankle.position = f[2];
        heel.position = f[3];
        footIndex.position = f[4];

        // Aggiorna rotazioni segmenti
        quadricipite.position = hip.position;
        quadricipite.rotation = Quaternion.LookRotation(knee.position - hip.position);

        polpaccio.position = knee.position;
        polpaccio.rotation = Quaternion.LookRotation(ankle.position - knee.position);

        piede.position = ankle.position;
        piede.rotation = Quaternion.LookRotation(footIndex.position - ankle.position);

        currentFrame++;
    }
}
