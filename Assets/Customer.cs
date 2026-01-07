using UnityEngine;

public class Customer : MonoBehaviour
{
    [Header("Mood")]
    public CustomerMood mood = CustomerMood.Happy;
    [SerializeField] private CustomerMoodIcon moodIcon;

    [Tooltip("если true - клиент всегда злой (пока НЕ убегает панически)")]
    public bool alwaysAngry = false;

    [Header("Timing (seconds)")]
    public float happyDuration = 20f;
    public float neutralDuration = 20f;
    public float angryDuration = 15f; // сколько злой ДО ухода (с учётом mult)

    [Header("Audio")]
    [SerializeField] private AudioSource panicAudioSource;
    [SerializeField] private AudioClip panicClip;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParamName = "Speed";
    [Tooltip("Если слишком рано считает что 'стоит' — уменьши. Если дёргается — увеличь.")]
    [SerializeField] private float stopDistance = 0.02f;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 1.8f;
    [SerializeField] private float runSpeed = 5f;

    public float rotateSpeed = 720f;
    [SerializeField] private float faceTargetRotateSpeed = 720f;

    [Header("Look at player when arrived at cashier")]
    [SerializeField] private Transform playerLookTarget; // обычно XR Camera

    private float moodTimer = 0f;
    private bool isLeaving = false;
    private bool isPanicRunning = false;
    private bool isPanicking = false;


    private float currentSpeed;
    private int queueIndex = -1; // 0 = у кассы, 1+ = очередь

    private Transform targetPoint;
    private Transform exitPoint;
    private CustomerManager manager;

    private bool cashierArrivedSent = false;

    private int speedParamHash;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        speedParamHash = Animator.StringToHash(speedParamName);

        // Важно: чтобы анимации обновлялись даже когда персонаж "не видим" камерой (VR + bounds)
        if (animator != null)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    public void Init(CustomerManager mgr, Transform queuePoint, Transform exit, bool alwaysAngryFlag)
    {
        manager = mgr;
        exitPoint = exit;

        alwaysAngry = alwaysAngryFlag;
        mood = alwaysAngry ? CustomerMood.Angry : CustomerMood.Happy;

        moodTimer = 0f;
        isLeaving = false;
        isPanicRunning = false;

        currentSpeed = walkSpeed;

        cashierArrivedSent = false;

        if (playerLookTarget == null && Camera.main != null)
            playerLookTarget = Camera.main.transform;

        SetTarget(queuePoint);
        ApplyMoodVisual();

        SetAnimatorSpeed(0f);
    }

    public void OnQueueIndexChanged(int newIndex)
    {
        if (queueIndex == newIndex) return;

        queueIndex = newIndex;

        if (queueIndex == 0)
        {
            moodTimer = 0f;

            // у кассы снова happy (если не alwaysAngry и не паника)
            if (!alwaysAngry && !isPanicRunning)
                mood = CustomerMood.Happy;

            ApplyMoodVisual();

            cashierArrivedSent = false;
        }
    }

    public void SetTarget(Transform point)
    {
        targetPoint = point;
    }

    public void Leave()
    {
        if (isLeaving) return;

        isLeaving = true;
        isPanicRunning = false;
        isPanicking = false;

        // обычный уход — шагом
        currentSpeed = walkSpeed;

        cashierArrivedSent = true;
        SetTarget(exitPoint);
    }


    /// <summary>
    /// Панический побег => включаем Scared ТОЛЬКО тут
    /// </summary>
    public void PanicRunToExit()
    {
        if (isLeaving) return;

        isLeaving = true;
        isPanicRunning = true;
        isPanicking = true;

        // ?? СРАЗУ БЕЖИМ
        currentSpeed = runSpeed;

        mood = CustomerMood.Scared;
        ApplyMoodVisual();

        cashierArrivedSent = true;
        SetTarget(exitPoint);

        if (panicAudioSource != null && panicClip != null)
            panicAudioSource.PlayOneShot(panicClip);
    }


    public void ForceAngry()
    {
        if (isPanicRunning) return;
        if (alwaysAngry) return;

        mood = CustomerMood.Angry;
        ApplyMoodVisual();
    }

    private void Update()
    {
        MoveTowardsTarget();
        UpdateAnimatorSpeed();

        // дошёл до кассы
        if (!isLeaving && queueIndex == 0 && targetPoint != null && !cashierArrivedSent)
        {
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = targetPoint.position; b.y = 0f;

            if (Vector3.Distance(a, b) < 0.15f)
            {
                cashierArrivedSent = true;

                if (playerLookTarget != null)
                {
                    StopAllCoroutines();
                    StartCoroutine(RotateToFaceTransform(playerLookTarget));
                }

                manager?.NotifyCustomerArrivedAtCashier(this);
            }
        }

        // настроение обновляем только если НЕ уходит и НЕ в панике
        if (!isLeaving && !isPanicRunning)
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

        if (to.magnitude < stopDistance) return;

        transform.position += to.normalized * (currentSpeed * Time.deltaTime);

        if (to.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotateSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null) return;

        float value = 0f;

        if (targetPoint != null)
        {
            Vector3 to = targetPoint.position - transform.position;
            to.y = 0f;

            // пока реально идём/бежим — Speed > 0
            if (to.magnitude >= stopDistance)
                value = currentSpeed;
        }

        SetAnimatorSpeed(value);
    }

    private void SetAnimatorSpeed(float value)
    {
        if (animator == null) return;
        animator.SetFloat(speedParamHash, value);
    }

    private void UpdateMoodByTime()
    {
        float mult = (queueIndex >= 1) ? 2f : 1f;

        if (alwaysAngry)
        {
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

    // вызывается из CustomerManager.AcceptNextCustomer()
    public void OnOrderAccepted() { }

    // вызывается из CustomerOrderUI (после диктовки)
    public void ResetPatienceAfterDictation()
    {
        moodTimer = 0f;
    }

    public bool IsStandingAtCashier()
    {
        return !isLeaving && queueIndex == 0;
    }
}
