using UnityEngine;
using TMPro;

public class CashRegisterUI : MonoBehaviour
{
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private OrderManager orderManager;

    [Header("UI")]
    [SerializeField] private GameObject acceptOrderButton;   // сам объект кнопки
    [SerializeField] private TMP_Text statusText;            // "Ожидание клиента..."

    [Header("Optional UI")]
    [SerializeField] private TMP_Text hintText;

    private void Awake()
    {
        if (customerManager == null) customerManager = FindObjectOfType<CustomerManager>();
        if (orderManager == null) orderManager = FindObjectOfType<OrderManager>();
    }

    private void OnEnable()
    {
        if (customerManager != null)
        {
            customerManager.OnCustomerArrivedAtCashier += OnCustomerArrivedAtCashier;
            customerManager.OnActiveCustomerLeft += ShowWaiting;
        }
    }

    private void OnDisable()
    {
        if (customerManager != null)
        {
            customerManager.OnCustomerArrivedAtCashier -= OnCustomerArrivedAtCashier;
            customerManager.OnActiveCustomerLeft -= ShowWaiting;
        }
    }

    private void Start()
    {
        ShowWaiting();
    }

    private void OnCustomerArrivedAtCashier(Customer c)
    {
        // Клиент реально стоит у кассы -> показываем кнопку
        if (acceptOrderButton != null) acceptOrderButton.SetActive(true);
        if (statusText != null) statusText.text = "";
        if (hintText != null) hintText.text = "";
    }

    public void ShowWaiting()
    {
        // Клиент идёт/уходит/нет клиента -> кнопки быть не должно
        if (acceptOrderButton != null) acceptOrderButton.SetActive(false);
        if (statusText != null) statusText.text = "Ожидание клиента...";
    }

    // Вешается в Button -> OnClick()
    public void AcceptOrderButton()
    {
        if (customerManager == null || orderManager == null) return;

        // Заказ можно принять только когда клиент стоит у кассы и ActiveCustomer ещё не назначен
        if (!customerManager.CanAcceptOrder())
        {
            if (hintText != null) hintText.text = "Нельзя принять заказ сейчас";
            return;
        }

        var customer = customerManager.AcceptNextCustomer();
        if (customer == null) return;

        // стартуем заказ: генерим рецепт и показываем
        orderManager.StartNewOrder();

        // кнопку скрываем до следующего клиента
        if (acceptOrderButton != null) acceptOrderButton.SetActive(false);
        if (hintText != null) hintText.text = "Заказ принят";
    }
}
