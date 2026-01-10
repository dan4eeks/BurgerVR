using System.Collections;
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

    [Header("Customer Reaction")]
    [SerializeField] private float thinkingMinTime = 3f;
    [SerializeField] private float thinkingMaxTime = 4f;

    [Header("Order timing")]
    [SerializeField] private float maxCookTime = 160f;

    [Header("Thinking SFX")]
    [SerializeField] private AudioSource reactionAudioSource;
    [SerializeField] private AudioClip drumrollClip;

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

    private Coroutine reactionCoroutine;

    private IEnumerator HandleCustomerReaction(Customer customer, CustomerMood resultMood)
    {
        Debug.Log("HandleCustomerReaction started");
        if (customer == null)
            yield break;

        // 1?? THINKING
        customer.StartThinking();

        // ?? START DRUMROLL
        if (reactionAudioSource != null && drumrollClip != null)
        {
            reactionAudioSource.Stop();
            reactionAudioSource.clip = drumrollClip;
            reactionAudioSource.loop = true;
            reactionAudioSource.Play();
        }
        else
        {
            Debug.LogWarning($"Drumroll missing. source={(reactionAudioSource != null)} clip={(drumrollClip != null)}");
        }

        float thinkingDelay = UnityEngine.Random.Range(thinkingMinTime, thinkingMaxTime);
        yield return new WaitForSeconds(thinkingDelay);

        // ?? STOP DRUMROLL
        if (reactionAudioSource != null && reactionAudioSource.isPlaying)
            reactionAudioSource.Stop();

        // 2?? RESULT (анимация + звук)
        customer.ApplyOrderResult(resultMood);

        // 3?? ? ЖДЁМ, ПОКА ЗАКОНЧИТСЯ ЗВУК РЕАКЦИИ
        float reactionWait = customer.LastReactionDuration;
        if (reactionWait <= 0f)
            reactionWait = 0.25f;

        yield return new WaitForSeconds(reactionWait);

        // 4?? ?? КЛИЕНТ УХОДИТ (ОЧЕРЕДЬ ДВИГАЕТСЯ)
        bool orderOk = (resultMood != CustomerMood.Angry);
        customerManager?.CompleteActiveCustomer(orderOk);
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

    public void NotifyPlateSubmitted()
    {
        if (plateSpawner != null)
            plateSpawner.OnPlateSubmitted();
    }


    // =========================
    //  SUBMIT ЗАКАЗА
    // =========================
    public bool Submit(Plate plate)
    {
        Debug.Log($"OrderManager.Submit called. orderActive={orderActive}");
        if (!orderActive || plate == null)
            return false;

        // 1) Считаем штрафные очки
        int points = CalculatePenaltyPoints(plate);

        // 2) Определяем настроение клиента по очкам + учитываем "вечно злого"
        Customer customer = (customerManager != null) ? customerManager.ActiveCustomer : null;

        CustomerMood resultMood;
        if (customer != null && customer.alwaysAngry)
        {
            resultMood = CustomerMood.Angry;
        }
        else if (points >= 10)
        {
            resultMood = CustomerMood.Angry;
        }
        else if (points >= 5)
        {
            resultMood = CustomerMood.Neutral;
        }
        else
        {
            resultMood = CustomerMood.Happy;
        }

        // 3) Обновляем UI (по желанию, удобно для отладки)
        if (resultText != null)
            resultText.text = $"Penalty: {points}\nResult: {resultMood}";

        // 4) Заказ завершён (чтобы таймер не тикал дальше)
        orderActive = false;

        // 5) СТАРТ реакции: Thinking -> Result -> (после звука) уход
        if (reactionCoroutine != null)
            StopCoroutine(reactionCoroutine);

        reactionCoroutine = StartCoroutine(HandleCustomerReaction(customer, resultMood));

        // 6) Очистка тарелки/спавн новой — это у тебя уже было раньше.
        // Если у тебя здесь была логика очистки/удаления — оставь её как было.

        // Submit принят (мы запустили реакцию)
        return true;
    }


    private void FailByTimeout()
    {
        orderActive = false;

        if (recipeText != null) recipeText.text = "";

        if (resultText != null)
        {
            resultText.text = "Слишком долго...";
            resultText.color = Color.red;
        }

        var c = customerManager != null ? customerManager.ActiveCustomer : null;
        if (c != null)
        {
            c.ForceAngry();
        }
        else
        {
            customerManager?.OnActiveCustomerLeft?.Invoke();
        }
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

    private int CalculatePenaltyPoints(Plate plate)
    {
        int points = 0;

        // (1) котлеты: +10 за каждую НЕ Cooked
        if (plate.PattyStates != null)
        {
            for (int i = 0; i < plate.PattyStates.Count; i++)
            {
                if (plate.PattyStates[i] != PattyCookState.Cooked)
                    points += 10;
            }
        }

        // (2) грязь: +10 за каждый DirtyFlags[i] == true
        if (plate.DirtyFlags != null)
        {
            for (int i = 0; i < plate.DirtyFlags.Count; i++)
            {
                if (plate.DirtyFlags[i])
                    points += 10;
            }
        }

        // (3)(5) время готовки
        float ratio = (maxCookTime > 0f) ? (cookTimer / maxCookTime) : 0f;
        if (ratio >= 0.75f && ratio < 1.0f) points += 5;
        else if (ratio >= 0.50f && ratio < 0.75f) points += 2;

        // (4) промахи по рецепту: +3 за каждый mismatch/лишний/недостающий
        int aCount = plate.Stack != null ? plate.Stack.Count : 0;
        int bCount = currentRecipe != null ? currentRecipe.Count : 0;
        int max = Mathf.Max(aCount, bCount);

        int misses = 0;
        for (int i = 0; i < max; i++)
        {
            bool hasA = i < aCount;
            bool hasB = i < bCount;

            if (!hasA || !hasB)
            {
                misses++; // лишний или недостающий
                continue;
            }

            if (plate.Stack[i] != currentRecipe[i])
                misses++;
        }

        points += misses * 3;

        return points;
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
