using UnityEngine;

public class SubmitZone : MonoBehaviour
{
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private SubmitZoneHighlighter highlighter;

    private void Awake()
    {
        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        if (highlighter == null)
            highlighter = GetComponent<SubmitZoneHighlighter>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Plate plate = other.GetComponentInParent<Plate>();
        if (plate == null) return;

        if (orderManager == null)
        {
            Debug.LogError("SubmitZone: OrderManager not found in scene!");
            return;
        }

        // ? сдаём заказ
        orderManager.Submit(plate);

        // ? и выключаем подсветку
        if (highlighter != null)
            highlighter.SetHighlight(false);
    }
}
