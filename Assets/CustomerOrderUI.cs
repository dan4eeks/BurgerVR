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

    [Header("Overhead UI (under MoodUI)")]
    [SerializeField] private Button acceptButton; // AcceptButton (Button)
    [SerializeField] private GameObject recipeBubbleRoot; // RecipeBubbleRoot (GameObject)
    [SerializeField] private Image bubbleIcon; // BubbleIcon (Image)

    [Header("Order playback")]
    [SerializeField] private float iconDuration = 1f;

    [Header("Sprites by IngredientType index")]
    [SerializeField] private Sprite[] ingredientSprites; // size = 8, index = (int)IngredientType

    [Header("Optional: HUD on player (recipe list)")]
    [SerializeField] private RecipeHUDUI recipeHud;

    private Coroutine playbackCo;

    private void Awake()
    {
        if (customer == null) customer = GetComponent<Customer>();
        if (customerManager == null) customerManager = FindObjectOfType<CustomerManager>();
        if (orderManager == null) orderManager = FindObjectOfType<OrderManager>();
        if (recipeHud == null) recipeHud = FindObjectOfType<RecipeHUDUI>(true);

        // авто-поиск UI по пут€м (чтобы не мучатьс€ перетаскиванием)
        if (acceptButton == null)
        {
            var t = transform.Find("MoodUI/AcceptButton");
            if (t != null) acceptButton = t.GetComponent<Button>();
        }

        if (recipeBubbleRoot == null)
        {
            var t = transform.Find("MoodUI/RecipeBubbleRoot");
            if (t != null) recipeBubbleRoot = t.gameObject;
        }

        if (bubbleIcon == null)
        {
            var t = transform.Find("MoodUI/RecipeBubbleRoot/BubbleIcon");
            if (t != null) bubbleIcon = t.GetComponent<Image>();
        }

        // стартовые состо€ни€
        SetAcceptVisible(false);
        SetBubbleVisible(false);

        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(OnAcceptClicked);
        }
    }

    private void OnEnable()
    {
        if (customerManager != null)
        {
            customerManager.OnCustomerArrivedAtCashier += HandleArrivedAtCashier;
            customerManager.OnActiveCustomerLeft += HandleActiveCustomerLeft;
        }
    }

    private void OnDisable()
    {
        if (customerManager != null)
        {
            customerManager.OnCustomerArrivedAtCashier -= HandleArrivedAtCashier;
            customerManager.OnActiveCustomerLeft -= HandleActiveCustomerLeft;
        }
    }

    // CustomerManager вызывает, когда конкретный клиент реально дошЄл до кассы (Q0)
    private void HandleArrivedAtCashier(Customer c)
    {
        if (customer == null) return;
        if (c != customer) return;

        // показываем кнопку только если он реально стоит у кассы
        if (!customer.IsStandingAtCashier()) return;

        SetAcceptVisible(true);
        SetBubbleVisible(false);
        // чек Ќ≈ показываем Ч он по€витс€ когда начнЄт диктовать
        if (recipeHud != null) recipeHud.HideHUD();
    }

    //  огда активный клиент ушЄл/заказ завершЄн/кто-то ушЄл злым Ч UI должен уйти в "ожидание"
    private void HandleActiveCustomerLeft()
    {
        // на вс€кий случай пр€чем кнопку у всех
        SetAcceptVisible(false);
        SetBubbleVisible(false);

        if (recipeHud != null) recipeHud.HideHUD();
    }

    private void OnAcceptClicked()
    {
        if (customer == null || customerManager == null || orderManager == null) return;

        // защита от повторных нажатий/не того клиента
        if (!customer.IsStandingAtCashier()) return;
        if (!customerManager.CanAcceptOrder()) return;

        // назначаем активного клиента
        var accepted = customerManager.AcceptNextCustomer();
        if (accepted != customer) return;

        // убираем кнопку, начинаем заказ
        SetAcceptVisible(false);

        orderManager.StartNewOrder();
        List<IngredientType> recipe = orderManager.GetCurrentRecipeCopy();

        // старт диктовки
        if (playbackCo != null) StopCoroutine(playbackCo);
        playbackCo = StartCoroutine(PlayDictation(recipe));
    }

    private IEnumerator PlayDictation(List<IngredientType> recipe)
    {
        if (recipe == null || recipe.Count == 0) yield break;

        // ? чек по€вл€етс€ только когда диктовка началась
        if (recipeHud != null)
        {
            recipeHud.ShowHUD();
            recipeHud.ShowProgress(recipe, 0, ingredientSprites);
        }

        SetBubbleVisible(true);

        for (int i = 0; i < recipe.Count; i++)
        {
            // bubble icon
            if (bubbleIcon != null)
                bubbleIcon.sprite = GetSprite(recipe[i]);

            // чек заполн€ем по секундам (как диктует)
            if (recipeHud != null)
                recipeHud.ShowProgress(recipe, i + 1, ingredientSprites);

            yield return new WaitForSeconds(iconDuration);
        }

        // bubble исчезает, чек остаЄтс€ пока клиент не уйдЄт (ты так хотел раньше)
        SetBubbleVisible(false);

        // сброс терпени€ после диктовки (если у теб€ есть этот метод)
        customer.ResetPatienceAfterDictation();
    }

    private Sprite GetSprite(IngredientType t)
    {
        int idx = (int)t;
        if (ingredientSprites == null) return null;
        if (idx < 0 || idx >= ingredientSprites.Length) return null;
        return ingredientSprites[idx];
    }

    private void SetAcceptVisible(bool on)
    {
        if (acceptButton != null)
            acceptButton.gameObject.SetActive(on);
    }

    private void SetBubbleVisible(bool on)
    {
        if (recipeBubbleRoot != null)
            recipeBubbleRoot.SetActive(on);
    }

        // Compatibility: Customer.cs still calls these
    public void OnOrderAccepted()
    {
        // Ќа вс€кий случай пр€чем кнопку, чтобы не нажимали повторно
        SetAcceptVisible(false);
    }

    public void OnCustomerLeaving()
    {
        //  лиент уходит -> пр€чем его UI и чек
        SetAcceptVisible(false);
        SetBubbleVisible(false);

        if (playbackCo != null)
        {
            StopCoroutine(playbackCo);
            playbackCo = null;
        }

        if (recipeHud != null)
            recipeHud.HideHUD();
    }

    // Compatibility: Customer.cs calls this when the customer reaches the cashier.
    // We only show the accept button here; dictation starts after clicking Accept.
    public void OnReachedCashier()
    {
        if (customer == null) return;

        // показываем кнопку только если реально у кассы
        if (!customer.IsStandingAtCashier()) return;

        SetAcceptVisible(true);
        SetBubbleVisible(false);

        if (recipeHud != null)
            recipeHud.HideHUD();
    }


}
