using UnityEngine;

public class IngredientCondition : MonoBehaviour
{
    [Header("Dirty settings")]
    [SerializeField] private string floorTag = "Floor";
    [SerializeField] private Material dirtOverlayMaterial;
    [SerializeField] private Renderer targetRenderer;

    public bool IsDirty { get; private set; }

    private Material[] _originalMats;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer != null)
            _originalMats = targetRenderer.materials;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsDirty) return;

        // Если ударились об пол — становимся грязными
        if (collision.collider.CompareTag(floorTag))
        {
            MakeDirty();
        }
    }

    public void MakeDirty()
    {
        if (IsDirty) return;
        IsDirty = true;

        if (targetRenderer == null || dirtOverlayMaterial == null) return;

        // Добавляем материал-оверлей вторым слотом
        var mats = targetRenderer.materials;
        var newMats = new Material[mats.Length + 1];
        for (int i = 0; i < mats.Length; i++) newMats[i] = mats[i];
        newMats[newMats.Length - 1] = dirtOverlayMaterial;
        targetRenderer.materials = newMats;
    }

    public void Clean()
    {
        IsDirty = false;
        if (targetRenderer != null && _originalMats != null)
            targetRenderer.materials = _originalMats;
    }
}
