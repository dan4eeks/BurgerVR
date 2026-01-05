using UnityEngine;

public class SubmitZone : MonoBehaviour
{
    [SerializeField] private OrderManager orderManager;

    private void Awake()
    {
        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();
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

        orderManager.Submit(plate);
    }
}
