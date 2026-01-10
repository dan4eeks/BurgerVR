using UnityEngine;
using TMPro;
using System.Collections;

public class ShiftIntroScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goalText;

    [Header("Timings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float visibleDuration = 2f;

    public IEnumerator Play(int day, int targetClients)
    {
        titleText.text = $"День {day}";
        goalText.text = $"Цель: {targetClients} клиентов";

        // fade in
        yield return Fade(0f, 1f);

        // visible
        yield return new WaitForSeconds(visibleDuration);

        // fade out
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
