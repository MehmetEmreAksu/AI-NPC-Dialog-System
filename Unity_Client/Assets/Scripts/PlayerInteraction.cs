using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    public float interactionDistance = 3f;
    private Camera mainCamera;

    void Start()
    {
        // Automatically find the camera attached to the player.
        mainCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        // Stop raycasting if the dialog system is active.
        if (DialogSystem.Instance != null && DialogSystem.Instance.dialogPanel.activeSelf)
        {
            return;
        }

        // Calculate the center of the screen for the raycast.
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        // Cast a ray forward.
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // Check if the hit object implements the IInteractable interface.
            IInteractable interactableObject = hit.collider.GetComponent<IInteractable>();

            if (interactableObject != null)
            {
                // Trigger interaction when the E key is pressed.
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    interactableObject.Interact();
                }
            }
        }
    }
}