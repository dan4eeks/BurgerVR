using System;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private float panicRunSpeedMult = 2.2f;

    [Header("Panic delay (seconds)")]
    [SerializeField] private float panicDelayMin = 0f;
    [SerializeField] private float panicDelayMax = 2f;

    private readonly List<Customer> queue = new List<Customer>();
    private float spawnTimer;

    private float panicTimer = 0f;

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

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            TrySpawnCustomer();
        }
    }

    private System.Collections.IEnumerator PanicAfterDelay(Customer c, float delay)
    {
        if (c == null) yield break;

        yield return new WaitForSeconds(delay);

        // Если клиента уже уничтожили/он ушёл — просто выходим
        if (c == null) yield break;

        c.PanicRunToExit(panicRunSpeedMult);
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

        Customer done = ActiveCustomer;
        ActiveCustomer = null;

        queue.Remove(done);

        if (!orderOk && done != null)
            done.ForceAngry();
        
        done?.Leave();

        ReassignQueueTargets();

        // UI -> ожидание до следующего "дошёл к кассе"
        OnActiveCustomerLeft?.Invoke();

    }

    public void OnCustomerLeftAngry(Customer c)
    {
        if (ActiveCustomer == c)
            ActiveCustomer = null;

        queue.Remove(c);
        ReassignQueueTargets();

        // ВАЖНО: UI всегда в "ожидание", а кнопку покажем только когда новый реально дошёл
        OnActiveCustomerLeft?.Invoke();
    }

    public void OnCustomerExited(Customer c)
    {
        // optional: оставить для совместимости/статистики
    }

    // =========================
    // PANIC FROM SMOKE ALARM
    // =========================
    private void OnSmokeAlarmBeep()
{
    if (panicTimer > 0f) return;
    panicTimer = panicCooldown;

    // Бежать некому, если 0-1 человек
    if (queue.Count <= 1) return;

    // Все, кто НЕ у кассы (индексы 1..), начинают паниковать через рандомную задержку
    for (int i = queue.Count - 1; i >= 1; i--)
    {
        Customer c = queue[i];
        if (c == null)
        {
            queue.RemoveAt(i);
            continue;
        }

        // ВАЖНО: убираем из очереди сразу, чтобы очередь сдвинулась,
        // но сам клиент начнёт бежать через delay
        queue.RemoveAt(i);

        float delay = UnityEngine.Random.Range(panicDelayMin, panicDelayMax);
        StartCoroutine(PanicAfterDelay(c, delay));
    }

    ReassignQueueTargets();

    if (queue.Count == 0)
        OnActiveCustomerLeft?.Invoke();
}

}
