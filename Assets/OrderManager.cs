using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OrderManager : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private PlateSpawner plateSpawner;
    [SerializeField] private CustomerManager customerManager;

    [Header("UI")]
    [SerializeField] private TMP_Text recipeText;
    [SerializeField] private TMP_Text resultText;

    // Текущий рецепт
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

        if (customerManager == null)
            customerManager = FindObjectOfType<CustomerManager>();

        UpdateRecipeUI();

        if (resultText != null)
            resultText.text = "";
    }

    // =========================
    //  ЗАПУСК НОВОГО ЗАКАЗА
    // =========================
    public void StartNewOrder()
    {
        // Пока рецепт фиксированный
        UpdateRecipeUI();

        if (resultText != null)
            resultText.text = "";
    }

    // =========================
    //  SUBMIT ЗАКАЗА
    // =========================
    public void Submit(Plate plate)
    {
        if (plate == null) return;

        bool ok = SameSequence(plate.Stack, currentRecipe);

        // UI результат
        if (resultText != null)
        {
            resultText.text = ok ? "ORDER OK" : "ORDER FAIL";
            resultText.color = ok ? Color.green : Color.red;
        }

        // Сообщаем CustomerManager
        if (customerManager != null)
            customerManager.CompleteActiveCustomer(ok);

        // Удаляем тарелку и спавним новую
        Destroy(plate.gameObject);

        if (plateSpawner != null)
            plateSpawner.OnPlateSubmitted();
    }

    // =========================
    //  UI
    // =========================
    private void UpdateRecipeUI()
    {
        if (recipeText == null) return;

        recipeText.text = "Recipe:\n";
        foreach (IngredientType ing in currentRecipe)
            recipeText.text += ing + "\n";
    }

    // =========================
    //  UTILS
    // =========================
    private bool SameSequence(IReadOnlyList<IngredientType> a, IReadOnlyList<IngredientType> b)
    {
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }
}
