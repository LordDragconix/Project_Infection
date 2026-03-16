using TMPro;
using UnityEngine;

public class SpeedDisplay : MonoBehaviour
{
    public Rigidbody rb;
    public TextMeshProUGUI speedText;

    private void Update()
    {
        // Measure horizontal speed only (ignoring Y)
        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = flatVelocity.magnitude;

        // Display with one decimal place
        speedText.text = $"Speed: {speed:F1}";
    }
}
