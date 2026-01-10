using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

public class SetXROriginToSpawn : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    private XROrigin xrOrigin;

    private void Awake()
    {
        xrOrigin = GetComponent<XROrigin>();
    }

    private IEnumerator Start()
    {
        if (xrOrigin == null || spawnPoint == null)
        {
            Debug.LogError("SetXROriginToSpawn: не назначен XROrigin или SpawnPoint");
            yield break;
        }

        // Ждём 1 кадр, чтобы трекинг камеры успел выставить локальную позицию HMD
        yield return null;

        // 1) Ставим КАМЕРУ в точку спавна (компенсируя физический оффсет игрока)
        xrOrigin.MoveCameraToWorldLocation(spawnPoint.position);

        // 2) Поворачиваем риг по Y, чтобы смотреть туда же
        Vector3 e = transform.eulerAngles;
        e.y = spawnPoint.eulerAngles.y;
        transform.eulerAngles = e;
    }
}
