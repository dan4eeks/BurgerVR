using UnityEngine;

public class TrashBinZone : MonoBehaviour
{
    [Header("What can be trashed")]
    [SerializeField] private bool trashIngredients = true;
    [SerializeField] private bool trashPlates = false;
    [SerializeField] private PlateSpawner plateSpawner;

    [Header("Behaviour")]
    [SerializeField] private bool requireTag = false;
    [SerializeField] private string trashTag = "Trashable";
    [SerializeField] private float destroyDelay = 0.05f;

    [Header("Optional FX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip trashSfx;

    private void OnTriggerEnter(Collider other)
    {
        // Если требуется тег — проверяем
        if (requireTag && !other.CompareTag(trashTag))
            return;

        // 1) Ингредиенты
        if (trashIngredients)
        {
            Ingredient ing = other.GetComponentInParent<Ingredient>();
            if (ing != null)
            {
                TrashObject(ing.gameObject);
                return;
            }
        }

        // 2) Тарелки (по желанию)
        if (trashPlates)
        {
            Plate plate = other.GetComponentInParent<Plate>();
            if (plate != null)
            {
                TrashPlate(plate);
                return;
            }
        }
    }

    private void TrashPlate(Plate plate)
    {
        if (plate == null) return;

        // звук
        if (audioSource != null && trashSfx != null)
            audioSource.PlayOneShot(trashSfx);

        // уничтожаем тарелку
        Destroy(plate.gameObject, destroyDelay);

        // ?? СРАЗУ спавним новую
        if (plateSpawner != null)
            plateSpawner.SpawnPlate();
    }


    private void TrashObject(GameObject go)
    {
        if (go == null) return;

        // Чтоб предмет точно "отпустился" и не завис в руке:
        Rigidbody rb = go.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Звук (опционально)
        if (audioSource != null && trashSfx != null)
            audioSource.PlayOneShot(trashSfx);

        // Небольшая задержка — помогает XR отпустить объект без ошибок
        Destroy(go, destroyDelay);
    }
}
