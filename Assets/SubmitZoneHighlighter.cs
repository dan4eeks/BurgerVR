using UnityEngine;

public class SubmitZoneHighlighter : MonoBehaviour
{
    [Header("Visual to toggle (recommended)")]
    [SerializeField] private GameObject highlightVisual; // например, GlowRing / подсвеченная рамка

    public void SetHighlight(bool on)
    {
        if (highlightVisual != null)
            highlightVisual.SetActive(on);
    }

    private void Awake()
    {
        // по умолчанию подсветка выключена
        SetHighlight(false);
    }
}
