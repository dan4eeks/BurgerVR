using System.Collections.Generic;
using UnityEngine;

public class Plate : MonoBehaviour
{
    public Transform buildSpot;
    public List<IngredientType> Stack = new List<IngredientType>();

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

        // делаем "одно целое" с тарелкой
        Rigidbody rb = ing.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        Collider col = ing.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        Stack.Add(ing.type);

        float h = ing.layerHeight > 0f ? ing.layerHeight : defaultLayerHeight;
        currentHeight += h;
    }

    public void ClearPlate()
    {
        EnsureBuildSpot();

        for (int i = buildSpot.childCount - 1; i >= 0; i--)
            Destroy(buildSpot.GetChild(i).gameObject);

        Stack.Clear();
        currentHeight = 0f;
    }
}
