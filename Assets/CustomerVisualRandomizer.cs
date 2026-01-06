using UnityEngine;

public class CustomerVisualRandomizer : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private GameObject[] visualPrefabs;

    [Header("Optional")]
    [SerializeField] private bool randomYaw = true;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        ApplyRandomVisual();
    }

    public void ApplyRandomVisual()
    {
        if (visualPrefabs == null || visualPrefabs.Length == 0)
        {
            Debug.LogWarning("CustomerVisualRandomizer: no visualPrefabs assigned");
            return;
        }

        // очистим старый визуал (если есть)
        for (int i = visualRoot.childCount - 1; i >= 0; i--)
            Destroy(visualRoot.GetChild(i).gameObject);

        int idx = Random.Range(0, visualPrefabs.Length);
        GameObject visual = Instantiate(visualPrefabs[idx], visualRoot);

        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        if (randomYaw)
        {
            // иногда разные модели смотр€т по-разному Ч можно слегка рандомить или поправить позже
            // visualRoot.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }
}
