using UnityEngine;

public class Flashlight : MonoBehaviour
{
    [SerializeField] private Light flashlight;
    [SerializeField] private KeyCode toggleKey = KeyCode.F;
    [SerializeField] private AudioSource clickSfx; // optional

    void Awake()
    {
        if (!flashlight) flashlight = GetComponentInChildren<Light>();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            bool newState = !flashlight.enabled;
            flashlight.enabled = newState;

            if (clickSfx) clickSfx.Play();
        }
    }
}
