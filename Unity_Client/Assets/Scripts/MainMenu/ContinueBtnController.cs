using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class MainMenuManager : MonoBehaviour
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
                UpdateSaveDate(latestSaveFolder.Name);
                StartCoroutine(SendContinueRequestToPython(latestSaveFolder.Name));
            }
        }
    }

    private IEnumerator SendContinueRequestToPython(string folderName)
    {
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
                Debug.Log("[CONTINUE] Python Veritabaný Deðiþtirildi!");
                // Yorum satýrýný kaldýrdýk ve sahne adýný "Main" olarak girdik:
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            }
        }
    }

    private void UpdateSaveDate(string folderName)
    {
        string metaFile = Path.Combine(Application.persistentDataPath, "saves", folderName, "metadata.json");

        if (File.Exists(metaFile))
        {
            string jsonContents = File.ReadAllText(metaFile);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(jsonContents);

            // Tarihi þu anki zamana güncelle
            data.saveDate = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            string updatedJson = JsonUtility.ToJson(data, true);
            File.WriteAllText(metaFile, updatedJson);
            Debug.Log($"[AUTO-SAVE] {folderName} tarihi güncellendi: {data.saveDate}");
        }
    }
}