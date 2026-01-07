using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FloatingUIRect : MonoBehaviour
{
    [Header("Float")]
    [SerializeField] private float amplitude = 8f; // попробуй 10Ц20
    [SerializeField] private float speed = 2f;

    private RectTransform rt;
    private Vector2 startAnchoredPos;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        startAnchoredPos = rt.anchoredPosition;
    }

    private void LateUpdate()
    {
        float offset = Mathf.Sin(Time.unscaledTime * speed) * amplitude;
        rt.anchoredPosition = startAnchoredPos + Vector2.up * offset;
    }

    private void OnDisable()
    {
        rt.anchoredPosition = startAnchoredPos;
    }
}
