using UnityEngine;

public class NPCInteraction : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Player object is missing.");
            return;
        }

        // 1. Adama sana dönmesini söyle
        Vector3 lookDirection = player.transform.position - transform.position;
        lookDirection.y = 0f;
        transform.rotation = Quaternion.LookRotation(lookDirection);

        // 2. ADAM YÜRÜYORSA DURDUR (NPC_Roam'u bul ve beklemesini söyle)
        NPC_Roam npcRoam = GetComponent<NPC_Roam>();
        if (npcRoam != null)
        {
            npcRoam.PrepareForDialog();
        }

        // 3. Dialog sistemine "Þu an bu NPC ile konuþuyorum" bilgisini gönder
        if (DialogSystem.Instance != null)
        {
            DialogSystem.Instance.StartChat(npcRoam); // npcRoam referansýný yolladýk!
        }
    }
}