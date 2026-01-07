using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecipeHUDUI : MonoBehaviour
{
    [SerializeField] private Image[] slots; // Slot0 сверху, Slot7 снизу

    public void ShowHUD() => gameObject.SetActive(true);
    public void HideHUD() => gameObject.SetActive(false);

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }


    public void Show(List<IngredientType> recipe, Sprite[] spritesByEnumIndex)
    {
        if (slots == null || slots.Length == 0) return;

        // очистка
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].enabled = false;
            slots[i].sprite = null;
        }

        if (recipe == null) return;

        int n = slots.Length;
        int r = Mathf.Min(recipe.Count, n);

        // заполняем СНИЗУ ВВЕРХ:
        // i=0 (нижняя булка) -> slots[n-1]
        for (int i = 0; i < r; i++)
        {
            int slotIndex = (n - 1) - i; // вниз->вверх по слотам

            int enumIndex = (int)recipe[i];
            Sprite spr = (spritesByEnumIndex != null &&
                        enumIndex >= 0 &&
                        enumIndex < spritesByEnumIndex.Length)
                ? spritesByEnumIndex[enumIndex]
                : null;

            slots[slotIndex].sprite = spr;
            slots[slotIndex].enabled = (spr != null);
            slots[slotIndex].GetComponent<AutoAspectImage>()?.Apply();

        }

        gameObject.SetActive(true);
    }

    public void ShowProgress(List<IngredientType> recipe, int revealedCount, Sprite[] spritesByEnumIndex)
    {
        if (recipe == null) { Hide(); return; }

        int count = Mathf.Clamp(revealedCount, 0, recipe.Count);

        // временно показываем только часть рецепта (0..count-1)
        var partial = new List<IngredientType>(count);
        for (int i = 0; i < count; i++) partial.Add(recipe[i]);

        Show(partial, spritesByEnumIndex);
    }

}
