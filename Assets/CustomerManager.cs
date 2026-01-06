using System.Collections.Generic;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    [Header("Customer prefab (GameObject). Must have Customer component on ROOT.")]
    [SerializeField] private GameObject customerPrefab;

    [Header("Spawn / Exit")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform exitPoint;

    [Header("Queue points (0 = at cashier)")]
    [SerializeField] private Transform[] queuePoints;

    [Header("Spawn timing")]
    [SerializeField] private float spawnInterval = 25f;

    [Header("Always angry chance")]
    [Range(0f, 1f)]
    [SerializeField] private float alwaysAngryChance = 0.1f;

    private readonly List<Customer> queue = new List<Customer>();
    private float spawnTimer;

    public Customer ActiveCustomer { get; private set; }

    private void Start()
    {
        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            TrySpawnCustomer();
        }
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

        Customer c = go.GetComponent<Customer>();
        if (c == null)
        {
            Debug.LogError("CustomerManager: spawned prefab has NO Customer component on ROOT!");
            Destroy(go);
            return;
        }

        queue.Add(c);

        int myIndex = queue.Count - 1;
        bool alwaysAngry = (Random.value < alwaysAngryChance);

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
        queue[i].OnQueueIndexChanged(i); // ? вот оно
    }
}


    public bool CanAcceptOrder()
    {
        return queue.Count > 0 && ActiveCustomer == null;
    }

    public Customer AcceptNextCustomer()
    {
        if (!CanAcceptOrder()) return null;

        ActiveCustomer = queue[0];
        if (ActiveCustomer != null)
            ActiveCustomer.OnOrderAccepted();

        return ActiveCustomer;
    }

    public void CompleteActiveCustomer(bool orderOk)
    {
        if (ActiveCustomer == null) return;

        Customer done = ActiveCustomer;
        ActiveCustomer = null;

        queue.Remove(done);

        if (done != null)
            done.Leave();

        ReassignQueueTargets();
    }

    public void OnCustomerLeftAngry(Customer c)
    {
        if (ActiveCustomer == c) ActiveCustomer = null;

        queue.Remove(c);
        ReassignQueueTargets();
    }

    public void OnCustomerExited(Customer c)
    {
        // optional
    }
}
