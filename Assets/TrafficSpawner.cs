using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Route
    {
        public string name;
        public Transform from;
        public Transform to;

        [Tooltip("Смещение полосы вправо/влево относительно направления движения")]
        public float laneOffset = 0.9f;
    }

    [Header("Routes (A<->B, C<->D, E<->F, G<->H)")]
    public Route[] routes;

    [Header("Car Prefabs (must have CarMover on prefab)")]
    public CarMover[] carPrefabs;

    [Header("Spawn")]
    public float spawnIntervalMin = 2f;
    public float spawnIntervalMax = 5f;
    public float carSpeedMin = 5f;
    public float carSpeedMax = 10f;

    [Header("Pool")]
    public int prewarmCount = 8;

    [Header("Randomness")]
    public float spawnJitter = 0.2f;

    private readonly Queue<CarMover> pool = new Queue<CarMover>();
    private float timer;

    private void Start()
    {
        // Защита от твоей ошибки
        if (carPrefabs == null || carPrefabs.Length == 0)
        {
            Debug.LogError("[TrafficSpawner] Car Prefabs пустой! Добавь префабы машин в инспектор.");
            enabled = false;
            return;
        }

        if (routes == null || routes.Length == 0)
        {
            Debug.LogError("[TrafficSpawner] Routes пустой! Добавь пары точек (A-B, C-D, ...).");
            enabled = false;
            return;
        }

        PrewarmPool(prewarmCount);
        timer = Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        SpawnCarOnRandomRoute();
        timer = Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    private void PrewarmPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
            if (prefab == null) continue;

            var car = Instantiate(prefab, transform);
            car.gameObject.SetActive(false);
            pool.Enqueue(car);
        }
    }

    private void SpawnCarOnRandomRoute()
    {
        // выбираем маршрут
        Route r = routes[Random.Range(0, routes.Length)];
        if (r == null || r.from == null || r.to == null) return;

        // направление: true = from->to, false = to->from
        bool forward = Random.value > 0.5f;

        Vector3 start = forward ? r.from.position : r.to.position;
        Vector3 end   = forward ? r.to.position   : r.from.position;

        Vector3 dir = (end - start).normalized;

        // "Полоса": встречные потоки по разным сторонам
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        float signedLane = forward ? r.laneOffset : -r.laneOffset;

        start += right * signedLane;
        end   += right * signedLane;

        // небольшой джиттер
        start += right * Random.Range(-spawnJitter, spawnJitter);
        end   += right * Random.Range(-spawnJitter, spawnJitter);

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        CarMover car = GetFromPoolOrCreate();
        float spd = Random.Range(carSpeedMin, carSpeedMax);

        car.Activate(start, end, rot, spd, ReturnToPool);
    }

    private CarMover GetFromPoolOrCreate()
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        var prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
        var car = Instantiate(prefab, transform);
        car.gameObject.SetActive(false);
        return car;
    }

    private void ReturnToPool(CarMover car)
    {
        car.gameObject.SetActive(false);
        pool.Enqueue(car);
    }
}
