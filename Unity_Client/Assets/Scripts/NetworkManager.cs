using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class APIRequestData
{
    public string player_message;
    public string player_action;
    public string npc_id;
    public string voice_model;

    public string npc_role;
    public string suspect_name;
    public string guilty_name;
}

[System.Serializable]
public class APIResponseData
{
    public string npc_message;
    public string npc_emotion;
    public string npc_action;
    public string audio_url;
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("API Configuration")]
    public string apiUrl = "http://127.0.0.1:8000/chat";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    // AGA BÜYÜ BURADA: Fonksiyon parametreleri eskisi gibi temiz (sadece 4 tane)!
    public void SendMessageToServer(string message, string npcId, string action, string voiceModel)
    {
        StartCoroutine(PostRequest(message, npcId, action, voiceModel));
    }

    private IEnumerator PostRequest(string message, string npcId, string action, string voiceModel)
    {
        APIRequestData requestData = new APIRequestData();
        requestData.player_message = message;
        requestData.player_action = action;
        requestData.npc_id = npcId;
        requestData.voice_model = voiceModel;

        // VERÝYÝ ELDEN TAÞIMIYORUZ, DÝREKT MERKEZDEN ÇEKÝYORUZ!
        if (GameManager.Instance != null)
        {
            requestData.npc_role = GameManager.Instance.GetNpcRole(npcId);
            requestData.suspect_name = GameManager.Instance.suspectNPC;
            requestData.guilty_name = GameManager.Instance.guiltyNPC;
        }

        string jsonPayload = JsonUtility.ToJson(requestData);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Connection Error: " + request.error);
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

    // SES FONKSÝYONU: Parametreleri yine temiz tuttuk!
    public void SendVoiceToServer(byte[] voiceData, string npcId, string action, string voiceModel)
    {
        StartCoroutine(PostVoiceRequest(voiceData, npcId, action, voiceModel));
    }

    private IEnumerator PostVoiceRequest(byte[] voiceData, string npcId, string action, string voiceModel)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("voice_file", voiceData, "player_voice.wav", "audio/wav");

        form.AddField("player_id", "default_player");
        form.AddField("npc_id", npcId);
        form.AddField("player_action", action);
        form.AddField("voice_model", voiceModel);

        // MERKEZDEN GÝZLÝCE ÇEKÝP FORMA EKLÝYORUZ!
        if (GameManager.Instance != null)
        {
            form.AddField("npc_role", GameManager.Instance.GetNpcRole(npcId));
            form.AddField("suspect_name", GameManager.Instance.suspectNPC);
            form.AddField("guilty_name", GameManager.Instance.guiltyNPC);
        }

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

    public void SendEndChatSignal(string playerId)
    {
        StartCoroutine(PostEndChat(playerId));
    }

    private IEnumerator PostEndChat(string playerId)
    {
        string endChatUrl = "http://127.0.0.1:8000/end_chat";
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

    private IEnumerator DownloadAndPlayAudio(string url, string message, string emotion)
    {
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
                AudioClip downloadedClip = DownloadHandlerAudioClip.GetContent(www);
                DialogSystem.Instance.ReceiveResponse(message, emotion, downloadedClip);
            }
        }
    }
}