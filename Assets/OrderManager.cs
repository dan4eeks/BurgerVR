using System;
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

    [Header("Order timing")]
    [SerializeField] private float maxCookTime = 30f;

    private readonly List<IngredientType> currentRecipe = new List<IngredientType>();

    private bool orderActive = false;
    private float cookTimer = 0f;

    private enum OrderGrade
    {
        Fail,
        Bad,
        Excellent
    }

    private void Awake()
    {
        if (plateSpawner == null)
            plateSpawner = FindObjectOfType<PlateSpawner>();

        if (customerManager == null)
            customerManager = FindObjectOfType<CustomerManager>();

        if (recipeText != null) recipeText.text = "";
        if (resultText != null)
        {
            resultText.text = "Ожидание клиента...";
            resultText.color = Color.white;
        }
    }

    public List<IngredientType> GetCurrentRecipeCopy()
    {
        return new List<IngredientType>(currentRecipe);
    }


    private void Update()
    {
        if (!orderActive) return;

        cookTimer += Time.deltaTime;
        if (cookTimer >= maxCookTime)
            FailByTimeout();
    }

    // =========================
    //  ЗАПУСК НОВОГО ЗАКАЗА
    // =========================
    public void StartNewOrder()
    {
        orderActive = true;
        cookTimer = 0f;

        GenerateRandomRecipe();
        UpdateRecipeUI();

        if (resultText != null)
        {
            resultText.text = "";
            resultText.color = Color.white;
        }
    }

    // =========================
    //  SUBMIT ЗАКАЗА
    // =========================
    public void Submit(Plate plate)
    {
        if (plate == null) return;

        // ? ВСЕГДА очищаем тарелку и спавним новую (чтобы тарелки не "зависали")
        void RespawnPlate()
        {
            Destroy(plate.gameObject);
            if (plateSpawner != null)
                plateSpawner.OnPlateSubmitted();
        }

        // Если заказа нет — просто переспавним тарелку и выходим
        if (!orderActive)
        {
            RespawnPlate();
            return;
        }

        bool recipeOk = SameSequence(plate.Stack, currentRecipe);
        bool pattiesOk = recipeOk && PattiesCookedCorrectly(plate);
        bool dirtyOk = recipeOk && !HasAnyDirtyIngredient(plate);

        OrderGrade grade;
        if (!recipeOk)
            grade = OrderGrade.Fail;
        else if (!pattiesOk || !dirtyOk)
            grade = OrderGrade.Bad;
        else
            grade = OrderGrade.Excellent;

        // UI
        if (resultText != null)
        {
            switch (grade)
            {
                case OrderGrade.Fail:
                    resultText.text = "ORDER FAIL";
                    resultText.color = Color.red;
                    break;

                case OrderGrade.Bad:
                    // покажем причину
                    string reason = "";
                    if (!pattiesOk) reason += " Patty raw/burnt;";
                    if (!dirtyOk) reason += " Ingredients dirty;";
                    resultText.text = "ORDER BAD!" + reason;
                    resultText.color = Color.yellow;
                    break;

                case OrderGrade.Excellent:
                    resultText.text = "ORDER EXCELLENT!";
                    resultText.color = Color.green;
                    break;
            }

            // + настроение клиента (кроме alwaysAngry)
            Customer c = customerManager != null ? customerManager.ActiveCustomer : null;
            if (c != null && !c.alwaysAngry)
                resultText.text += "\nMood: " + c.mood;
        }

        // завершаем заказ
        orderActive = false;

        // скрываем рецепт после сдачи
        if (recipeText != null) recipeText.text = "";

        // Клиент доволен только если Excellent
        bool orderOk = (grade == OrderGrade.Excellent);
        if (customerManager != null)
            customerManager.CompleteActiveCustomer(orderOk);

        RespawnPlate();
    }

    private void FailByTimeout()
    {
        orderActive = false;

        if (recipeText != null) recipeText.text = "";

        if (resultText != null)
        {
            resultText.text = "Клиент ушёл\nОжидание клиента...";
            resultText.color = Color.white;
        }

        // Уведомляем UI через CustomerManager (у тебя это уже заведено)
        // Если активный клиент был принят — убираем его как "плохой заказ"
        if (customerManager != null && customerManager.ActiveCustomer != null)
            customerManager.CompleteActiveCustomer(false);
        else
            customerManager?.OnActiveCustomerLeft?.Invoke();
    }

    // =========================
    //  ПРОВЕРКА ПРОЖАРКИ
    // =========================
    private bool PattiesCookedCorrectly(Plate plate)
    {
        // Если котлет нет — ок
        if (plate.PattyStates == null || plate.PattyStates.Count == 0)
            return true;

        for (int i = 0; i < plate.PattyStates.Count; i++)
        {
            if (plate.PattyStates[i] != PattyCookState.Cooked)
                return false;
        }
        return true;
    }

    // =========================
    //  ПРОВЕРКА ГРЯЗИ
    // =========================
    private bool HasAnyDirtyIngredient(Plate plate)
    {
        if (plate.DirtyFlags == null || plate.DirtyFlags.Count == 0)
            return false;

        for (int i = 0; i < plate.DirtyFlags.Count; i++)
        {
            if (plate.DirtyFlags[i])
                return true;
        }
        return false;
    }

    // =========================
    //  РЕЦЕПТЫ
    // =========================
    private void GenerateRandomRecipe()
    {
        currentRecipe.Clear();

        // База
        currentRecipe.Add(IngredientType.BunBottom);
        currentRecipe.Add(IngredientType.Patty);

        // Универсальные "добавки" из enum (без жёстких имён вроде Cheese)
        IngredientType[] all = (IngredientType[])Enum.GetValues(typeof(IngredientType));
        List<IngredientType> extras = new List<IngredientType>();

        for (int i = 0; i < all.Length; i++)
        {
            IngredientType v = all[i];
            if (v == IngredientType.BunBottom) continue;
            if (v == IngredientType.BunTop) continue;
            if (v == IngredientType.Patty) continue;
            extras.Add(v);
        }

        int extrasCount = extras.Count == 0 ? 0 : UnityEngine.Random.Range(0, 3); // 0..2
        for (int i = 0; i < extrasCount; i++)
        {
            currentRecipe.Add(extras[UnityEngine.Random.Range(0, extras.Count)]);
        }

        currentRecipe.Add(IngredientType.BunTop);
    }

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
