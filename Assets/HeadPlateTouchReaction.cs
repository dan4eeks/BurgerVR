using UnityEngine;

public class HeadPlateTouchReaction : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Click";

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 0.6f;

    private float lastTime;

    private void Awake()
    {
        Debug.Log($"[HeadPlate] Awake on {name}");

        if (animator == null)
        {
            // 1) пробуем в детях CustomerRoot (самый надёжный)
            var root = transform.root; // верхний объект инстанса клиента в сцене
            animator = root.GetComponentInChildren<Animator>(true);

            Debug.Log(animator
                ? $"[HeadPlate] Animator found in children of root: {animator.gameObject.name}"
                : "[HeadPlate] ? Animator NOT found in root children");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[HeadPlate] Trigger ENTER by: {other.name}");

        if (Time.time < lastTime + cooldown)
        {
            Debug.Log("[HeadPlate] Cooldown active, ignoring");
            return;
        }

        // ищем тарелку
        Plate plate = other.GetComponentInParent<Plate>();
        if (plate == null)
        {
            Debug.Log("[HeadPlate] Not a plate, ignoring");
            return;
        }

        Customer customer = GetComponentInParent<Customer>();
        if (customer == null) return;

        // ТОЛЬКО always angry (по твоей задумке)
        if (!customer.alwaysAngry) return;

        // 1) жертва -> dead
        customer.Die();
        var shift = FindObjectOfType<ShiftManager>();
        if (shift != null)
            shift.FailAfterCustomerDeath(customer, "Вы убили клиента!");

        // 2) все остальные -> scared + убегают (как пожар)
        CustomerManager mgr = FindObjectOfType<CustomerManager>();
        if (mgr != null)
            mgr.PanicFromPlateHit(customer);

        Debug.Log("[HeadPlate] ? Plate detected!");

        lastTime = Time.time;

        if (animator == null)
        {
            Debug.Log("[HeadPlate] ? Animator is NULL, cannot play animation");
            return;
        }

        Debug.Log($"[HeadPlate] ? SetTrigger('{triggerName}')");
        animator.SetTrigger(triggerName);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[HeadPlate] Trigger EXIT by: {other.name}");
    }
}
