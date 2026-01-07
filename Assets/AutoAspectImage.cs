using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AutoAspectImage : MonoBehaviour
{
    [SerializeField] private AspectRatioFitter fitter;

    private Image img;

    private void Awake()
    {
        img = GetComponent<Image>();
        if (fitter == null) fitter = GetComponent<AspectRatioFitter>();
        Apply();
    }

    private void OnEnable() => Apply();

    public void Apply()
    {
        if (img == null || fitter == null || img.sprite == null) return;

        var r = img.sprite.rect;
        if (r.height <= 0) return;

        fitter.aspectRatio = r.width / r.height;
    }
}
