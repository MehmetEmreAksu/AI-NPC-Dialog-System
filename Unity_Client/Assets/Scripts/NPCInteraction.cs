using UnityEngine;
using System.Collections;

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

        StartCoroutine(SmoothLookAt(player.transform, this.transform));
    }

    // KAYMAK GÝBÝ DÖNDÜREN KAMERA BÜYÜSÜ
    private IEnumerator SmoothLookAt(Transform player, Transform npc)
    {
        Vector3 playerToNpc = (npc.position - player.position).normalized;
        Vector3 npcToPlayer = (player.position - npc.position).normalized;

        playerToNpc.y = 0f;
        npcToPlayer.y = 0f;

        Quaternion playerTargetRot = Quaternion.LookRotation(playerToNpc);
        Quaternion npcTargetRot = Quaternion.LookRotation(npcToPlayer);

        float time = 0f;
        float duration = 0.5f; // Yarým saniyede usulca dönecekler

        while (time < duration)
        {
            player.rotation = Quaternion.Slerp(player.rotation, playerTargetRot, time / duration);
            npc.rotation = Quaternion.Slerp(npc.rotation, npcTargetRot, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        player.rotation = playerTargetRot;
        npc.rotation = npcTargetRot;
    }
}