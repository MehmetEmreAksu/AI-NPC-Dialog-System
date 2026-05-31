using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System; // Tarih (DateTime) iþlemleri için eklendi

public class MainMenuController : MonoBehaviour
{
    [Header("Menü Panelleri")]
    public GameObject mainMenuPanel;  // Ana menü ekranýn
    public GameObject settingsPanel;  // Ayarlar ekranýn
    public GameObject loadGamePanel;  // Load Game ekranýn

    // --- NEW GAME SÝSTEMÝ (Eski StartGame'in Yerine) ---
    public void CreateNewGame()
    {
        string savesDirectory = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(savesDirectory))
        {
            Directory.CreateDirectory(savesDirectory);
        }

        // 1. Çakýþmayý önlemek için tarihe dayalý eþsiz bir klasör adý (Örn: save_20260531_181530)
        string folderName = "save_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string newFolderPath = Path.Combine(savesDirectory, folderName);
        Directory.CreateDirectory(newFolderPath);

        // 2. Sýfýr kilometre metadata.json dosyasýný oluþtur
        GameSaveData newData = new GameSaveData
        {
            saveName = "Bilinmeyen Serüven", // Ýstersen bu baþlangýç ismini deðiþtirebilirsin
            saveDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        };

        string jsonContents = JsonUtility.ToJson(newData, true);
        File.WriteAllText(Path.Combine(newFolderPath, "metadata.json"), jsonContents);

        Debug.Log($"[NEW GAME] Yeni kayýt oluþturuldu: {folderName}. Python'a baðlanýlýyor...");

        // 3. Python'a bildir ve asýl oyuna gir
        StartCoroutine(SendNewGameRequestToPython(folderName));
    }

    private IEnumerator SendNewGameRequestToPython(string folderName)
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
                Debug.Log("[BAÞARILI] Python Yeni Veritabanýný Açtý! Oyuna giriliyor...");
                SceneManager.LoadScene("Main"); // Sahnenin adýný doðrudan "Main" olarak yazdýk
            }
            else
            {
                Debug.LogError("Python'a ulaþýlamadý: " + request.error);
            }
        }
    }

    // --- ARAYÜZ (UI) YÖNETÝMÝ ---

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OpenLoadGame()
    {
        mainMenuPanel.SetActive(false);
        loadGamePanel.SetActive(true);
    }

    public void CloseLoadGame()
    {
        loadGamePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // EXIT butonu için
    public void QuitGame()
    {
        Debug.Log("Oyundan çýkýlýyor...");
        Application.Quit();
    }
}