using UnityEngine;
using UnityEngine.UI; 
using System.IO;
using System.Linq;
using UnityEngine.Networking; // Python ile iletiþim (HTTP) için þart
using System.Collections;     // Coroutine (bekleme) iþlemleri için þart
using System.Text;            // JSON formatlamasý için þart
using UnityEngine.SceneManagement; // Sahne (Oyun) yüklemek için þart

public class MainMenuManager : MonoBehaviour
{
    [Header("Arayüz Elemanlarý")]
    public Button btnContinue; 

    void Start()
    {
        CheckSaveFiles();
    }

    private void CheckSaveFiles()
    {
        string savesDirectory = Path.Combine(Application.persistentDataPath, "saves");

        // Kayýt klasörü yoksa veya içi tamamen boþsa Continue butonunu kapat
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
            DirectoryInfo dirInfo = new DirectoryInfo(savesDirectory);
            
            // En son deðiþtirilen (oynanan) klasörü bul
            DirectoryInfo latestSaveFolder = dirInfo.GetDirectories()
                                                    .OrderByDescending(d => d.LastWriteTime)
                                                    .FirstOrDefault();

            if (latestSaveFolder != null)
            {
                string folderName = latestSaveFolder.Name;
                Debug.Log($"[CONTINUE] En son kayýt bulundu: {folderName}. Python sunucusuna istek atýlýyor...");
                
                // Python'a baðlanma sürecini baþlat
                StartCoroutine(SendContinueRequestToPython(folderName));
            }
        }
    }

    // Python ile konuþan asýl Asenkron fonksiyon
    private IEnumerator SendContinueRequestToPython(string folderName)
    {
        string pythonUrl = "http://127.0.0.1:8000/load_save";
        
        // Python'a göndereceðimiz JSON paketi
        string jsonData = "{\"save_folder\": \"" + folderName + "\"}";
        
        using (UnityWebRequest request = new UnityWebRequest(pythonUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Python'dan cevap gelene kadar bekle
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[BAÞARILI] Python RAG Veritabaný Deðiþtirildi! " + request.downloadHandler.text);
                
                // --- ARTIK OYUNA GÝREBÝLÝRÝZ ---
                // DÝKKAT: "OyunSahnesi" yazan yere kendi asýl oyun sahnene verdiðin adý yaz!
                // SceneManager.LoadScene("OyunSahnesi"); 
            }
            else
            {
                Debug.LogError("Python ile iletiþim kurulamadý: " + request.error);
            }
        }
    }
}