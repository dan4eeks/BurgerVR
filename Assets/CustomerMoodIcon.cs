using UnityEngine;
using UnityEngine.UI;

public class CustomerMoodIcon : MonoBehaviour
{
    [SerializeField] private Image image;

    [Header("Sprites")]
    [SerializeField] private Sprite happy;
    [SerializeField] private Sprite neutral;
    [SerializeField] private Sprite angry;
    [SerializeField] private Sprite scared;
    [SerializeField] private Sprite thinking;


    private void Awake()
    {
        if (image == null)
            image = GetComponentInChildren<Image>();
    }

    public void SetMood(CustomerMood mood)
    {
        if (image == null) return;

        switch (mood)
        {
            case CustomerMood.Happy:
                image.sprite = happy;
                break;
            case CustomerMood.Neutral:
                image.sprite = neutral;
                break;
            case CustomerMood.Angry:
                image.sprite = angry;
                break;
            case CustomerMood.Scared:
                image.sprite = scared;
                break;
            case CustomerMood.Thinking:
                image.sprite = thinking;
                break;
        }
    }
}
