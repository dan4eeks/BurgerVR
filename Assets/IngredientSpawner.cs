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

    [Header("Limit")]
    [SerializeField] private int maxAlive = 3;

    private float timer;
    private readonly List<Ingredient> alive = new List<Ingredient>();

    private void Start()
    {
        timer = 0f; // чтобы первый появился сразу
    }

    private void Update()
    {
        CleanupDestroyed();

        if (maxAlive <= 0) return;
        if (alive.Count >= maxAlive) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Spawn();
            timer = spawnIntervalSeconds;
        }
    }

    private void Spawn()
    {
        if (ingredientPrefab == null || spawnPoint == null)
        {
            Debug.LogError("IngredientSpawner: prefab or spawnPoint not assigned");
            return;
        }

        var ing = Instantiate(
            ingredientPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        alive.Add(ing);
    }

    // Опционально: если хочешь вручную освобождать слот, когда ингредиент взяли/выкинули/снапнули
    public void NotifyTakenOrTrashed(Ingredient ing)
    {
        if (ing == null) return;
        alive.Remove(ing);
    }

    // Удаляем из списка null (Unity делает ссылку null, если объект уничтожен)
    private void CleanupDestroyed()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null)
                alive.RemoveAt(i);
        }
    }

    // Удобно для UI/дебага
    public int AliveCount => alive.Count;
}
