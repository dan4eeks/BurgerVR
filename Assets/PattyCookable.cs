using UnityEngine;

public class PattyCookable : MonoBehaviour
{
    // ?? Глобальное событие: прозвучал "alarm beep" от сгоревшей котлеты
    public static System.Action OnSmokeAlarmBeepGlobal;
    public static float CookTimeSeconds = 22f; // x по умолчанию

    [Header("Timing (seconds on grill)")]
    [SerializeField] private float timeToCooked = 30f;
    [SerializeField] private float timeToBurntAfterCooked = 15f;

    [Header("Visuals")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material rawMat;
    [SerializeField] private Material cookedMat;
    [SerializeField] private Material burntMat;

    [Header("Sizzle audio (loop while on grill)")]
    [SerializeField] private AudioSource sizzleSource;
    [SerializeField] private AudioClip sizzleLoopClip;
    [SerializeField] private float sizzleVolume = 0.6f;

    [Header("One-shot audio")]
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioClip cookedSfx;
    [SerializeField] private AudioClip burntSfx;
    [SerializeField] private float oneShotVolume = 0.9f;

    [Header("Burnt smoke")]
    [SerializeField] private ParticleSystem smokeParticles;
    [SerializeField] private bool smokeOnlyOnGrill = false;

    [Header("Smoke alarm (burnt only)")]
    [SerializeField] private AudioSource smokeAlarmSource;
    [SerializeField] private AudioClip smokeAlarmBeep;
    [SerializeField] private float smokeAlarmInterval = 2.5f;

    public PattyCookState State { get; private set; } = PattyCookState.Raw;

    private float grillTimer = 0f;

    // Надёжно определяем "на плите" даже если несколько коллайдеров
    private int grillContacts = 0;
    private bool onGrill = false;

    private float smokeAlarmTimer = 0f;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (sizzleSource == null)
            sizzleSource = GetComponent<AudioSource>();

        if (oneShotSource == null)
            oneShotSource = sizzleSource;

        ApplyVisual();
        UpdateSizzleAudio();
        UpdateSmoke();
    }

    private void OnDisable()
    {
        if (sizzleSource != null && sizzleSource.isPlaying)
            sizzleSource.Stop();

        smokeAlarmTimer = 0f;
    }

    private void Update()
    {
        if (onGrill)
        {
            grillTimer += Time.deltaTime;

            if (State == PattyCookState.Raw && grillTimer >= timeToCooked)
            {
                State = PattyCookState.Cooked;
                ApplyVisual();
                PlayOneShot(cookedSfx);
            }

            float burntTime = timeToCooked + timeToBurntAfterCooked;
            if (State == PattyCookState.Cooked && grillTimer >= burntTime)
            {
                State = PattyCookState.Burnt;
                ApplyVisual();
                PlayOneShot(burntSfx);
            }
        }

        UpdateSmoke();
        UpdateSmokeAlarm();
    }

    // Совместимость (если где-то ещё вызываешь старый метод)
    public void SetOnGrill(bool value)
    {
        if (value)
        {
            grillContacts = Mathf.Max(grillContacts, 1);
            onGrill = true;
        }
        else
        {
            grillContacts = 0;
            onGrill = false;
        }

        UpdateSizzleAudio();
        UpdateSmoke();
    }

    // Рекомендуемый путь (через GrillZone OnTriggerEnter/Exit)
    public void GrillContactEnter()
    {
        grillContacts++;
        if (grillContacts == 1)
        {
            onGrill = true;
            UpdateSizzleAudio();
            UpdateSmoke();
        }
    }

    public void GrillContactExit()
    {
        grillContacts = Mathf.Max(0, grillContacts - 1);
        if (grillContacts == 0)
        {
            onGrill = false;
            UpdateSizzleAudio();
            UpdateSmoke();
        }
    }

    // =========================
    // VISUALS
    // =========================
    private void ApplyVisual()
    {
        if (targetRenderer == null) return;

        Material m = rawMat;
        if (State == PattyCookState.Cooked) m = cookedMat;
        else if (State == PattyCookState.Burnt) m = burntMat;

        if (m != null)
            targetRenderer.sharedMaterial = m;
    }

    // =========================
    // SIZZLE AUDIO
    // =========================
    private void UpdateSizzleAudio()
    {
        if (sizzleSource == null) return;

        if (!onGrill)
        {
            if (sizzleSource.isPlaying)
                sizzleSource.Stop();
            return;
        }

        if (sizzleLoopClip != null)
        {
            if (sizzleSource.clip != sizzleLoopClip)
                sizzleSource.clip = sizzleLoopClip;

            sizzleSource.loop = true;
        }

        sizzleSource.volume = sizzleVolume;

        if (!sizzleSource.isPlaying)
            sizzleSource.Play();
    }

    // =========================
    // SMOKE
    // =========================
    private void UpdateSmoke()
    {
        if (smokeParticles == null) return;

        bool shouldSmoke =
            State == PattyCookState.Burnt &&
            (!smokeOnlyOnGrill || onGrill);

        if (shouldSmoke)
        {
            if (!smokeParticles.isPlaying)
                smokeParticles.Play();
        }
        else
        {
            if (smokeParticles.isPlaying)
                smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    // =========================
    // SMOKE ALARM
    // =========================
    private void UpdateSmokeAlarm()
    {
        if (State != PattyCookState.Burnt)
        {
            smokeAlarmTimer = 0f;
            return;
        }

        smokeAlarmTimer += Time.deltaTime;

        if (smokeAlarmTimer >= smokeAlarmInterval)
        {
            smokeAlarmTimer = 0f;

            if (smokeAlarmSource != null && smokeAlarmBeep != null)
            {
                smokeAlarmSource.PlayOneShot(smokeAlarmBeep);
            }

            // ?? событие паники
            OnSmokeAlarmBeepGlobal?.Invoke();
        }
    }

    // =========================
    // UTILS
    // =========================
    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;

        AudioSource src = oneShotSource != null ? oneShotSource : sizzleSource;
        if (src != null)
            src.PlayOneShot(clip, oneShotVolume);
    }
}
