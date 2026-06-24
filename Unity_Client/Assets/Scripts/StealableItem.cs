using UnityEngine;

// Senin o m�thi� IInteractable aray�z�n� kullan�yoruz!
public class StealableItem : MonoBehaviour, IInteractable
{
    [Header("E�ya Bilgileri")]
    public string itemName = "Sacred Silver Goblet"; // Llama'n�n e�yan�n ne oldu�unu anlamas� i�in �ngilizce isim

    [Header("Yakalanma Ayarlar�")]
    public float detectionRadius = 15f; // NPC'lerin bu h�rs�zl��� fark edebilece�i mesafe

    public string GetPrompt() => "[E] Cal";

    public void Interact()
    {
        // 1. E�YAYI �ANTAYA AT (G�r�nmez yap)
        gameObject.SetActive(false);
        Debug.Log($"<color=yellow>[*] {itemName} �al�nd�!</color>");

        // 2. ETRAFTAK� NPC'LER� TARA (Olay Yeri �nceleme)
        // Objenin etraf�na g�r�nmez bir k�re �iziyoruz, i�ine giren adamlar� buluyoruz.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        bool caught = false;

        foreach (var hitCollider in hitColliders)
        {
            NPC_Roam npc = hitCollider.GetComponent<NPC_Roam>();

            // E�er k�renin i�indeki obje bir NPC ise...
            if (npc != null)
            {
                // 3. G�RME A�ISI KONTROL� (Adam�n s�rt� m� d�n�k?)
                Vector3 directionToPlayer = (transform.position - npc.transform.position).normalized;
                float viewAngle = Vector3.Dot(npc.transform.forward, directionToPlayer);

                // Dot �arp�m� 0'dan b�y�kse adam�n �n�ndeyiz, bizi %100 g�rd� demektir!
                if (viewAngle > 0.1f)
                {
                    Debug.Log($"<color=red>[!] {npc.npcID} h�rs�zl��� G�RD� ve pe�ine d��t�!</color>");
                    caught = true;

                    // UI'I HEMEN A�MA! Sadece adam� pe�imize tak�yoruz.
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    npc.StartChasingPlayer(itemName, player.transform);

                    break; // Sadece bir ki�i pe�imize d��s�n
                }
            }
        }

        if (!caught)
        {
            Debug.Log("<color=green>Kimse g�rmedi, tereya��ndan k�l �eker gibi �ald�k!</color>");
        }
    }

    // 4. AGA B�Y� BURADA: Llama'y� K��k�rtma Fonksiyonu
    private void ForceNpcReaction(NPC_Roam npc)
    {
        if (DialogSystem.Instance != null && NetworkManager.Instance != null)
        {
            // Sohbet aray�z�n� a��yoruz
            DialogSystem.Instance.StartChat(npc);

            // Oyuncu mesaj� YOK, ama eylem olarak HIRSIZLIK g�nderiyoruz!
            string thePrompt = $"I just STOLE the {itemName} right in front of your eyes!";

            // Metin olarak bo� (""), ama action olarak thePrompt yolluyoruz
            NetworkManager.Instance.SendMessageToServer("", npc.npcID, thePrompt, npc.voiceModel);
        }
    }
}