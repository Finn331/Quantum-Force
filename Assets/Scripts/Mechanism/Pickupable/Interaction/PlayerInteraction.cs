using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Tooltip("Jarak maksimal pemain bisa berinteraksi dengan objek.")]
    public float interactionRange = 5f;

    private Camera playerCamera;

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("PlayerInteraction script needs to be on an object with a Camera component!", this);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
        {
            if (hit.collider.TryGetComponent(out CatapultHandle handle))
            {
                handle.Interact();
            }
        }
    }
}