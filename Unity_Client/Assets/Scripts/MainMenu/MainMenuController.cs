using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System; // Tarih (DateTime) i�lemleri i�in eklendi

public class MainMenuController : MonoBehaviour
{
    [Header("Men� Panelleri")]
    public GameObject mainMenuPanel;  // Ana men� ekran�n
    public GameObject settingsPanel;  // Ayarlar ekran�n
    public GameObject loadGamePanel;  // Load Game ekran�n

    // --- NEW GAME S�STEM� (Eski StartGame'in Yerine) ---
    public void CreateNewGame()
    {
        string savesDirectory = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(savesDirectory))
        {
            Directory.CreateDirectory(savesDirectory);
        }

        // 1. �ak��may� �nlemek i�in tarihe dayal� e�siz bir klas�r ad� (�rn: save_20260531_181530)
        string folderName = "save_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string newFolderPath = Path.Combine(savesDirectory, folderName);
        Directory.CreateDirectory(newFolderPath);

        // 2. S�f�r kilometre metadata.json dosyas�n� olu�tur
        GameSaveData newData = new GameSaveData
        {
            saveName = "Bilinmeyen Ser�ven", // �stersen bu ba�lang�� ismini de�i�tirebilirsin
            saveDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        };

        string jsonContents = JsonUtility.ToJson(newData, true);
        File.WriteAllText(Path.Combine(newFolderPath, "metadata.json"), jsonContents);

        Debug.Log($"[NEW GAME] Yeni kay�t olu�turuldu: {folderName}. Python'a ba�lan�l�yor...");

        // GameManager'a "bu YENI oyun, su klasore ait" bilgisini birak.
        PlayerPrefs.SetString(GameManager.PREF_ACTIVE_SAVE, folderName);
        PlayerPrefs.SetInt(GameManager.PREF_IS_NEW_GAME, 1);
        PlayerPrefs.Save();

        // 3. Python'a bildir ve as�l oyuna gir
        StartCoroutine(SendNewGameRequestToPython(folderName));
    }

    private IEnumerator SendNewGameRequestToPython(string folderName)
    {
        // Once Python sunucusunun ayaga kalkmasini bekle (ilk acilista birkac saniye surebilir).
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
                Debug.Log("[BA�ARILI] Python Yeni Veritaban�n� A�t�! Oyuna giriliyor...");
                SceneManager.LoadScene("Main"); // Sahnenin ad�n� do�rudan "Main" olarak yazd�k
            }
            else
            {
                Debug.LogError("Python'a ula��lamad�: " + request.error);
            }
        }
    }

    // Sunucu /ping'e 200 donene kadar (veya zaman asimina kadar) yoklar.
    private IEnumerator WaitForPythonServer()
    {
        string pingUrl = "http://127.0.0.1:8000/ping";
        float timeout = 30f;        // en fazla 30 sn bekle
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

    // --- ARAY�Z (UI) Y�NET�M� ---

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

    // EXIT butonu i�in
    public void QuitGame()
    {
        Debug.Log("Oyundan ��k�l�yor...");
        Application.Quit();
    }
}