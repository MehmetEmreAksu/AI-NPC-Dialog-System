using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

// Represents the data payload sent to the Python API.
[System.Serializable]
public class APIRequestData
{
    public string player_message;
    public string player_action;
}

// Represents the data payload received from the Python API.
[System.Serializable]
public class APIResponseData
{
    public string npc_message;
    public string npc_emotion;
    public string npc_action;
    public string audio_url; // Python'dan ses dosyasý gelirse bu alana URL'si gelecek
}

public class NetworkManager : MonoBehaviour
{
    // Singleton instance for global access.
    public static NetworkManager Instance { get; private set; }

    [Header("API Configuration")]
    [Tooltip("Enter the server IP and port. Example: http://192.168.1.15:8000/chat")]
    public string apiUrl = "http://127.0.0.1:8000/chat";

    private void Awake()
    {
        // Enforce Singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    // Public method called by the DialogSystem when the player submits a message.
    public void SendMessageToServer(string message)
    {
        StartCoroutine(PostRequest(message));
    }

    // Coroutine to handle the asynchronous HTTP POST request.
    private IEnumerator PostRequest(string message, string action = "")
    {
        // 1. Prepare the JSON request data.
        APIRequestData requestData = new APIRequestData();
        requestData.player_message = message;
        requestData.player_action = action;

        string jsonPayload = JsonUtility.ToJson(requestData);

        // 2. Configure the HTTP request.
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 3. Send the request and wait for the server's response.
        yield return request.SendWebRequest();

        // 4. Process the result.
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Connection Error: " + request.error);

            // Notify the dialog system about the failure.
            if (DialogSystem.Instance != null)
            {
                DialogSystem.Instance.ReceiveResponse("System error: Could not connect to the server.", "calm", null);
            }
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("<color=cyan>PYTHON'DAN GELEN CEVAP: </color>" + jsonResponse);

            APIResponseData responseData = JsonUtility.FromJson<APIResponseData>(jsonResponse);

            if (DialogSystem.Instance != null && responseData != null)
            {

                // ZIRH: Eðer python emotion göndermeyi unutursa veya saçmalarsa varsayýlan olarak "calm" yap.
                string receivedEmotion = string.IsNullOrEmpty(responseData.npc_emotion) ? "calm" : responseData.npc_emotion.ToLower();

                if (!string.IsNullOrEmpty(responseData.audio_url))
                {
                    StartCoroutine(DownloadAndPlayAudio(responseData.audio_url, responseData.npc_message, receivedEmotion));
                }
                else 
                {

                    // Hem metni hem duyguyu DialogSistemi'ne yolluyoruz
                    DialogSystem.Instance.ReceiveResponse(responseData.npc_message, receivedEmotion, null);
                }

            }
        }
    }

    // NetworkManager.cs içine eklenecek yeni fonksiyon
    public void SendVoiceToServer(byte[] voiceData)
    {
        StartCoroutine(PostVoiceRequest(voiceData));
    }

    private IEnumerator PostVoiceRequest(byte[] voiceData)
    {
        // Ses verisini form-data olarak hazýrlýyoruz (Týpký Postman'den dosya yükler gibi)
        WWWForm form = new WWWForm();
        form.AddBinaryData("voice_file", voiceData, "player_voice.wav", "audio/wav");

        // Python'daki ses karþýlama endpoint'in. Örn: /chat/voice
        string voiceApiUrl = "http://127.0.0.1:8000/chat/voice";

        UnityWebRequest request = UnityWebRequest.Post(voiceApiUrl, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Ses Gönderme Hatasý: " + request.error);
            if (DialogSystem.Instance != null)
                DialogSystem.Instance.ReceiveResponse("Seni duyamadým, baðlantý koptu.", "", null);
        }
        else
        {
            // Python'dan gelen cevap tamamen ayný JSON yapýsýnda (text, emotion, action) dönecek
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("<color=cyan>SES CEVABI GELDÝ: </color>" + jsonResponse);

            APIResponseData responseData = JsonUtility.FromJson<APIResponseData>(jsonResponse);

            if (DialogSystem.Instance != null && responseData != null)
            {
                string receivedEmotion = string.IsNullOrEmpty(responseData.npc_emotion) ? "calm" : responseData.npc_emotion.ToLower();
                if (!string.IsNullOrEmpty(responseData.audio_url))
                {
                    StartCoroutine(DownloadAndPlayAudio(responseData.audio_url, responseData.npc_message, receivedEmotion));
                }
                else
                {
                    DialogSystem.Instance.ReceiveResponse(responseData.npc_message, receivedEmotion, null);
                }
            }
        }
    }

    // --- ESC'YE BASILINCA ÇALIÞACAK FONKSÝYON ---
    public void SendEndChatSignal(string playerId)
    {
        StartCoroutine(PostEndChat(playerId));
    }

    private IEnumerator PostEndChat(string playerId)
    {
        // Python'daki end_chat adresin
        string endChatUrl = "http://127.0.0.1:8000/end_chat";

        // JSON formatýnda player_id gönderiyoruz
        string jsonPayload = "{\"player_id\": \"" + playerId + "\"}";

        UnityWebRequest request = new UnityWebRequest(endChatUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("<color=orange>[*] Sohbet bitti sinyali Python'a ulaþtý. Hafýza özetleniyor.</color>");
        }
        else
        {
            Debug.LogError("End Chat sinyali gönderilemedi: " + request.error);
        }
    }

    // YENÝ FONKSÝYON: Sesi Streaming Mantýðýyla Ýndirir
    private IEnumerator DownloadAndPlayAudio(string url, string message, string emotion)
    {
        // Cache Buster Hilesi: Unity'nin eski kaseti çalmasýný engellemek için url sonuna sahte zaman damgasý ekliyoruz
        string finalUrl = url + "?t=" + Time.realtimeSinceStartup;

        yield return new WaitForSeconds(0.25f);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(finalUrl, AudioType.WAV))
        {
            ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = false;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Ses indirme hatasý: " + www.error);
                DialogSystem.Instance.ReceiveResponse(message, emotion, null);
            }
            else
            {
                // Sesi yakaladýk!
                AudioClip downloadedClip = DownloadHandlerAudioClip.GetContent(www);
                DialogSystem.Instance.ReceiveResponse(message, emotion, downloadedClip);
            }
        }
    }

}