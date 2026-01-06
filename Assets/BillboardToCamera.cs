using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    [SerializeField] private bool lockPitch = true;
    private Transform cam;

    private void Awake()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main != null) cam = Camera.main.transform;
            else return;
        }

        Vector3 dir = transform.position - cam.position;

        if (lockPitch)
            dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir);
    }
}
