using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class ContinueBtnController : MonoBehaviour
{
    public Button btnContinue;

    void Start()
    {
        CheckSaveFiles();
    }

    private void CheckSaveFiles()
    {
        string savesDirectory = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(savesDirectory) || new DirectoryInfo(savesDirectory).GetDirectories().Length == 0)
        {
            btnContinue.interactable = false;
        }
        else
        {
            btnContinue.interactable = true;
        }
    }

    public void LoadLatestSave()
    {
        string savesDirectory = Path.Combine(Application.persistentDataPath, "saves");
        if (Directory.Exists(savesDirectory))
        {
            DirectoryInfo latestSaveFolder = new DirectoryInfo(savesDirectory).GetDirectories()
                                                    .OrderByDescending(d => d.LastWriteTime)
                                                    .FirstOrDefault();
            if (latestSaveFolder != null)
            {
                // GameManager'a "bu DEVAM, su klasore ait" bilgisini birak.
                PlayerPrefs.SetString(GameManager.PREF_ACTIVE_SAVE, latestSaveFolder.Name);
                PlayerPrefs.SetInt(GameManager.PREF_IS_NEW_GAME, 0);
                PlayerPrefs.Save();

                UpdateSaveDate(latestSaveFolder.Name);
                StartCoroutine(SendContinueRequestToPython(latestSaveFolder.Name));
            }
        }
    }

    private IEnumerator SendContinueRequestToPython(string folderName)
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
                Debug.Log("[CONTINUE] Python Veritaban� De�i�tirildi!");
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