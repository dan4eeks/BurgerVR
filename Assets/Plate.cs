using System.Collections.Generic;
using UnityEngine;

public class Plate : MonoBehaviour
{
    public Transform buildSpot;

    // Порядок ингредиентов
    public List<IngredientType> Stack = new List<IngredientType>();

    // Прожарка котлет (по порядку добавления котлет)
    public List<PattyCookState> PattyStates = new List<PattyCookState>();

    // Грязь каждого добавленного ингредиента (по порядку Stack)
    public List<bool> DirtyFlags = new List<bool>();

    public float defaultLayerHeight = 0.03f;

    private float currentHeight = 0f;

    private void Awake()
    {
        EnsureBuildSpot();
        currentHeight = 0f;
    }

    private void EnsureBuildSpot()
    {
        if (buildSpot != null) return;

        Transform found = transform.Find("BuildSpot");
        if (found != null)
        {
            buildSpot = found;
            return;
        }

        GameObject go = new GameObject("BuildSpot");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        buildSpot = go.transform;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Ingredient ing = collision.collider.GetComponentInParent<Ingredient>();
        if (ing == null) return;

        AddIngredient(ing);
    }

    public void AddIngredient(Ingredient ing)
    {
        if (ing == null || ing.snapped) return;
        EnsureBuildSpot();

        ing.snapped = true;

        Transform t = ing.transform;
        t.SetParent(buildSpot, true);
        t.localRotation = Quaternion.identity;
        t.localPosition = new Vector3(0f, currentHeight, 0f);

        // делаем "одно целое" с тарелкой (XR-safe)
        var grab = ing.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
        if (grab != null) grab.enabled = false; // больше нельзя взять

        Rigidbody rb = ing.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider col = ing.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;


        // 1) Тип ингредиента
        Stack.Add(ing.type);

        // 2) Грязь ингредиента (если нет IngredientCondition — считаем чистым)
        bool isDirty = false;
        IngredientCondition cond = ing.GetComponentInParent<IngredientCondition>();
        if (cond != null) isDirty = cond.IsDirty;
        DirtyFlags.Add(isDirty);

        // 3) Если котлета — сохраняем степень прожарки в момент добавления
        if (ing.type == IngredientType.Patty)
        {
            PattyCookable cookable = ing.GetComponentInParent<PattyCookable>();
            PattyCookState state = cookable != null ? cookable.State : PattyCookState.Raw;
            PattyStates.Add(state);
        }

        float h = ing.layerHeight > 0f ? ing.layerHeight : defaultLayerHeight;
        currentHeight += h;
    }

    public void ClearPlate()
    {
        EnsureBuildSpot();

        for (int i = buildSpot.childCount - 1; i >= 0; i--)
            Destroy(buildSpot.GetChild(i).gameObject);

        Stack.Clear();
        PattyStates.Clear();
        DirtyFlags.Clear();
        currentHeight = 0f;
    }
}
