using UnityEngine;

public class HandHitDetector : MonoBehaviour
{
    [Header("Hit settings")]
    [SerializeField] private float minHitSpeed = 1.2f; // под VR подстроишь
    [SerializeField] private float cooldown = 0.25f;

    private Vector3 _prevPos;
    private float _cooldownTimer;

    private void Start()
    {
        _prevPos = transform.position;
        _cooldownTimer = 0f;
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;
        _prevPos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_cooldownTimer > 0f) return;

        var receiver = other.GetComponentInParent<CustomerHitReceiver>();
        if (receiver == null) return;

        float speed = GetHandSpeed();
        if (speed < minHitSpeed) return;

        _cooldownTimer = cooldown;
        receiver.OnHit(transform, speed);
    }

    private float GetHandSpeed()
    {
        // проста€ оценка скорости по перемещению за кадр
        // (лучше, чем rigidbody.velocity, потому что руки часто без rigidbody)
        // „тобы было честнее, можно хранить prevPos в LateUpdate, но и так ок.
        Vector3 delta = transform.position - _prevPos;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        return speed;
    }
}
