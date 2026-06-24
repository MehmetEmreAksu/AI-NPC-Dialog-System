using UnityEngine;
using System.Collections;

public class NPCInteraction : MonoBehaviour, IInteractable
{
    public string GetPrompt() => "[E] Konus";

    public void Interact()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Player object is missing.");
            return;
        }

        // 1. Adama sana d�nmesini s�yle
        Vector3 lookDirection = player.transform.position - transform.position;
        lookDirection.y = 0f;
        transform.rotation = Quaternion.LookRotation(lookDirection);

        // 2. ADAM Y�R�YORSA DURDUR (NPC_Roam'u bul ve beklemesini s�yle)
        NPC_Roam npcRoam = GetComponent<NPC_Roam>();
        if (npcRoam != null)
        {
            npcRoam.PrepareForDialog();
        }

        // 3. Dialog sistemine "�u an bu NPC ile konu�uyorum" bilgisini g�nder
        if (DialogSystem.Instance != null)
        {
            DialogSystem.Instance.StartChat(npcRoam); // npcRoam referans�n� yollad�k!
        }

        StartCoroutine(SmoothLookAt(player.transform, this.transform));
    }

    // KAYMAK G�B� D�ND�REN KAMERA B�Y�S�
    private IEnumerator SmoothLookAt(Transform player, Transform npc)
    {
        Vector3 playerToNpc = (npc.position - player.position).normalized;
        Vector3 npcToPlayer = (player.position - npc.position).normalized;

        playerToNpc.y = 0f;
        npcToPlayer.y = 0f;

        Quaternion playerTargetRot = Quaternion.LookRotation(playerToNpc);
        Quaternion npcTargetRot = Quaternion.LookRotation(npcToPlayer);

        float time = 0f;
        float duration = 0.5f; // Yar�m saniyede usulca d�necekler

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