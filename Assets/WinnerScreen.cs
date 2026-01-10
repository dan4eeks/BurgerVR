using UnityEngine;
using TMPro;
using System.Collections;

public class WinnerScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text progressText;

    [Header("Timings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float visibleDuration = 2.0f;

    public IEnumerator PlayShiftCleared(int shift, int targetShifts)
    {
        if (titleText != null) titleText.text = "СМЕНА ПРОЙДЕНА ?";
        if (progressText != null) progressText.text = $"Прогресс: {shift} / {targetShifts}";

        yield return Fade(0f, 1f);
        yield return new WaitForSeconds(visibleDuration);
        yield return Fade(1f, 0f);
    }

    public IEnumerator PlayFinalVictory()
    {
        if (titleText != null) titleText.text = "ПОБЕДА ??";
        if (progressText != null) progressText.text = "Вы пережили все смены!";

        yield return Fade(0f, 1f);
        yield return new WaitForSeconds(2.5f);
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
