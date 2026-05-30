using UnityEngine;
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
                    slotTexts[i].text = $"Level {data.playerLevel} - {data.saveDate}";
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
            // Burada Unity'nin WebRequest'i (veya HTTP Client'ý) ile Python Flask API'sine 
            // "Seçilen Save Klasörü: selectedFolderName" bilgisini yollayacaksýn.
        }
    }
}

[System.Serializable]
public class GameSaveData
{
    public string playerName;
    public string saveDate;
    public int playerLevel;
}