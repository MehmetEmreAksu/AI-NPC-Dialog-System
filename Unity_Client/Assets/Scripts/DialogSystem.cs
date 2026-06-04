using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;


public class DialogSystem : MonoBehaviour
{
    // Singleton instance for global access
    public static DialogSystem Instance { get; private set; }

    // Diyalog su an ekranda acik mi? PauseMenuController bunu kontrol eder:
    // diyalogdayken ESC pause menusunu ACMAZ, diyalogu kapatir.
    public bool IsDialogOpen => dialogPanel != null && dialogPanel.activeSelf;

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
    public string micDeviceName; // Bo� b�rak�rsan varsay�lan mikrofonu se�er
    private AudioClip recordedClip;
    private bool isRecording = false;
    private float recordingStartTime;

    [Header("Player State")]
    public string currentPlayerAction = "standing still and looking at you";


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

            // --- YEN�: BAS KONU� S�STEM� (V TU�U) ---
            if (Keyboard.current != null)
            {
                // alt tu�una BASMAYA BA�LADI�INDA
                if (Keyboard.current.altKey.wasPressedThisFrame && !isRecording)
                {
                    StartRecordingVoice();
                }

                // alt tu�undan EL�N� �EKT���NDE
                if (Keyboard.current.altKey.wasReleasedThisFrame && isRecording)
                {
                    StopRecordingAndSendVoice();
                }
            }
            // NOT: ESC artik burada degil, tek elden PauseMenuController'da yonetiliyor.
            // (Ayni frame'de hem diyalog kapanip hem pause acilma yarisini onlemek icin.)
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

    // Mikrofonu Ba�lat
    private void StartRecordingVoice()
    {
        isRecording = true;
        recordingStartTime = Time.time;

        // Ekrana haval� bir bilgi verelim
        playerInput.text = "";
        npcText.text = "<color=red>?? Dinleniyor... (Konu� ve V'yi b�rak)</color>";

        // Cihaz�n varsay�lan mikrofonunu 15 saniyeli�ine dinlemeye ba�la (44100 Hz standart kalitedir)
        recordedClip = Microphone.Start(micDeviceName, false, 15, 44100);
    }

    private void StopRecordingAndSendVoice()
    {
        isRecording = false;
        Microphone.End(micDeviceName);

        float duration = Time.time - recordingStartTime;

        // 1 saniyeden az bas-�ek yapt�ysa iptal et (Kazara basmalar� engeller)
        if (duration < 1.0f)
        {
            npcText.text = "Dinleme iptal edildi. �ok k�sa konu�tun.";
            return;
        }

        npcText.text = "<color=yellow>? Ses Python'a g�nderiliyor, bekle...</color>";
        playerInput.gameObject.SetActive(false);

        // O yazd���m�z �evirici s�n�f ile sesi byte dizisine (.wav) �evir
        byte[] wavData = WavUtility.FromAudioClip(recordedClip);

        // NetworkManager'a metin de�il, SES byte dizisini g�nder diyoruz!
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendVoiceToServer(wavData, currentNPC.npcID, currentPlayerAction, currentNPC.voiceModel);
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
            NetworkManager.Instance.SendMessageToServer(typedMessage, currentNPC.npcID, currentPlayerAction, currentNPC.voiceModel);
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

        // B�Y� BURADA: Konu�tu�umuz adama "Bak karde�im metin geldi, duygun da �u" diyoruz.
        if (currentNPC != null)
        {
            currentNPC.SetTalkingAnimation(true, emotion);
        }

        //ses �alma
        if(voiceClip != null && dialogAudioSource != null)
        {
            dialogAudioSource.clip = voiceClip;
            dialogAudioSource.pitch = 1f; // Ger�ek ses oldu�u i�in pitch'i bozmuyoruz
            dialogAudioSource.loop = false; // Tek seferlik �alacak
            dialogAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("<color=red>D�KKAT: voiceClip veya dialogAudioSource NULL (Bo�) geldi!</color>");
        }

        StartCoroutine(TypewriterEffect(message, voiceClip));
    }

    // Displays text character by character
    // 3. Yaz� H�z�n� Senkronize Eden Yeni Typewriter
    // 3. Yaz� H�z�n� Senkronize Eden Yeni ve ZIRHLI Typewriter
    private IEnumerator TypewriterEffect(string text, AudioClip voiceClip)
    {
        isNpcSpeaking = true;
        playerInput.gameObject.SetActive(false);
        npcText.text = "";

        // Varsay�lan g�venli h�z�m�z� al�yoruz
        float calculatedSpeed = typingSpeed;

        // AGA B�Y� BURADA: Unity'nin sa�malamas�n� (NaN veya 0 d�nmesini) engelliyoruz
        if (voiceClip != null && text.Length > 0 && voiceClip.length > 0.1f)
        {
            calculatedSpeed = (voiceClip.length - 0.1f) / text.Length;
        }

        // ZIRH: E�er Unity'nin matemati�i ��ld�r�rsa diye HIZ KOR�DORU �ekiyoruz!
        // H�z asla 0.01'den h�zl�, 0.06'dan yava� OLAMAYACAK. 
        // B�ylece ilk harfte tak�l�p sonsuza kadar bekleme bug'� tarihe kar��acak.
        calculatedSpeed = Mathf.Clamp(calculatedSpeed, 0.01f, 0.06f);

        foreach (char letter in text.ToCharArray())
        {
            npcText.text += letter;
            yield return new WaitForSeconds(calculatedSpeed);
        }

        // Yaz� bitti, Animator'� s�f�rla (E�er adam o s�rada �lmediyse/yok olmad�ysa)
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
    //        // E�er adam�n kaseti yoksa veya bo�sa hi� �al��ma
    //        if (currentNPC == null || currentNPC.myVoiceClips.Length == 0 || dialogAudioSource == null)
    //        {
    //            yield break;
    //        }
    //        int lastPlayedIndex = -1;
    //        // Metin akmaya devam etti�i s�rece (isNpcSpeaking true oldu�u s�rece) bu d�ng� d�necek
    //        while (isNpcSpeaking)
    //        {
    //            // E�er hoparl�r o an sessizse (Yani �ald��� kaset bittiyse)
    //            if (!dialogAudioSource.isPlaying)
    //            {
    //                // Rastgele bir kaset se�
    //                int randomIndex = Random.Range(0, currentNPC.myVoiceClips.Length);
    //
    //                if (currentNPC.myVoiceClips.Length > 1)
    //                {
    //                    while (randomIndex == lastPlayedIndex)
    //                    {
    //                        // Ayn� kaset denk geldik�e yeniden zar at! (Farkl�y� bulana kadar d�ner)
    //                        randomIndex = Random.Range(0, currentNPC.myVoiceClips.Length);
    //                    }
    //                }
    //
    //                lastPlayedIndex = randomIndex;
    //
    //                dialogAudioSource.clip = currentNPC.myVoiceClips[randomIndex];
    //
    //                // Robotikli�i k�rmak i�in pitch ile milimetrik oyna
    //                dialogAudioSource.pitch = currentNPC.myVoicePitch + Random.Range(-0.03f, 0.03f);
    //
    //                // Yeni kaseti �almaya ba�la!
    //                dialogAudioSource.Play();
    //            }
    //
    //            // Frame atla ve hoparl�r�n bitip bitmedi�ini kontrol etmeye devam et
    //            yield return null;
    //        }
    //
    //        // isNpcSpeaking false oldu (Metin tamamen bitti). 
    //        // Adam�n c�mlesi yar�m kalm�� olsa bile sesi an�nda �ak diye kes!
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

        // DE����KL�K 4: Konu�ma tamamen bitti (ESC'ye bas�ld�). Adam y�r�meye devam etsin!
        if (currentNPC != null)
        {
            currentNPC.EndDialog();
            currentNPC = null; // Haf�zay� temizle
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