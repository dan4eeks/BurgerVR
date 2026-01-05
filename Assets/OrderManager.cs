using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OrderManager : MonoBehaviour
{
    [SerializeField] private PlateSpawner plateSpawner;

    [Header("UI")]
    [SerializeField] private TMP_Text recipeText;
    [SerializeField] private TMP_Text resultText;

    private readonly List<IngredientType> currentRecipe = new List<IngredientType>()
    {
        IngredientType.BunBottom,
        IngredientType.Patty,
        IngredientType.BunTop
    };

    private void Awake()
    {
        if (plateSpawner == null)
            plateSpawner = FindObjectOfType<PlateSpawner>();

        UpdateRecipeUI();
        if (resultText != null)
            resultText.text = "";
    }

    private void UpdateRecipeUI()
    {
        if (recipeText == null) return;

        recipeText.text = "Recipe:\n";
        foreach (var ing in currentRecipe)
            recipeText.text += ing + "\n";
    }

    public void Submit(Plate plate)
    {
        bool ok = SameSequence(plate.Stack, currentRecipe);

        if (resultText != null)
        {
            resultText.text = ok ? "ORDER OK" : "ORDER FAIL";
            resultText.color = ok ? Color.green : Color.red;
        }

        Destroy(plate.gameObject);

        if (plateSpawner != null)
            plateSpawner.OnPlateSubmitted();
    }

    private bool SameSequence(IReadOnlyList<IngredientType> a, IReadOnlyList<IngredientType> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}
