using Unity.Netcode;
using UnityEngine;

public class OwnerCamera : NetworkBehaviour
{
    [SerializeField] Camera cam;           // drag PlayerCam here
    [SerializeField] AudioListener al;     // drag the one on PlayerCam

    public override void OnNetworkSpawn()
    {
        // Find refs if not wired
        if (!cam) cam = GetComponentInChildren<Camera>(true);
        if (!al && cam) al = cam.GetComponent<AudioListener>();

        bool mine = IsOwner;
        if (cam) cam.gameObject.SetActive(mine);
        if (al) al.enabled = mine;

        if (mine) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }
}
