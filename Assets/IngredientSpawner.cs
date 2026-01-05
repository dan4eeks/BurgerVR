using UnityEngine;

public class IngredientSpawner : MonoBehaviour
{
    [Header("What to spawn")]
    [SerializeField] private Ingredient ingredientPrefab;

    [Header("Where to spawn")]
    [SerializeField] private Transform spawnPoint;

    [Header("Timing")]
    [SerializeField] private float spawnIntervalSeconds = 20f;

    private float timer;

    private void Start()
    {
        timer = spawnIntervalSeconds;
    }

    public void OnIngredientTaken()
{
    // теперь не нужен, но оставлен для совместимости
}


    private void Update()
    {
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

        Instantiate(
            ingredientPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );
    }
}
