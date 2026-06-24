using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    public float interactionDistance = 3f;
    private Camera mainCamera;

    [Header("Etkilesim Ipucu (Prompt)")]
    // Istersen kendi UI Text'ini buraya ata. Bos birakirsan kod otomatik olusturur.
    public Text promptLabel;

    void Start()
    {
        // Automatically find the camera attached to the player.
        mainCamera = GetComponentInChildren<Camera>();

        // Prompt etiketi atanmadiysa koddan olustur (editörde UI yapmana gerek kalmasin).
        if (promptLabel == null)
            promptLabel = BuildPromptLabel();

        HidePrompt();
    }

    void Update()
    {
        // Stop raycasting if the dialog system is active.
        if (DialogSystem.Instance != null && DialogSystem.Instance.dialogPanel.activeSelf)
        {
            HidePrompt();
            return;
        }

        // Calculate the center of the screen for the raycast.
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        IInteractable interactableObject = null;

        // Cast a ray forward.
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // Check if the hit object implements the IInteractable interface.
            interactableObject = hit.collider.GetComponent<IInteractable>();
        }

        if (interactableObject != null)
        {
            // Baktigimiz objenin kendi ipucu metnini goster.
            ShowPrompt(interactableObject.GetPrompt());

            // Trigger interaction when the E key is pressed.
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactableObject.Interact();
            }
        }
        else
        {
            HidePrompt();
        }
    }

    private void ShowPrompt(string text)
    {
        if (promptLabel == null) return;
        if (!promptLabel.gameObject.activeSelf) promptLabel.gameObject.SetActive(true);
        promptLabel.text = text;
    }

    private void HidePrompt()
    {
        if (promptLabel != null && promptLabel.gameObject.activeSelf)
            promptLabel.gameObject.SetActive(false);
    }

    // Ekranin ortasinin biraz altina kucuk bir ipucu yazisi kuran kod.
    private Text BuildPromptLabel()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("InteractionPromptCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject textGO = new GameObject("PromptText", typeof(RectTransform));
        textGO.transform.SetParent(canvasGO.transform, false);
        Text t = textGO.AddComponent<Text>();
        t.font = font;
        t.text = "[E]";
        t.fontSize = 34;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        // Hafif golge ki acik zeminlerde de okunsun
        Shadow sh = textGO.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
        sh.effectDistance = new Vector2(2f, -2f);

        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -90f); // crosshair'in biraz altinda
        rt.sizeDelta = new Vector2(600f, 60f);

        return t;
    }
}
