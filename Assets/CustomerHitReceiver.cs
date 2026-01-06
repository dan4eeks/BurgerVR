using UnityEngine;

public class CustomerHitReceiver : MonoBehaviour
{
    [SerializeField] private Customer customer;

    [Header("Only allow hitting at cashier")]
    [SerializeField] private bool onlyAtCashier = true;

    [Header("Reaction")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private float smallShakeAngle = 12f;
    [SerializeField] private float shakeSpeed = 16f;

    private bool _reacting;

    private void Awake()
    {
        if (customer == null) customer = GetComponent<Customer>();
    }

    public void OnHit(Transform hand, float speed)
    {
        if (customer == null) return;

        // Разрешаем только по "вечно злому"
        if (!customer.alwaysAngry) return;

        // Опционально: только когда он у кассы
        if (onlyAtCashier && !IsAtCashier(customer)) return;

        // Комедийная реакция
        if (!_reacting)
            StartCoroutine(ReactCoroutine());

        if (audioSource != null && hitSfx != null)
            audioSource.PlayOneShot(hitSfx);

        // Геймплейный твист (по желанию):
        // после удара он "успокаивается" = дольше ждёт или не уходит так быстро
        // customer.AddPatienceBonus( ... ) — если добавишь такую механику
    }

    private bool IsAtCashier(Customer c)
    {
        // Быстрый способ без доступа к queueIndex:
        // считаем что "у кассы" = уже послан сигнал прибытия (cashierArrivedSent).
        // Но этот флаг private. Поэтому проще:
        // ? сделать в Customer публичный метод IsStandingAtCashier()
        return c.IsStandingAtCashier();
    }

    private System.Collections.IEnumerator ReactCoroutine()
    {
        _reacting = true;

        Quaternion startRot = transform.rotation;
        Quaternion left = startRot * Quaternion.Euler(0f, -smallShakeAngle, 0f);
        Quaternion right = startRot * Quaternion.Euler(0f, smallShakeAngle, 0f);

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime * shakeSpeed;
            transform.rotation = Quaternion.Slerp(left, right, Mathf.PingPong(t, 1f));
            yield return null;
        }

        transform.rotation = startRot;
        _reacting = false;
    }
}
