using UnityEngine;

public class Customer : MonoBehaviour
{
    [Header("Mood")]
    public CustomerMood mood = CustomerMood.Happy;
    [SerializeField] private CustomerMoodIcon moodIcon;

    [Tooltip("≈сли true Ч клиент всегда злой (10% шанс)")]
    public bool alwaysAngry = false;

    [Header("Timing (seconds)")]
    public float happyDuration = 20f;
    public float neutralDuration = 20f;
    public float angryDuration = 15f; // после этого уходит

    [Header("Movement")]
    public float moveSpeed = 1.2f;
    public float rotateSpeed = 720f;
    [SerializeField] private float faceTargetRotateSpeed = 720f;

    private float moodTimer = 0f;
    private bool isLeaving = false;
    
    private int queueIndex = -1; // 0 = у кассы, 1+ = стоит в очереди

    private Transform targetPoint;
    private Transform exitPoint;
    private CustomerManager manager;

    // ? ¬ј∆Ќќ: сигнатура Init совпадает с тем, как зовЄт CustomerManager
    public void Init(CustomerManager mgr, Transform queuePoint, Transform exit, bool alwaysAngryFlag)
    {
        manager = mgr;
        exitPoint = exit;

        alwaysAngry = alwaysAngryFlag;
        mood = alwaysAngry ? CustomerMood.Angry : CustomerMood.Happy;

        moodTimer = 0f;
        isLeaving = false;

        SetTarget(queuePoint);
        ApplyMoodVisual();
    }

    public void OnQueueIndexChanged(int newIndex)
{
    // если индекс не изменилс€ Ч ничего не делаем
    if (queueIndex == newIndex) return;

    queueIndex = newIndex;

    // “ребование: когда подошЄл к кассе Ч снова счастливый
    if (queueIndex == 0 && !alwaysAngry)
    {
        mood = CustomerMood.Happy;
        moodTimer = 0f;
        ApplyMoodVisual();
    }
}


    public void SetTarget(Transform point)
{
    targetPoint = point;

    // если это касса (Q0) Ч будем смотреть строго в еЄ rotation
    if (queueIndex == 0 && targetPoint != null)
    {
        StopAllCoroutines();
        StartCoroutine(RotateToTargetRotation(targetPoint.rotation));
    }
}


    public void OnOrderAccepted()
    {
        // —ейчас ничего не делаем.
        // ≈сли захочешь: ускор€ть/замедл€ть ухудшение настроени€ после прин€ти€.
    }

    public void Leave()
    {
        if (isLeaving) return;
        isLeaving = true;
        SetTarget(exitPoint);
    }

    private void Update()
    {
        MoveTowardsTarget();

        if (!isLeaving)
        {
            moodTimer += Time.deltaTime;
            UpdateMoodByTime();
        }

        // дошЄл до выхода
        if (isLeaving && targetPoint != null && Vector3.Distance(transform.position, targetPoint.position) < 0.15f)
        {
            if (manager != null)
                manager.OnCustomerExited(this);

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
    // если стоит в очереди Ќ≈ первым Ч настроение портитс€ в 2 раза медленнее
    float mult = (queueIndex >= 1) ? 2f : 1f;

    float happyT = happyDuration * mult;
    float neutralT = neutralDuration * mult;
    float angryT = angryDuration * mult;

    if (alwaysAngry)
    {
        // он всегда злой, но уйдЄт через angryT (тоже с учЄтом очереди)
        if (moodTimer >= angryT)
        {
            Leave();
            if (manager != null) manager.OnCustomerLeftAngry(this);
        }
        return;
    }

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
        if (manager != null) manager.OnCustomerLeftAngry(this);
    }
}


    private void ApplyMoodVisual()
{
    if (moodIcon == null)
        moodIcon = GetComponentInChildren<CustomerMoodIcon>();

    if (moodIcon != null)
        moodIcon.SetMood(mood);
}

private System.Collections.IEnumerator RotateToTargetRotation(Quaternion targetRot)
{
    while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            faceTargetRotateSpeed * Time.deltaTime
        );
        yield return null;
    }

    transform.rotation = targetRot;
}


}
