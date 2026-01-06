using UnityEngine;

public class Customer : MonoBehaviour
{
    [Header("Mood")]
    public CustomerMood mood = CustomerMood.Happy;
    [SerializeField] private CustomerMoodIcon moodIcon;

    [Tooltip("если true - клиент всегда злой")]
    public bool alwaysAngry = false;

    [Header("Timing (seconds)")]
    public float happyDuration = 20f;
    public float neutralDuration = 20f;
    public float angryDuration = 15f; // для обычного: после angryDuration уходит

    [Header("Movement")]
    public float moveSpeed = 1.2f;
    public float rotateSpeed = 720f;
    [SerializeField] private float faceTargetRotateSpeed = 720f;

    [Header("Look at player when arrived at cashier")]
    [SerializeField] private Transform playerLookTarget; // обычно XR Camera

    private float moodTimer = 0f;
    private bool isLeaving = false;

    private int queueIndex = -1; // 0 = у кассы, 1+ = очередь

    private Transform targetPoint;
    private Transform exitPoint;
    private CustomerManager manager;

    private bool cashierArrivedSent = false;

    public void Init(CustomerManager mgr, Transform queuePoint, Transform exit, bool alwaysAngryFlag)
    {
        manager = mgr;
        exitPoint = exit;

        alwaysAngry = alwaysAngryFlag;
        mood = alwaysAngry ? CustomerMood.Angry : CustomerMood.Happy;

        moodTimer = 0f;
        isLeaving = false;

        cashierArrivedSent = false;

        if (playerLookTarget == null && Camera.main != null)
            playerLookTarget = Camera.main.transform;

        SetTarget(queuePoint);
        ApplyMoodVisual();
    }

    public void OnQueueIndexChanged(int newIndex)
    {
        if (queueIndex == newIndex) return;

        queueIndex = newIndex;

        if (queueIndex == 0)
        {
            // сброс таймера всем (важно для alwaysAngry, чтобы не "сгорал" мгновенно)
            moodTimer = 0f;

            // обычный у кассы снова счастливый
            if (!alwaysAngry)
                mood = CustomerMood.Happy;

            ApplyMoodVisual();

            // разрешаем отправку "дошёл до кассы"
            cashierArrivedSent = false;
        }
    }

    public void SetTarget(Transform point)
    {
        targetPoint = point;
    }

    public void OnOrderAccepted()
    {
        // можно расширить позже
    }

    public void Leave()
    {
        if (isLeaving) return;
        isLeaving = true;

        cashierArrivedSent = true; // чтобы UI не показывал кнопку из-за этого клиента
        SetTarget(exitPoint);
    }

    /// <summary>
    /// Панический побег (для механики alarm).
    /// </summary>
    public void PanicRunToExit(float speedMultiplier = 2.2f)
    {
        if (isLeaving) return;

        isLeaving = true;
        cashierArrivedSent = true;

        moveSpeed *= speedMultiplier;

        // Можно усилить визуально:
        // mood = CustomerMood.Angry; ApplyMoodVisual();

        SetTarget(exitPoint);
    }

    public bool IsStandingAtCashier()
    {
        return !isLeaving && queueIndex == 0;
    }

    private void Update()
    {
        MoveTowardsTarget();

        // дошёл до кассы: только тогда показываем кнопку
        if (!isLeaving && queueIndex == 0 && targetPoint != null && !cashierArrivedSent)
        {
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = targetPoint.position; b.y = 0f;

            if (Vector3.Distance(a, b) < 0.15f)
            {
                cashierArrivedSent = true;

                // Повернуться к игроку
                if (playerLookTarget != null)
                {
                    StopAllCoroutines();
                    StartCoroutine(RotateToFaceTransform(playerLookTarget));
                }

                manager?.NotifyCustomerArrivedAtCashier(this);
            }
        }

        if (!isLeaving)
        {
            moodTimer += Time.deltaTime;
            UpdateMoodByTime();
        }

        // дошёл до выхода
        if (isLeaving && targetPoint != null && Vector3.Distance(transform.position, targetPoint.position) < 0.15f)
        {
            manager?.OnCustomerExited(this);
            Destroy(gameObject);
        }
    }

    private void MoveTowardsTarget()
    {
        if (targetPoint == null) return;

        Vector3 to = targetPoint.position - transform.position;
        to.y = 0f;

        if (to.magnitude < 0.02f) return;

        transform.position += to.normalized * (moveSpeed * Time.deltaTime);

        if (to.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotateSpeed * Time.deltaTime);
        }
    }

    private void UpdateMoodByTime()
    {
        // если стоит в очереди НЕ первым — настроение портится в 2 раза медленнее
        float mult = (queueIndex >= 1) ? 2f : 1f;

        if (alwaysAngry)
        {
            // ВЕЧНО злой, но таймер ожидания как у обычного:
            // обычный уходит после Happy + Neutral + Angry (с учётом mult)
            float totalWait = (happyDuration + neutralDuration + angryDuration) * mult;

            if (moodTimer >= totalWait)
            {
                Leave();
                manager?.OnCustomerLeftAngry(this);
            }
            return;
        }

        float happyT = happyDuration * mult;
        float neutralT = neutralDuration * mult;
        float angryT = angryDuration * mult;

        if (mood == CustomerMood.Happy && moodTimer >= happyT)
        {
            mood = CustomerMood.Neutral;
            moodTimer = 0f;
            ApplyMoodVisual();
            return;
        }

        if (mood == CustomerMood.Neutral && moodTimer >= neutralT)
        {
            mood = CustomerMood.Angry;
            moodTimer = 0f;
            ApplyMoodVisual();
            return;
        }

        if (mood == CustomerMood.Angry && moodTimer >= angryT)
        {
            Leave();
            manager?.OnCustomerLeftAngry(this);
        }
    }

    private void ApplyMoodVisual()
    {
        if (moodIcon == null)
            moodIcon = GetComponentInChildren<CustomerMoodIcon>();

        if (moodIcon != null)
            moodIcon.SetMood(mood);
    }

    private System.Collections.IEnumerator RotateToFaceTransform(Transform t)
    {
        if (t == null) yield break;

        while (true)
        {
            Vector3 dir = t.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f) yield break;

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

            if (Quaternion.Angle(transform.rotation, targetRot) < 1f)
                break;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                faceTargetRotateSpeed * Time.deltaTime
            );

            yield return null;
        }

        Vector3 finalDir = t.position - transform.position;
        finalDir.y = 0f;
        if (finalDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(finalDir.normalized, Vector3.up);
    }
}
