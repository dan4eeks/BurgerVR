using UnityEngine;

public class PlateSpawner : MonoBehaviour
{
    [SerializeField] private Plate platePrefab;
    [SerializeField] private Transform spawnPoint;

    private Plate currentPlate;

    private void Start()
    {
        SpawnPlate();
    }

    public void SpawnPlate()
    {
        if (platePrefab == null || spawnPoint == null)
        {
            Debug.LogError("PlateSpawner: Assign platePrefab and spawnPoint!");
            return;
        }

        if (currentPlate != null)
            Destroy(currentPlate.gameObject);

        currentPlate = Instantiate(
            platePrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );
    }

    public void OnPlateSubmitted()
    {
        SpawnPlate();
    }
}
