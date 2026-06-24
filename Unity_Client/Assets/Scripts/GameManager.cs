using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Current Game State")]
    public string guiltyNPC;
    public string suspectNPC;
    public string innocentNPC;

    // Menulerin (New Game / Continue / Load) doldurdugu PlayerPrefs anahtarlari
    public const string PREF_ACTIVE_SAVE = "ActiveSaveFolder";
    public const string PREF_IS_NEW_GAME = "IsNewGame";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        string folder = PlayerPrefs.GetString(PREF_ACTIVE_SAVE, "");
        bool isNewGame = PlayerPrefs.GetInt(PREF_IS_NEW_GAME, 1) == 1;

        // Editörde dogrudan Main sahnesinden Play'e basildiysa kayit bilgisi yoktur:
        // sadece rastgele ata, kayda/hafizaya dokunma (test kolayligi).
        if (string.IsNullOrEmpty(folder))
        {
            AssignRoles();
            UnityEngine.Debug.Log("[GameManager] Aktif kayit yok (editör testi). Roller gecici atandi.");
            return;
        }

        string metaPath = Path.Combine(Application.persistentDataPath, "saves", folder, "metadata.json");

        // --- DEVAM ETME: roller kayittan yuklenir, hafiza KORUNUR ---
        if (!isNewGame && File.Exists(metaPath))
        {
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(metaPath));
            if (data != null && !string.IsNullOrEmpty(data.guiltyNPC))
            {
                guiltyNPC = data.guiltyNPC;
                suspectNPC = data.suspectNPC;
                innocentNPC = data.innocentNPC;
                UnityEngine.Debug.Log($"<color=cyan>[DEVAM] Roller kayittan yuklendi -> Suclu: {guiltyNPC}, Supheli: {suspectNPC}, Masum: {innocentNPC}. Hafiza korundu.</color>");
                return; // Yeniden atama YOK, hafiza sifirlama YOK
            }
            // metadata'da rol yoksa (eski kayit) -> asagi dusup yeni gibi kurar
            UnityEngine.Debug.LogWarning("[GameManager] Kayitta rol bilgisi yok, yeni gibi olusturuluyor.");
        }

        // --- YENI OYUN (veya rolsuz eski kayit): rastgele ata, kaydet, hafizayi sifirla ---
        AssignRoles();
        SaveRolesToMetadata(metaPath);
        StartCoroutine(ResetNPCMemory());
    }

    private IEnumerator ResetNPCMemory()
    {
        UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:8000/reset_memory", "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            UnityEngine.Debug.Log("<color=cyan>NPC Hafizalari Sifirlandi! Yeni oyuna haziriz.</color>");
    }

    public void AssignRoles()
    {
        // Aday listesi (Sahnendeki NPC_Roam.npcID degerleri ile BIREBIR ayni olmali!)
        List<string> npcs = new List<string> { "Blacksmith", "Merchant", "Headman" };

        int guiltyIndex = Random.Range(0, npcs.Count);
        guiltyNPC = npcs[guiltyIndex];
        npcs.RemoveAt(guiltyIndex);

        int suspectIndex = Random.Range(0, npcs.Count);
        suspectNPC = npcs[suspectIndex];
        npcs.RemoveAt(suspectIndex);

        innocentNPC = npcs[0];

        UnityEngine.Debug.Log($"<color=green>[GIZEM BASLADI]</color> Suclu: {guiltyNPC}, Supheli: {suspectNPC}, Masum: {innocentNPC}");
    }

    // Rolleri kayit klasorunun metadata.json'ina yazar (saveName/saveDate korunur).
    private void SaveRolesToMetadata(string metaPath)
    {
        try
        {
            GameSaveData data = null;
            if (File.Exists(metaPath))
                data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(metaPath));
            if (data == null) data = new GameSaveData();

            data.guiltyNPC = guiltyNPC;
            data.suspectNPC = suspectNPC;
            data.innocentNPC = innocentNPC;

            Directory.CreateDirectory(Path.GetDirectoryName(metaPath));
            File.WriteAllText(metaPath, JsonUtility.ToJson(data, true));
            UnityEngine.Debug.Log("[GameManager] Roller metadata.json'a kaydedildi.");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("[GameManager] Roller kaydedilemedi: " + e.Message);
        }
    }

    // Secilen NPC'nin rolunu donduren yardimci fonksiyon
    public string GetNpcRole(string npcId)
    {
        if (npcId == guiltyNPC) return "Guilty";
        if (npcId == suspectNPC) return "Suspect";
        return "Innocent";
    }
}
