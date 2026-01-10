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
        Debug.Log("SubmitZone: OnTriggerEnter " + other.name);
        Plate plate = other.GetComponentInParent<Plate>();
        if (plate == null) return;
        Debug.Log("SubmitZone: plate detected " + plate.name);

        if (orderManager == null)
        {
            Debug.LogError("SubmitZone: OrderManager not found in scene!");
            return;
        }

        // если сабмит НЕ принят — тарелку не трогаем
        if (!orderManager.Submit(plate))
            return;

        // ? сабмит принят -> удаляем тарелку и спавним новую
        Destroy(plate.gameObject);
        orderManager.NotifyPlateSubmitted(); // сделаем ниже
    }
}
