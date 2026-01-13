using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class CustomerManager : MonoBehaviour
{
    [Header("Customer prefab (GameObject). Must have Customer component on ROOT.")]
    [SerializeField] private GameObject customerPrefab;

    [Header("Spawn / Exit")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform exitPoint;

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip bellEnterClip;

    [Header("Queue points (0 = at cashier)")]
    [SerializeField] private Transform[] queuePoints;

    [Header("Spawn timing")]
    [SerializeField] private float spawnInterval = 25f;

    [Header("Always angry chance")]
    [Range(0f, 1f)]
    [SerializeField] private float alwaysAngryChance = 0.1f;

    [Header("Panic settings (smoke alarm)")]
    [SerializeField] private float panicCooldown = 6f;      // чтобы не срабатывать на каждый beep

    [Header("Panic delay (seconds)")]
    [SerializeField] private float panicDelayMin = 0f;
    [SerializeField] private float panicDelayMax = 1.5f;

    public event Action<Customer> OnCustomerGaveUpWaiting; // ушёл, не дождавшись
    public event Action<Customer> OnCustomerExitedEvent;   // реально вышел (уничтожился)

    private readonly HashSet<Customer> evacuating = new HashSet<Customer>();
    public bool IsEvacuationInProgress => evacuating.Count > 0;

    private readonly List<Customer> queue = new List<Customer>();
    private float spawnTimer;

    public bool SpawningEnabled { get; private set; } = true;

    private int spawnLimitThisShift = int.MaxValue;
    private int spawnedThisShift = 0;

    public int SpawnedThisShift => spawnedThisShift;

    // (опционально, если надо слушать из ShiftManager)
    public Action<Customer> OnCustomerSpawned;

    private float panicTimer = 0f;

    public void ConfigureShiftSpawning(int customersTarget, float newSpawnInterval)
    {
        spawnLimitThisShift = Mathf.Max(0, customersTarget);
        spawnedThisShift = 0;

        spawnInterval = Mathf.Max(0.1f, newSpawnInterval);
        spawnTimer = spawnInterval;

        SpawningEnabled = true;
    }

    public void PanicFromPlateHit(Customer victim)
    {
        // Собираем всех клиентов (как в OnSmokeAlarmBeep)
        List<Customer> toEvacuate = new List<Customer>();

        if (ActiveCustomer != null)
            toEvacuate.Add(ActiveCustomer);

        for (int i = 0; i < queue.Count; i++)
        {
            var c = queue[i];
            if (c != null && !toEvacuate.Contains(c))
                toEvacuate.Add(c);
        }

        // Убираем жертву из эвакуации
        if (victim != null)
            toEvacuate.Remove(victim);

        // Вычистим очередь/кассу
        if (victim != null)
        {
            if (ActiveCustomer == victim) ActiveCustomer = null;
            queue.Remove(victim);
        }

        queue.Clear();
        ActiveCustomer = null;

        OnActiveCustomerLeft?.Invoke();

        // Остальные бегут как при пожаре
        for (int i = 0; i < toEvacuate.Count; i++)
        {
            Customer c = toEvacuate[i];
            if (c == null) continue;
            if (c.IsDead) continue;

            evacuating.Add(c);

            float delay = UnityEngine.Random.Range(panicDelayMin, panicDelayMax);
            StartCoroutine(PanicAfterDelay(c, delay)); // внутри вызывает c.PanicRunToExit()
        }
    }

    public void SetSpawningEnabled(bool enabled)
    {
        SpawningEnabled = enabled;
    }

    public int QueueCount => queue.Count;
    public bool HasPanicRunningCustomerInQueue()
    {
        // В твоём Customer есть приватные флаги isPanicRunning/isPanicking,
        // поэтому напрямую проверить нельзя без правок Customer.
        // Сделаем по-другому: ShiftManager будет считать "паникой" именно факт SmokeAlarmBeep.
        return false;
    }

    public Customer ActiveCustomer { get; private set; }

    // ?? Клиент реально дошёл до кассы и стоит там
    public Action<Customer> OnCustomerArrivedAtCashier;

    // ?? Активный клиент ушёл / заказ завершён / кто-то ушёл из очереди — UI показать "ожидание"
    public Action OnActiveCustomerLeft;

    private void OnEnable()
    {
        PattyCookable.OnSmokeAlarmBeepGlobal += OnSmokeAlarmBeep;
    }

    private void OnDisable()
    {
        PattyCookable.OnSmokeAlarmBeepGlobal -= OnSmokeAlarmBeep;
    }

    private void Start()
    {
        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        if (panicTimer > 0f)
            panicTimer -= Time.deltaTime;

        if (!SpawningEnabled)
            return;

        if (spawnedThisShift >= spawnLimitThisShift)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            TrySpawnCustomer();
        }
    }


    public void ClearAllCustomers()
    {
        // Удаляем активного клиента у кассы
        if (ActiveCustomer != null)
        {
            Destroy(ActiveCustomer.gameObject);
            ActiveCustomer = null;
        }

        // Удаляем очередь
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] != null)
                Destroy(queue[i].gameObject);
        }

        queue.Clear();

        // (опционально, но полезно) сброс локальных таймеров
        spawnTimer = spawnInterval;
        panicTimer = 0f;

        // дергаем событие, чтобы UI/логика знали, что активного клиента больше нет
        OnActiveCustomerLeft?.Invoke();
    }

    private System.Collections.IEnumerator PanicAfterDelay(Customer c, float delay)
    {
        if (c == null) yield break;

        yield return new WaitForSeconds(delay);

        // Если клиента уже уничтожили/он ушёл — просто выходим
        if (c == null) yield break;
        c.PanicRunToExit();

    }


    private void TrySpawnCustomer()
    {
        if (customerPrefab == null)
        {
            Debug.LogError("CustomerManager: customerPrefab is NULL (assign CustomerPrefab)!");
            return;
        }
        if (spawnPoint == null)
        {
            Debug.LogError("CustomerManager: spawnPoint is NULL (assign CustomerSpawnPoint)!");
            return;
        }
        if (exitPoint == null)
        {
            Debug.LogError("CustomerManager: exitPoint is NULL (assign CustomerExitPoint)!");
            return;
        }
        if (queuePoints == null || queuePoints.Length == 0)
        {
            Debug.LogError("CustomerManager: queuePoints is empty (assign Q0,Q1,Q2...)!");
            return;
        }
        if (queue.Count >= queuePoints.Length)
            return;

        GameObject go = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);

        if (uiAudioSource != null && bellEnterClip != null)
            uiAudioSource.PlayOneShot(bellEnterClip);

        Customer c = go.GetComponent<Customer>();
        if (c == null)
        {
            Debug.LogError("CustomerManager: spawned prefab has NO Customer component on ROOT!");
            Destroy(go);
            return;
        }

        queue.Add(c);

        int myIndex = queue.Count - 1;
        bool alwaysAngry = (UnityEngine.Random.value < alwaysAngryChance);

        c.Init(this, queuePoints[myIndex], exitPoint, alwaysAngry);

        spawnedThisShift++;
        OnCustomerSpawned?.Invoke(c);

        ReassignQueueTargets();
    }

    private void ReassignQueueTargets()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            if (i >= queuePoints.Length) continue;
            if (queue[i] == null) continue;

            queue[i].SetTarget(queuePoints[i]);
            queue[i].OnQueueIndexChanged(i);
            // ?? НЕ вызываем событие здесь — оно придёт из Customer, когда он реально дошёл
        }
    }

    /// <summary>Customer вызывает это один раз, когда реально дошёл до Q0 и стоит там.</summary>
    public void NotifyCustomerArrivedAtCashier(Customer c)
    {
        if (c == null) return;
        if (queue.Count == 0) return;

        if (queue[0] == c && ActiveCustomer == null)
            OnCustomerArrivedAtCashier?.Invoke(c);
    }

    public bool CanAcceptOrder()
    {
        return queue.Count > 0 && ActiveCustomer == null;
    }

    public Customer AcceptNextCustomer()
    {
        if (!CanAcceptOrder()) return null;

        ActiveCustomer = queue[0];
        ActiveCustomer?.OnOrderAccepted();
        return ActiveCustomer;
    }

    public void CompleteActiveCustomer(bool orderOk)
    {
        if (ActiveCustomer == null) return;

        // ? снимок очереди ДО изменений
        var oldQueue = new System.Collections.Generic.List<Customer>(queue);

        Customer done = ActiveCustomer;
        ActiveCustomer = null;

        queue.Remove(done);

        if (!orderOk && done != null)
            done.ForceAngry();

        done?.Leave();

        // ? переназначили позиции
        ReassignQueueTargets();

        // ? после переназначения — поднимаем настроение тем, кто стал ближе к кассе
        BoostMoodForCustomersWhoMovedForward(oldQueue);

        // UI -> ожидание до следующего "дошёл к кассе"
        OnActiveCustomerLeft?.Invoke();
    }

    private void BoostMoodForCustomersWhoMovedForward(List<Customer> oldQueue)
    {
        for (int newIndex = 0; newIndex < queue.Count; newIndex++)
        {
            Customer c = queue[newIndex];
            if (c == null) continue;

            int oldIndex = oldQueue.IndexOf(c);
            if (oldIndex < 0) continue;

            int moved = oldIndex - newIndex;
            if (moved <= 0) continue;

            for (int i = 0; i < moved; i++)
                c.OnAdvancedInQueue(); // ? ВОТ ОН
        }
    }    

    public void OnCustomerLeftAngry(Customer c)
    {
        OnCustomerGaveUpWaiting?.Invoke(c);

        if (ActiveCustomer == c)
            ActiveCustomer = null;

        queue.Remove(c);
        ReassignQueueTargets();

        OnActiveCustomerLeft?.Invoke();
    }


    public void OnCustomerExited(Customer c)
    {
        OnCustomerExitedEvent?.Invoke(c);
    }


    // =========================
    // PANIC FROM SMOKE ALARM
    // =========================
    void OnSmokeAlarmBeep()
    {
        if (panicTimer > 0f) return;
        panicTimer = panicCooldown;

        FindObjectOfType<ShiftManager>()?.StartFireIncidentGraceTimer();
        
        // Собираем всех текущих клиентов (включая кассу)
        List<Customer> toEvacuate = new List<Customer>();

        if (ActiveCustomer != null)
            toEvacuate.Add(ActiveCustomer);

        for (int i = 0; i < queue.Count; i++)
        {
            var c = queue[i];
            if (c != null && !toEvacuate.Contains(c))
                toEvacuate.Add(c);
        }

        // Очищаем очередь и активного
        queue.Clear();
        ActiveCustomer = null;

        // Сразу говорим UI/логике: активного больше нет
        OnActiveCustomerLeft?.Invoke();

        // Запускаем эвакуацию
        for (int i = 0; i < toEvacuate.Count; i++)
        {
            Customer c = toEvacuate[i];
            if (c == null) continue;

            // помечаем, что ждём его ухода
            evacuating.Add(c);

            // можно дать небольшую рандомную задержку, чтобы смотрелось естественно
            float delay = UnityEngine.Random.Range(panicDelayMin, panicDelayMax);
            StartCoroutine(PanicAfterDelay(c, delay));
        }
    }

    public IEnumerator WaitForEvacuation(float timeoutSeconds = 10f)
    {
        float t = 0f;

        while (evacuating.Count > 0 && t < timeoutSeconds)
        {
            // чистим случайные null (если кого-то уничтожили иначе)
            evacuating.RemoveWhere(x => x == null);

            t += Time.deltaTime;
            yield return null;
        }

        // финальная очистка
        evacuating.RemoveWhere(x => x == null);
    }

}
