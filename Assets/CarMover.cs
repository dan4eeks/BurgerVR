using UnityEngine;

public class CarMover : MonoBehaviour
{
    [Header("Move")]
    public float speed = 6f;
    public float arriveDistance = 0.2f;

    private Vector3 target;
    private System.Action<CarMover> onArrived;
    private bool active;

    public void Activate(Vector3 startPos, Vector3 targetPos, Quaternion rotation, float moveSpeed, System.Action<CarMover> arrivedCallback)
    {
        transform.position = startPos;
        transform.rotation = rotation;

        target = targetPos;
        speed = moveSpeed;
        onArrived = arrivedCallback;

        active = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!active) return;

        Vector3 dir = (target - transform.position);
        float dist = dir.magnitude;

        if (dist <= arriveDistance)
        {
            active = false;
            onArrived?.Invoke(this);
            return;
        }

        Vector3 step = dir.normalized * (speed * Time.deltaTime);
        transform.position += step;
    }
}
