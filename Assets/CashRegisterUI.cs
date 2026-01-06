using UnityEngine;
using TMPro;

public class CashRegisterUI : MonoBehaviour
{
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private OrderManager orderManager;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text hintText;

    private void Awake()
    {
        if (customerManager == null) customerManager = FindObjectOfType<CustomerManager>();
        if (orderManager == null) orderManager = FindObjectOfType<OrderManager>();
    }

    public void AcceptOrderButton()
    {
        if (customerManager == null || orderManager == null) return;

        if (!customerManager.CanAcceptOrder())
        {
            if (hintText != null) hintText.text = "No customers / already cooking";
            return;
        }

        var customer = customerManager.AcceptNextCustomer();
        if (customer == null) return;

        // тут начинаем заказ: генерим рецепт и показываем на экране
        orderManager.StartNewOrder();

        if (hintText != null) hintText.text = "Order accepted!";
    }
}
