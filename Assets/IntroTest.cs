using UnityEngine;
using System.Collections;

public class IntroTest : MonoBehaviour
{
    [SerializeField] private ShiftIntroScreen intro;

    private void Start()
    {
        StartCoroutine(intro.Play(1, 3));
    }
}
