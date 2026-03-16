using Unity.Netcode;
using UnityEngine;

public class OwnerMouseLook : NetworkBehaviour
{
    [SerializeField] Camera cam;              // drag CameraHolder/PlayerCam
    [SerializeField] Transform orientation;   // drag Orientation
    [SerializeField] float sensX = 300f, sensY = 300f;

    float xRot, yRot;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { enabled = false; return; }
        if (!cam) cam = GetComponentInChildren<Camera>(true);

        if (cam && !cam.gameObject.activeSelf)
            cam.gameObject.SetActive(true);

        // initialise from current rotation so it doesn't snap on spawn
        Vector3 bodyEuler = transform.rotation.eulerAngles;
        yRot = bodyEuler.y;

        if (cam)
        {
            Vector3 camEuler = cam.transform.localRotation.eulerAngles;
            xRot = camEuler.x;
            if (xRot > 180f) xRot -= 360f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mx = Input.GetAxisRaw("Mouse X") * sensX * Time.deltaTime;
        float my = Input.GetAxisRaw("Mouse Y") * sensY * Time.deltaTime;

        yRot += mx;
        xRot = Mathf.Clamp(xRot - my, -90f, 90f);

        if (cam) cam.transform.localRotation = Quaternion.Euler(xRot, 0, 0);
        if (orientation) orientation.rotation = Quaternion.Euler(0, yRot, 0);
        transform.rotation = Quaternion.Euler(0, yRot, 0);
    }
}
