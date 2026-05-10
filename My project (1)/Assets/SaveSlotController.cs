using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;
using System.IO;
using System.Linq; // Sýralama (OrderByDescending) iþlemleri için þart

public class SaveSlotManager : MonoBehaviour
{
    public TMP_Text[] slotTexts; // UI'daki 5 adet Text objesi
    
    // Týklanan slotun hangi klasöre ait olduðunu hafýzada tutmak için dizi
    private string[] activeSaveFolders = new string[5]; 

    void Start()
    {
        RefreshSlots();
    }

    public void RefreshSlots()
    {
        // 1. Ana "saves" klasörünün yolunu belirle ve yoksa oluþtur
        string savesDirectory = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(savesDirectory))
        {
            Directory.CreateDirectory(savesDirectory);
        }
        Debug.Log("Save Klasörü Yolu: " + savesDirectory);

        // 2. Klasörleri bul, son deðiþtirilme tarihine göre (en yeni en üstte) sýrala ve en fazla 5 tane al
        DirectoryInfo dirInfo = new DirectoryInfo(savesDirectory);
        DirectoryInfo[] saveFolders = dirInfo.GetDirectories()
                                             .OrderByDescending(d => d.LastWriteTime)
                                             .Take(5)
                                             .ToArray();

        // 3. UI Slotlarýný Doldur
        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (i < saveFolders.Length)
            {
                // Bu slot için bir save klasörü var
                string folderPath = saveFolders[i].FullName;
                string folderName = saveFolders[i].Name; // Örn: "save_01"
                
                activeSaveFolders[i] = folderName; // Butona basýlýnca Python'a yollamak için klasör adýný tutuyoruz
                
                // Klasörün içindeki metadata.json dosyasýný oku
                string metaFile = Path.Combine(folderPath, "metadata.json");
                if (File.Exists(metaFile))
                {
                    string jsonContents = File.ReadAllText(metaFile);
                    GameSaveData data = JsonUtility.FromJson<GameSaveData>(jsonContents);
                    slotTexts[i].text = $"{data.saveName} <br><color=#A0A0A0><size=70%>{data.saveDate}</size></color>";
                }
                else
                {
                    slotTexts[i].text = "VERÝ BOZUK";
                }
            }
            else
            {
                // Klasör yok, slot boþ kalmalý
                activeSaveFolders[i] = null;
                slotTexts[i].text = "EMPTY SLOT";
            }
        }
    }

    // Butona týklandýðýnda çalýþacak olan metot
    public void LoadGameFromSlot(int slotIndex)
    {
        string selectedFolderName = activeSaveFolders[slotIndex];
        
        if (!string.IsNullOrEmpty(selectedFolderName))
        {
            Debug.Log($"{selectedFolderName} klasörü seçildi. Python backend'ine istek atýlýyor...");
            StartCoroutine(SendLoadRequestToPython(selectedFolderName));
            // "Seçilen Save Klasörü: selectedFolderName" bilgisini yollayacaksýn.
        }
    }

    private IEnumerator SendLoadRequestToPython(string folderName)
    {
        string pythonUrl = "http://127.0.0.1:8000/load_save";

        // Python'un beklediði JSON formatýný hazýrlýyoruz: {"save_folder": "save_01"}
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
                Debug.Log("Python RAG Veritabaný baþarýyla deðiþtirildi! Sinyal: " + request.downloadHandler.text);
                // TODO: Buradan sonra Unity'nin SceneManager.LoadScene() komutu ile asýl oyun sahnesine geçiþ yapabilirsin.
            }
            else
            {
                Debug.LogError("Python ile iletiþim kurulamadý: " + request.error);
            }
        }
    }
}

[System.Serializable]
public class GameSaveData
{
    public string saveName;
    public string saveDate;
}