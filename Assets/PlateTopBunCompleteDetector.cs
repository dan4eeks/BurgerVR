using System.Collections;
using UnityEngine;

public class PlateTopBunCompleteDetector : MonoBehaviour
{
    [SerializeField] private IngredientType completeOnType = IngredientType.BunTop;
    [SerializeField] private float confirmDelay = 0.08f; // подожди чуть-чуть, чтобы успел snap

    private SubmitZoneHighlighter submitHighlight;
    private bool completed;

    private void Awake()
    {
        // найдЄм подсветку в сцене (можно руками прив€зать, но так проще)
        submitHighlight = FindObjectOfType<SubmitZoneHighlighter>(true);
    }

    private void OnEnable()
    {
        completed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;

        Ingredient ing = other.GetComponentInParent<Ingredient>();
        if (ing == null) return;

        // BunTop?
        if (ing.type != completeOnType) return;

        // подождЄм, пока тарелка реально "защЄлкнет" ингредиент
        StartCoroutine(ConfirmSnapAndHighlight(ing));
    }

    private IEnumerator ConfirmSnapAndHighlight(Ingredient ing)
    {
        yield return new WaitForSeconds(confirmDelay);

        // snapped Ч поле уже есть в Ingredient :contentReference[oaicite:2]{index=2}
        if (ing == null) yield break;
        if (!ing.snapped) yield break;

        completed = true;
        if (submitHighlight != null)
            submitHighlight.SetHighlight(true);
    }
}
