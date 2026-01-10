using UnityEngine;
using TMPro;
using System.Collections;

public class GameOverScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text reasonText; // можно оставить null

    [Header("Timings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float visibleDuration = 2.5f;

    public IEnumerator Play(string reason)
    {
        if (titleText != null) titleText.text = "ИГРА ОКОНЧЕНА";
        if (reasonText != null) reasonText.text = string.IsNullOrWhiteSpace(reason) ? "" : $"Причина: {reason}";

        yield return Fade(0f, 1f);
        yield return new WaitForSeconds(visibleDuration);
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        canvasGroup.alpha = from;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
