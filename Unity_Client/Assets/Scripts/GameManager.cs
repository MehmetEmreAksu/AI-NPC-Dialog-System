using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics; // OS Process yönetimi için þart


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Current Game State")]
    public string guiltyNPC;
    public string suspectNPC;
    public string innocentNPC;

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
        AssignRoles();
        StartCoroutine(ResetNPCMemory());
    }
    private IEnumerator ResetNPCMemory()
    {
        UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:8000/reset_memory", "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            UnityEngine.Debug.Log("<color=cyan>NPC Hafýzalarý Sýfýrlandý! Yeni oyuna hazýrýz.</color>");
    }

    public void AssignRoles()
    {
        // Aday listesi (Sahnendeki NPC ID'leri ile BÝREBÝR ayný olmalý)
        List<string> npcs = new List<string> { "Demirci", "Tuccar", "Muhtar" };

        // 1. Suçluyu seç ve listeden çýkar
        int guiltyIndex = Random.Range(0, npcs.Count);
        guiltyNPC = npcs[guiltyIndex];
        npcs.RemoveAt(guiltyIndex);

        // 2. Þüpheliyi seç ve listeden çýkar
        int suspectIndex = Random.Range(0, npcs.Count);
        suspectNPC = npcs[suspectIndex];
        npcs.RemoveAt(suspectIndex);

        // 3. Kalan kiþi masumdur
        innocentNPC = npcs[0];

        UnityEngine.Debug.Log($"<color=green>[GÝZEM BAÞLADI]</color> Suçlu: {guiltyNPC}, Þüpheli: {suspectNPC}, Masum: {innocentNPC}");
    }

    // Seçilen NPC'nin rolünü döndüren yardýmcý fonksiyon
    public string GetNpcRole(string npcId)
    {
        if (npcId == guiltyNPC) return "Guilty";
        if (npcId == suspectNPC) return "Suspect";
        return "Innocent";
    }
}
