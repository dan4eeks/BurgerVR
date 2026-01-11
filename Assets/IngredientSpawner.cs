using System.Collections.Generic;
using UnityEngine;

public class IngredientSpawner : MonoBehaviour
{
    [Header("What to spawn")]
    [SerializeField] private Ingredient ingredientPrefab;

    [Header("Where to spawn")]
    [SerializeField] private Transform spawnPoint;

    [Header("Timing")]
    [SerializeField] private float spawnIntervalSeconds = 20f;

    [Header("Limit near spawner")]
    [SerializeField] private int maxAlive = 3;

    [Tooltip("≈сли ингредиент унесли дальше этой дистанции от spawnPoint Ч слот освобождаетс€")]
    [SerializeField] private float freeSlotDistance = 0.35f;

    private float timer;

    // считаем только то, что "занимает место" у спавнера
    private readonly List<Ingredient> alive = new List<Ingredient>();

    // чтобы не спамить одинаковыми ошибками каждый кадр
    private bool loggedMissingRefs;
    private bool loggedMaxAliveZero;

    private void Start()
    {
        timer = 0f; // первый спавн сразу
    }

    private void Update()
    {
        // 1) чистим список
        CleanupDestroyed();
        FreeMovedAway();

        // 2) проверки с пон€тными логами
        if (ingredientPrefab == null || spawnPoint == null)
        {
            if (!loggedMissingRefs)
            {
                Debug.LogError($"[{name}] IngredientSpawner: Ќ≈ назначен ingredientPrefab или spawnPoint.");
                loggedMissingRefs = true;
            }
            return;
        }
        loggedMissingRefs = false;

        if (maxAlive <= 0)
        {
            if (!loggedMaxAliveZero)
            {
                Debug.LogError($"[{name}] IngredientSpawner: maxAlive <= 0. —павн выключен (maxAlive={maxAlive}).");
                loggedMaxAliveZero = true;
            }
            return;
        }
        loggedMaxAliveZero = false;

        if (alive.Count >= maxAlive)
            return;

        // 3) таймер
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Spawn();
            timer = Mathf.Max(0.1f, spawnIntervalSeconds);
        }
    }

    private void Spawn()
    {
        var ing = Instantiate(ingredientPrefab, spawnPoint.position, spawnPoint.rotation);
        alive.Add(ing);
    }

    // если хочешь освобождать слот €вно при вз€тии/выбрасывании
    public void NotifyTakenOrTrashed(Ingredient ing)
    {
        if (ing == null) return;
        alive.Remove(ing);
    }

    private void CleanupDestroyed()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null)
                alive.RemoveAt(i);
        }
    }

    private void FreeMovedAway()
    {
        if (spawnPoint == null) return;

        for (int i = alive.Count - 1; i >= 0; i--)
        {
            var ing = alive[i];
            if (ing == null)
            {
                alive.RemoveAt(i);
                continue;
            }

            float d = Vector3.Distance(ing.transform.position, spawnPoint.position);
            if (d >= freeSlotDistance)
                alive.RemoveAt(i);
        }
    }

    // вызываетс€ из ShiftManager дл€ ускорени€ по дн€м
    public void ApplyDaySettings(float intervalSeconds, int newMaxAlive)
    {
        spawnIntervalSeconds = Mathf.Max(0.1f, intervalSeconds);
        maxAlive = Mathf.Max(0, newMaxAlive);
        timer = 0f; // применить сразу
    }

    public int AliveCount => alive.Count;
}
