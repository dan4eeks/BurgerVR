using UnityEngine;

public class GrillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var cookable = other.GetComponentInParent<PattyCookable>();
        if (cookable != null)
            cookable.SetOnGrill(true);
    }

    private void OnTriggerExit(Collider other)
    {
        var cookable = other.GetComponentInParent<PattyCookable>();
        if (cookable != null)
            cookable.SetOnGrill(false);
    }
}
