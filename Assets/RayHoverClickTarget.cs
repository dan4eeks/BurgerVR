using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))]
public class RayHoverClickTarget : MonoBehaviour
{
    [Header("Ray color on hover")]
    public Color hoverColor = Color.white;

    [Header("Action on click (Select)")]
    public UnityEvent onClicked;

    [Header("Click animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string clickTrigger = "Click";

    private XRSimpleInteractable interactable;

    private Gradient cachedValid;
    private Gradient cachedInvalid;
    private bool cached;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);

        if (animator == null)
            animator = GetComponentInParent<Animator>();
    }

    private void OnDestroy()
    {
        if (interactable == null) return;

        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private XRInteractorLineVisual FindLineVisual(Transform t)
    {
        if (!t) return null;

        var lv = t.GetComponent<XRInteractorLineVisual>();
        if (lv) return lv;

        lv = t.GetComponentInChildren<XRInteractorLineVisual>(true);
        if (lv) return lv;

        return t.GetComponentInParent<XRInteractorLineVisual>();
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"HOVER ENTER on {gameObject.name} by {args.interactorObject.transform.name}");

        var line = FindLineVisual(args.interactorObject.transform);
        if (line == null)
        {
            Debug.LogWarning("No XRInteractorLineVisual found on this interactor (controller).");
            return;
        }

        if (!cached)
        {
            cachedValid = line.validColorGradient;
            cachedInvalid = line.invalidColorGradient;
            cached = true;
        }

        var g = MakeSolidGradient(hoverColor);

        // красим и valid, и invalid Ч чтобы точно увидеть эффект
        line.validColorGradient = g;
        line.invalidColorGradient = g;
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"HOVER EXIT on {gameObject.name} by {args.interactorObject.transform.name}");

        var line = FindLineVisual(args.interactorObject.transform);
        if (line == null) return;

        if (cached)
        {
            line.validColorGradient = cachedValid;
            line.invalidColorGradient = cachedInvalid;
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"SELECT (CLICK) on {gameObject.name} by {args.interactorObject.transform.name}");

        if (animator != null && !string.IsNullOrEmpty(clickTrigger))
            animator.SetTrigger(clickTrigger);

        onClicked?.Invoke();
    }

    private Gradient MakeSolidGradient(Color c)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) }
        );
        return g;
    }
}
