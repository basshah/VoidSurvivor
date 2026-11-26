using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class FpsCounter : MonoBehaviour
{
    public float updateInterval = 0.5f;

    TMP_Text label;
    float elapsed;
    int frames;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
        label.raycastTarget = false;
    }

    void Update()
    {
        frames++;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed < updateInterval) return;

        float fps = frames / elapsed;
        label.SetText("FPS {0:0}", fps);
        frames = 0;
        elapsed = 0f;
    }
}
