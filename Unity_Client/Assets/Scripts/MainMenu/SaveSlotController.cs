using UnityEngine;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class SaveSlotController : MonoBehaviour
{
    public TMP_Text[] slotTexts; // UI'daki 5 adet EMPTY SLOT texti
    private string[] activeSaveFolders = new string[5];

    void Start()
    {
        RefreshSlots();
    }

    public void RefreshSlots()
    {
        string savesDirectory = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(savesDirectory))
        {
            Directory.CreateDirectory(savesDirectory);
        }

        DirectoryInfo dirInfo = new DirectoryInfo(savesDirectory);
        DirectoryInfo[] saveFolders = dirInfo.GetDirectories()
                                             .OrderByDescending(d => d.LastWriteTime)
                                             .Take(5)
                                             .ToArray();

        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (i < saveFolders.Length)
            {
                string folderPath = saveFolders[i].FullName;
                activeSaveFolders[i] = saveFolders[i].Name;

                string metaFile = Path.Combine(folderPath, "metadata.json");
                if (File.Exists(metaFile))
                {
                    string jsonContents = File.ReadAllText(metaFile);
                    GameSaveData data = JsonUtility.FromJson<GameSaveData>(jsonContents);
                    // Sadece isim ve tarih yazd�r�yoruz
                    slotTexts[i].text = $"{data.saveName} \n<size=70%><color=#A0A0A0>{data.saveDate}</color></size>";
                }
                else
                {
                    slotTexts[i].text = "VER� BOZUK";
                }
            }
            else
            {
                activeSaveFolders[i] = null;
                slotTexts[i].text = "EMPTY SLOT";
            }
        }
    }

    public void LoadGameFromSlot(int slotIndex)
    {
        string selectedFolderName = activeSaveFolders[slotIndex];
        if (!string.IsNullOrEmpty(selectedFolderName))
        {
            Debug.Log($"{selectedFolderName} se�ildi. Python'a istek at�l�yor...");
            UpdateSaveDate(selectedFolderName);
            StartCoroutine(SendLoadRequestToPython(selectedFolderName));
        }
    }

    private IEnumerator SendLoadRequestToPython(string folderName)
    {
        // Once Python sunucusunun ayaga kalkmasini bekle.
        yield return StartCoroutine(WaitForPythonServer());

        string pythonUrl = "http://127.0.0.1:8000/load_save";
        string jsonData = "{\"save_folder\": \"" + folderName + "\"}";

        using (UnityWebRequest request = new UnityWebRequest(pythonUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[BA�ARILI] Python Veritaban� De�i�tirildi!");
                // Yorum sat�r�n� kald�rd�k ve sahne ad�n� "Main" olarak girdik:
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            }
        }
    }

    // Sunucu /ping'e 200 donene kadar (veya zaman asimina kadar) yoklar.
    private IEnumerator WaitForPythonServer()
    {
        string pingUrl = "http://127.0.0.1:8000/ping";
        float timeout = 30f;
        float elapsed = 0f;
        float retryDelay = 0.5f;

        while (elapsed < timeout)
        {
            using (UnityWebRequest ping = UnityWebRequest.Get(pingUrl))
            {
                ping.timeout = 2;
                yield return ping.SendWebRequest();

                if (ping.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[SERVER] Python hazir.");
                    yield break;
                }
            }

            yield return new WaitForSeconds(retryDelay);
            elapsed += retryDelay;
        }

        Debug.LogError("[SERVER] Python sunucusu 30 sn icinde ayaga kalkmadi!");
    }

    private void UpdateSaveDate(string folderName)
    {
        string metaFile = Path.Combine(Application.persistentDataPath, "saves", folderName, "metadata.json");

        if (File.Exists(metaFile))
        {
            string jsonContents = File.ReadAllText(metaFile);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(jsonContents);

            // Tarihi �u anki zamana g�ncelle
            data.saveDate = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            string updatedJson = JsonUtility.ToJson(data, true);
            File.WriteAllText(metaFile, updatedJson);
            Debug.Log($"[AUTO-SAVE] {folderName} tarihi g�ncellendi: {data.saveDate}");
        }
    }
}

[System.Serializable]
public class GameSaveData
{
    public string saveName;
    public string saveDate;
}