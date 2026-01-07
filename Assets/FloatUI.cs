using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FloatRectUI : MonoBehaviour
{
    [SerializeField] private float amplitude = 10f; // в UI-пикселях (anchoredPosition)
    [SerializeField] private float frequency = 1.2f;

    private RectTransform rt;
    private Vector2 startAnchoredPos;

    private void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        startAnchoredPos = rt.anchoredPosition;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
        rt.anchoredPosition = startAnchoredPos + Vector2.up * offset;
    }

    private void OnDisable()
    {
        if (rt != null)
            rt.anchoredPosition = startAnchoredPos;
    }
}
