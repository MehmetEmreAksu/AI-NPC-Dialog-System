using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class DialogSystem : MonoBehaviour
{
    // Singleton instance for global access
    public static DialogSystem Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject dialogPanel;
    public TextMeshProUGUI npcText;
    public TMP_InputField playerInput;

    [Header("System References")]
    public FPSController playerFPS;
    public Camera playerCamera;

    [Header("Cinematic Settings")]
    public float normalFOV = 60f;
    public float zoomFOV = 40f;
    public float typingSpeed = 0.03f;

    [Header("Voice Settings")]
    public string micDeviceName; // Boþ býrakýrsan varsayýlan mikrofonu seçer
    private AudioClip recordedClip;
    private bool isRecording = false;
    private float recordingStartTime;


    public bool isNpcSpeaking = false;
    public AudioSource dialogAudioSource;

    private Coroutine zoomCoroutine;

    private NPC_Roam currentNPC;

    private void Awake()
    {
        // Ensure only one instance of DialogSystem exists
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Hide panel on start
        dialogPanel.SetActive(false);
    }

    private void Update()
    {
        if (dialogPanel.activeSelf)
        {
            // Listen for Enter key when the input field is active
            if (playerInput.gameObject.activeSelf && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                SendPlayerMessage();
            }

            // --- YENÝ: BAS KONUÞ SÝSTEMÝ (V TUÞU) ---
            if (Keyboard.current != null)
            {
                // alt tuþuna BASMAYA BAÞLADIÐINDA
                if (Keyboard.current.altKey.wasPressedThisFrame && !isRecording)
                {
                    StartRecordingVoice();
                }

                // alt tuþundan ELÝNÝ ÇEKTÝÐÝNDE
                if (Keyboard.current.altKey.wasReleasedThisFrame && isRecording)
                {
                    StopRecordingAndSendVoice();
                }
            }

            // Listen for ESC key to close dialog
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                StopDialog();
            }
        }
    }

    // Called when the interaction starts (Raycast hits NPC and player presses E)
    public void StartChat(NPC_Roam npc)
    {
        currentNPC = npc;
        dialogPanel.SetActive(true);

        // Freeze player movement and unlock cursor
        if (playerFPS != null) playerFPS.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Start smooth camera zoom
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(SmoothCameraZoom(zoomFOV));

        // Prepare the UI for player input
        npcText.text = "Write something to speak... (Press ESC to exit)";
        playerInput.gameObject.SetActive(true);
        playerInput.text = "";
        playerInput.ActivateInputField();
    }

    // Mikrofonu Baþlat
    private void StartRecordingVoice()
    {
        isRecording = true;
        recordingStartTime = Time.time;

        // Ekrana havalý bir bilgi verelim
        playerInput.text = "";
        npcText.text = "<color=red>?? Dinleniyor... (Konuþ ve V'yi býrak)</color>";

        // Cihazýn varsayýlan mikrofonunu 15 saniyeliðine dinlemeye baþla (44100 Hz standart kalitedir)
        recordedClip = Microphone.Start(micDeviceName, false, 15, 44100);
    }

    private void StopRecordingAndSendVoice()
    {
        isRecording = false;
        Microphone.End(micDeviceName);

        float duration = Time.time - recordingStartTime;

        // 1 saniyeden az bas-çek yaptýysa iptal et (Kazara basmalarý engeller)
        if (duration < 1.0f)
        {
            npcText.text = "Dinleme iptal edildi. Çok kýsa konuþtun.";
            return;
        }

        npcText.text = "<color=yellow>? Ses Python'a gönderiliyor, bekle...</color>";
        playerInput.gameObject.SetActive(false);

        // O yazdýðýmýz çevirici sýnýf ile sesi byte dizisine (.wav) çevir
        byte[] wavData = WavUtility.FromAudioClip(recordedClip);

        // NetworkManager'a metin deðil, SES byte dizisini gönder diyoruz!
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendVoiceToServer(wavData);
        }
    }

    // Sends the typed message to the Python API
    private void SendPlayerMessage()
    {
        string typedMessage = playerInput.text;
        if (string.IsNullOrEmpty(typedMessage)) return;

        // Clear UI and hide input field while waiting for response
        playerInput.text = "";
        playerInput.gameObject.SetActive(false);
        npcText.text = "..."; // Waiting indicator

        // Send the message via NetworkManager to the FastAPI server
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendMessageToServer(typedMessage);
        }
        else
        {
            Debug.LogError("NetworkManager instance not found in the scene.");
        }
    }

    // Called by NetworkManager when a response arrives from the server
    public void ReceiveResponse(string message, string emotion, AudioClip voiceClip)
    {
        dialogPanel.SetActive(true);

        if (playerFPS != null) playerFPS.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // BÜYÜ BURADA: Konuþtuðumuz adama "Bak kardeþim metin geldi, duygun da þu" diyoruz.
        if (currentNPC != null)
        {
            currentNPC.SetTalkingAnimation(true, emotion);
        }

        //ses çalma
        if(voiceClip != null && dialogAudioSource != null)
        {
            dialogAudioSource.clip = voiceClip;
            dialogAudioSource.pitch = 1f; // Gerçek ses olduðu için pitch'i bozmuyoruz
            dialogAudioSource.loop = false; // Tek seferlik çalacak
            dialogAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("<color=red>DÝKKAT: voiceClip veya dialogAudioSource NULL (Boþ) geldi!</color>");
        }

        StartCoroutine(TypewriterEffect(message, voiceClip));
    }

    // Displays text character by character
    // 3. Yazý Hýzýný Senkronize Eden Yeni Typewriter
    // 3. Yazý Hýzýný Senkronize Eden Yeni ve ZIRHLI Typewriter
    private IEnumerator TypewriterEffect(string text, AudioClip voiceClip)
    {
        isNpcSpeaking = true;
        playerInput.gameObject.SetActive(false);
        npcText.text = "";

        // Varsayýlan güvenli hýzýmýzý alýyoruz
        float calculatedSpeed = typingSpeed;

        // AGA BÜYÜ BURADA: Unity'nin saçmalamasýný (NaN veya 0 dönmesini) engelliyoruz
        if (voiceClip != null && text.Length > 0 && voiceClip.length > 0.1f)
        {
            calculatedSpeed = (voiceClip.length - 0.1f) / text.Length;
        }

        // ZIRH: Eðer Unity'nin matematiði çýldýrýrsa diye HIZ KORÝDORU çekiyoruz!
        // Hýz asla 0.01'den hýzlý, 0.06'dan yavaþ OLAMAYACAK. 
        // Böylece ilk harfte takýlýp sonsuza kadar bekleme bug'ý tarihe karýþacak.
        calculatedSpeed = Mathf.Clamp(calculatedSpeed, 0.01f, 0.06f);

        foreach (char letter in text.ToCharArray())
        {
            npcText.text += letter;
            yield return new WaitForSeconds(calculatedSpeed);
        }

        // Yazý bitti, Animator'ý sýfýrla (Eðer adam o sýrada ölmediyse/yok olmadýysa)
        if (currentNPC != null)
        {
            currentNPC.SetTalkingAnimation(false, "calm");
        }

        isNpcSpeaking = false;
        playerInput.gameObject.SetActive(true);
        playerInput.ActivateInputField();
    }

    //    private IEnumerator HandleVoicePhrases()
    //    {
    //        // Eðer adamýn kaseti yoksa veya boþsa hiç çalýþma
    //        if (currentNPC == null || currentNPC.myVoiceClips.Length == 0 || dialogAudioSource == null)
    //        {
    //            yield break;
    //        }
    //        int lastPlayedIndex = -1;
    //        // Metin akmaya devam ettiði sürece (isNpcSpeaking true olduðu sürece) bu döngü dönecek
    //        while (isNpcSpeaking)
    //        {
    //            // Eðer hoparlör o an sessizse (Yani çaldýðý kaset bittiyse)
    //            if (!dialogAudioSource.isPlaying)
    //            {
    //                // Rastgele bir kaset seç
    //                int randomIndex = Random.Range(0, currentNPC.myVoiceClips.Length);
    //
    //                if (currentNPC.myVoiceClips.Length > 1)
    //                {
    //                    while (randomIndex == lastPlayedIndex)
    //                    {
    //                        // Ayný kaset denk geldikçe yeniden zar at! (Farklýyý bulana kadar döner)
    //                        randomIndex = Random.Range(0, currentNPC.myVoiceClips.Length);
    //                    }
    //                }
    //
    //                lastPlayedIndex = randomIndex;
    //
    //                dialogAudioSource.clip = currentNPC.myVoiceClips[randomIndex];
    //
    //                // Robotikliði kýrmak için pitch ile milimetrik oyna
    //                dialogAudioSource.pitch = currentNPC.myVoicePitch + Random.Range(-0.03f, 0.03f);
    //
    //                // Yeni kaseti çalmaya baþla!
    //                dialogAudioSource.Play();
    //            }
    //
    //            // Frame atla ve hoparlörün bitip bitmediðini kontrol etmeye devam et
    //            yield return null;
    //        }
    //
    //        // isNpcSpeaking false oldu (Metin tamamen bitti). 
    //        // Adamýn cümlesi yarým kalmýþ olsa bile sesi anýnda þak diye kes!
    //        if (dialogAudioSource != null)
    //        {
    //            dialogAudioSource.Stop();
    //        }
    //    }
    // Closes the dialog panel and resets player state
    public void StopDialog()
    {
        StopAllCoroutines();
        dialogPanel.SetActive(false);

        // Unfreeze player and lock cursor
        if (playerFPS != null) playerFPS.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Zoom camera back out
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(SmoothCameraZoom(normalFOV));

        // DEÐÝÞÝKLÝK 4: Konuþma tamamen bitti (ESC'ye basýldý). Adam yürümeye devam etsin!
        if (currentNPC != null)
        {
            currentNPC.EndDialog();
            currentNPC = null; // Hafýzayý temizle
        }
    }

    // Smoothly interpolates camera FOV
    private IEnumerator SmoothCameraZoom(float targetFOV)
    {
        float elapsedTime = 0f;
        float startingFOV = playerCamera.fieldOfView;
        float zoomDuration = 0.5f;

        while (elapsedTime < zoomDuration)
        {
            elapsedTime += Time.deltaTime;
            playerCamera.fieldOfView = Mathf.Lerp(startingFOV, targetFOV, elapsedTime / zoomDuration);
            yield return null;
        }
        playerCamera.fieldOfView = targetFOV;
    }
}