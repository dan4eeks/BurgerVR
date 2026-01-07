using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Ingredient : MonoBehaviour
{
    [Header("Ingredient data")]
    public IngredientType type;
    public float layerHeight = 0.03f;

    [HideInInspector] public bool snapped = false;

    [Header("Spawner (who created me)")]
    public IngredientSpawner spawner;

    private void Awake()
    {
        // Находим XR Grab на этом объекте
        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
            return;

        // Когда ингредиент БЕРУТ В РУКУ
        grab.selectEntered.AddListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Сообщаем спавнеру: "меня взяли"
        if (spawner != null)
        {
            spawner.NotifyTakenOrTrashed(this);
            spawner = null; // защита от повторного вызова
        }
    }
}
