using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomerOrderUI : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Customer customer;
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private OrderManager orderManager;

    [SerializeField] private GameObject recipeBubbleRoot; // RecipeBubbleRoot
    [SerializeField] private Image bubbleIcon;            // BubbleIcon (Image)


    [Header("Overhead UI (under MoodUI)")]
    [SerializeField] private GameObject moodIconRoot;   // MoodIcon (или родитель)
    [SerializeField] private Button acceptButton;       // AcceptButton
    [SerializeField] private Image orderIcon;           // OrderIcon

    [Header("Order playback")]
    [SerializeField] private float iconDuration = 1f;

    [Header("Sprites by IngredientType index")]
    [Tooltip("Индекс = (int)IngredientType. Положи спрайты в том же порядке, что в enum IngredientType.")]
    [SerializeField] private Sprite[] ingredientSprites;

    [Header("Optional: HUD on player (recipe list)")]
    [SerializeField] private RecipeHUDUI recipeHud; // можно пока не заполнять

    private Coroutine playbackCo;

    private void Awake()
    {
        if (customer == null) customer = GetComponent<Customer>();
        if (customerManager == null) customerManager = FindObjectOfType<CustomerManager>();
        if (orderManager == null) orderManager = FindObjectOfType<OrderManager>();
        if (recipeHud == null) recipeHud = FindObjectOfType<RecipeHUDUI>(true);

        if (recipeBubbleRoot == null)
        {
            var t = transform.Find("MoodUI/RecipeBubbleRoot");
            if (t != null) recipeBubbleRoot = t.gameObject;
        }

        if (bubbleIcon == null)
        {
            var iconT = transform.Find("MoodUI/RecipeBubbleRoot/BubbleIcon");
            if (iconT != null) bubbleIcon = iconT.GetComponent<UnityEngine.UI.Image>();
        }

        if (recipeBubbleRoot != null) recipeBubbleRoot.SetActive(false);

        if (moodIconRoot == null)
        {
            var t = transform.Find("MoodUI/MoodIcon");
            if (t != null) moodIconRoot = t.gameObject;
        }

        // ? гарантия: эмоция всегда включена
        if (moodIconRoot != null) moodIconRoot.SetActive(true);
    }

    public void OnCustomerLeaving()
    {
        if (recipeHud != null)
            recipeHud.HideHUD();
    }



    private IEnumerator PlayDictation(List<IngredientType> recipe)
    {
        if (recipeHud != null)
        {
            recipeHud.ShowHUD();
            recipeHud.ShowProgress(recipe, 0, ingredientSprites); // сначала пустой/0 элементов
        }

        if (recipe == null || recipe.Count == 0) yield break;

        if (recipeBubbleRoot != null) recipeBubbleRoot.SetActive(true);

        for (int i = 0; i < recipe.Count; i++)
        {
            if (bubbleIcon != null)
                bubbleIcon.sprite = GetSprite(recipe[i]);
                bubbleIcon.GetComponent<AutoAspectImage>()?.Apply();
            if (recipeHud != null)
                recipeHud.ShowProgress(recipe, i + 1, ingredientSprites);

            yield return new WaitForSeconds(iconDuration);
        }

        if (recipeBubbleRoot != null) recipeBubbleRoot.SetActive(false);

        customer.ResetPatienceAfterDictation();
    }


    // Вызываем когда клиент РЕАЛЬНО дошёл до кассы
    public void OnReachedCashier()
    {
        if (customerManager == null || orderManager == null || customer == null) return;
        if (!customerManager.CanAcceptOrder()) return;

        var accepted = customerManager.AcceptNextCustomer();
        if (accepted != customer) return;

        orderManager.StartNewOrder();
        var recipe = orderManager.GetCurrentRecipeCopy();

        // если хочешь — HUD можно запустить прогрессом, но не обязательно
        // recipeHud.ShowProgress(recipe, 0, ingredientSprites);

        if (playbackCo != null) StopCoroutine(playbackCo);
        playbackCo = StartCoroutine(PlayDictation(recipe));
    }



    // Вызывается когда этого клиента назначили ActiveCustomer
    public void OnOrderAccepted()
    {
        // кнопку точно скрываем
        SetAcceptVisible(false);
    }

    private void OnAcceptClicked()
    {
        if (customerManager == null || orderManager == null || customer == null) return;

        // Кнопка должна работать только если:
        // - этот клиент стоит у кассы
        // - активного клиента ещё нет
        if (!customer.IsStandingAtCashier() || !customerManager.CanAcceptOrder())
            return;

        // Назначаем активного клиента (должен стать именно этот)
        var accepted = customerManager.AcceptNextCustomer();
        if (accepted != customer) return;

        // Генерим заказ
        orderManager.StartNewOrder();

        // Забираем рецепт и:
        // 1) показываем справа сверху (HUD), если подключен
        // 2) проигрываем по 1 секунде над головой
        var recipe = orderManager.GetCurrentRecipeCopy();
        if (recipeHud != null)
            recipeHud.Show(recipe, ingredientSprites);

        if (playbackCo != null) StopCoroutine(playbackCo);
        playbackCo = StartCoroutine(PlayIcons(recipe));
    }

    private IEnumerator PlayIcons(List<IngredientType> recipe)
{
    if (recipe == null || recipe.Count == 0) yield break;

    // Пауза терпения на время диктовки (см. пункт 3)
    customer.SetMoodPaused(true);

    SetOrderIconVisible(true);

    for (int i = 0; i < recipe.Count; i++)
    {
        // над головой
        orderIcon.sprite = GetSprite(recipe[i]);

        // ? на HUD — прогресс (первые i+1 ингредиентов)
        if (recipeHud != null)
            recipeHud.ShowProgress(recipe, i + 1, ingredientSprites);

        yield return new WaitForSeconds(iconDuration);
    }

    SetOrderIconVisible(false);

    // вернуть эмоцию + сброс терпения
    if (moodIconRoot != null) moodIconRoot.SetActive(true);

    customer.ResetPatienceAfterDictation();
    customer.SetMoodPaused(false);
}


    private Sprite GetSprite(IngredientType type)
    {
        int idx = (int)type;
        if (ingredientSprites == null) return null;
        if (idx < 0 || idx >= ingredientSprites.Length) return null;
        return ingredientSprites[idx];
    }

    private void SetAcceptVisible(bool v)
    {
        if (acceptButton != null)
            acceptButton.gameObject.SetActive(v);
    }

    private void SetOrderIconVisible(bool v)
    {
        if (orderIcon != null)
            orderIcon.gameObject.SetActive(v);
    }
}
