using System.Collections;
using UnityEngine;

public class Customer : MonoBehaviour
{
    [Header("Mood")]
    public CustomerMood mood = CustomerMood.Happy;
    [SerializeField] private CustomerMoodIcon moodIcon;

    [Tooltip(" true -    (   )")]
    public bool alwaysAngry = false;

    [Header("Timing (seconds)")]
    public float happyDuration = 60f;
    public float neutralDuration = 60f;
    public float angryDuration = 40f;

    [Header("Order Reaction")]
    public CustomerReactionState reactionState = CustomerReactionState.None;

    [Header("Cashier timing override (only when queueIndex == 0)")]
    [SerializeField] private bool useCashierTimings = true;
    [SerializeField] private float cashierHappyDuration = 30f;
    [SerializeField] private float cashierNeutralDuration = 30f;
    [SerializeField] private float cashierAngryDuration = 20f;

    [Header("Head interaction")]
    [SerializeField] private Collider headCollider;   // SphereCollider на HeadTarget
    [SerializeField] private GameObject headTargetGO;

    [Header("Audio")]
    [SerializeField] private AudioSource panicAudioSource;
    [SerializeField] private AudioClip panicClip;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParamName = "Speed";
    [Tooltip("     ''  .    .")]
    [SerializeField] private float stopDistance = 0.02f;

    [Header("Order Reaction Anim")]
    [SerializeField] private string thinkTrigger = "Think";
    [SerializeField] private string happyTrigger = "Happy";
    [SerializeField] private string neutralTrigger = "Neutral";
    [SerializeField] private string angryTrigger = "Angry";

    [Header("Reaction SFX")]
    [SerializeField] private AudioSource reactionAudioSource;
    [SerializeField] private AudioClip happyReactionClip;
    [SerializeField] private AudioClip neutralReactionClip;
    [SerializeField] private AudioClip angryReactionClip;

    [SerializeField] private float reactionVolume = 1f;


    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 7.5f;

    [Header("Cashier angry leave delay")]
    [SerializeField] private float cashierAngryLeaveDelay = 10f; //     ,   

    public float rotateSpeed = 720f;
    [SerializeField] private float faceTargetRotateSpeed = 720f;

    [Header("Look at player when arrived at cashier")]
    [SerializeField] private Transform playerLookTarget; //  XR Camera

    private float moodTimer = 0f;
    private bool isLeaving = false;
    private bool isPanicRunning = false;
    private bool isPanicking = false;

    private bool isReactingToOrder = false;
    public bool IsReactingToOrder => isReactingToOrder;

    private float currentSpeed;
    private int queueIndex = -1; // 0 =  , 1+ = 

    private Transform targetPoint;
    private Transform exitPoint;
    private CustomerManager manager;

    private bool cashierArrivedSent = false;

    private int speedParamHash;

    public float LastReactionDuration { get; private set; }

    // =========================
    // Death / Knockout (plate hit)
    // =========================
    [Header("Death")]
    [SerializeField] private string deadTrigger = "Click"; // если есть триггер в Animator (необязательно)
    [SerializeField] private string deadStateName = "Knocked Out"; // Имя STATE в Animator Controller
    [SerializeField] private float deathFallbackSeconds = 5.0f;

    public bool IsDead { get; private set; }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // стопаем логику/движение, чтобы он не бежал и не исчезал
        isLeaving = false;
        isPanicRunning = false;
        targetPoint = null;
        currentSpeed = 0f;
        SetAnimatorSpeed(0f);

        mood = CustomerMood.Dead;
        ApplyMoodVisual();

        if (animator != null && !string.IsNullOrEmpty(deadTrigger))
            animator.SetTrigger(deadTrigger);
    }

    /// Ждём завершения death-анимации (или fallback, если что-то не найдено)
    public IEnumerator WaitForDeathAnimation()
    {
        if (animator == null)
        {
            yield return new WaitForSeconds(deathFallbackSeconds);
            yield break;
        }

        // даём аниматору 1 кадр обработать Trigger
        yield return null;

        float start = Time.time;
        float maxWaitToEnter = Mathf.Max(0.25f, deathFallbackSeconds + 1.0f);

        // ждём входа в нужный state
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(deadStateName))
        {
            if (Time.time - start > maxWaitToEnter)
            {
                yield return new WaitForSeconds(deathFallbackSeconds);
                yield break;
            }
            yield return null;
        }

        // ждём, пока state проиграется до конца
        while (true)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);

            if (!st.IsName(deadStateName))
                break;

            if (st.normalizedTime >= 1f && !animator.IsInTransition(0))
                break;

            yield return null;
        }
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        speedParamHash = Animator.StringToHash(speedParamName);

        // :       " "  (VR + bounds)
        if (animator != null)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        
        if (headCollider == null)
            headCollider = GetComponentInChildren<SphereCollider>(true); // или найди точнее по имени

        if (headTargetGO == null && headCollider != null)
            headTargetGO = headCollider.gameObject;

        // ВАЖНО: по умолчанию выключаем всем
        ApplyHeadInteraction(false);
    }

    private void ApplyHeadInteraction(bool enabled)
    {
        if (headTargetGO != null) headTargetGO.SetActive(enabled);   // если хочешь убирать объект
        if (headCollider != null) headCollider.enabled = enabled;    // самое важное: физика
    }

    public void Init(CustomerManager mgr, Transform queuePoint, Transform exit, bool alwaysAngryFlag)
    {

        manager = mgr;
        exitPoint = exit;

        alwaysAngry = alwaysAngryFlag;
        ApplyHeadInteraction(alwaysAngry);

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

            //    happy (  alwaysAngry   )
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

        //    
        currentSpeed = walkSpeed;

        cashierArrivedSent = true;
        SetTarget(exitPoint);
    }


    /// <summary>
    ///   =>  Scared  
    /// </summary>
    public void PanicRunToExit()
    {
        if (isLeaving) return;

        isLeaving = true;
        isPanicRunning = true;
        isPanicking = true;

        // ??  
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

    // ? :    ,  timer  
    moodTimer = 0f;

    ApplyMoodVisual();
}


    private void Update()
    {

        if (IsDead) return;

        MoveTowardsTarget();
        UpdateAnimatorSpeed();

        //   
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

        if (!isLeaving && !isPanicRunning && !isReactingToOrder)
        {
            moodTimer += Time.deltaTime;
            UpdateMoodByTime();
        }


        //   
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

            //   /  Speed > 0
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
        if (IsDead) return;
        float happyT = happyDuration;
        float neutralT = neutralDuration;
        float angryT = angryDuration;

        //  :   Angry     angryT
        if (alwaysAngry)
        {
            //    ,    
            if (mood != CustomerMood.Angry)
            {
                mood = CustomerMood.Angry;
                ApplyMoodVisual();
            }

            float totalWait = happyT + neutralT + angryT;

            if (moodTimer >= totalWait)
            {
                Leave();
                manager?.OnCustomerLeftAngry(this);
            }
            return;
        }


        //  : Happy -> Neutral -> Angry -> 
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

    //   CustomerManager.AcceptNextCustomer()
    public void OnOrderAccepted() { }

    //   CustomerOrderUI ( )
    public void ResetPatienceAfterDictation()
    {
        //     ( ),
        //       .
        if (queueIndex >= 1)
            moodTimer = 0f;
    }


    public bool IsStandingAtCashier()
    {
        return !isLeaving && queueIndex == 0;
    }

    public void StartThinking()
    {
        isReactingToOrder = true;
        mood = CustomerMood.Thinking;
        ApplyMoodVisual();
        if (animator != null && !string.IsNullOrEmpty(thinkTrigger))
            animator.SetTrigger(thinkTrigger);
    }

    public void ApplyOrderResult(CustomerMood resultMood)
    {
        reactionState = CustomerReactionState.Result;

        mood = resultMood;
        ApplyMoodVisual();

        //  (    )
        if (animator != null)
        {
            string trig =
                resultMood == CustomerMood.Happy ? happyTrigger :
                resultMood == CustomerMood.Neutral ? neutralTrigger :
                resultMood == CustomerMood.Angry ? angryTrigger :
                null;

            if (!string.IsNullOrEmpty(trig))
                animator.SetTrigger(trig);
        }

        // ?? SFX 
        LastReactionDuration = 0f;

        if (reactionAudioSource != null)
        {
            AudioClip clip =
                resultMood == CustomerMood.Happy ? happyReactionClip :
                resultMood == CustomerMood.Neutral ? neutralReactionClip :
                resultMood == CustomerMood.Angry ? angryReactionClip :
                null;

            if (clip != null)
            {
                reactionAudioSource.Stop();
                reactionAudioSource.PlayOneShot(clip, reactionVolume);

                LastReactionDuration = clip.length; // ?   
            }
        }
    }

    // Called by CustomerManager when this customer moved closer to cashier by one position.
    public void OnAdvancedInQueue()
    {
        if (alwaysAngry) return;
        if (isPanicRunning) return;

        // If the customer is currently reacting or leaving, don't change mood.
        if (isLeaving || isReactingToOrder) return;

        switch (mood)
        {
            case CustomerMood.Angry:
                mood = CustomerMood.Neutral;
                break;
            case CustomerMood.Neutral:
                mood = CustomerMood.Happy;
                break;
            case CustomerMood.Happy:
                // stay happy
                break;
            default:
                // Scared or others: leave as is
                break;
        }

        ApplyMoodVisual();
    }
}
