using UnityEngine;

// Senin o müthiþ IInteractable arayüzünü kullanýyoruz!
public class StealableItem : MonoBehaviour, IInteractable
{
    [Header("Eþya Bilgileri")]
    public string itemName = "Sacred Silver Goblet"; // Llama'nýn eþyanýn ne olduðunu anlamasý için Ýngilizce isim

    [Header("Yakalanma Ayarlarý")]
    public float detectionRadius = 15f; // NPC'lerin bu hýrsýzlýðý fark edebileceði mesafe

    public void Interact()
    {
        // 1. EÞYAYI ÇANTAYA AT (Görünmez yap)
        gameObject.SetActive(false);
        Debug.Log($"<color=yellow>[*] {itemName} çalýndý!</color>");

        // 2. ETRAFTAKÝ NPC'LERÝ TARA (Olay Yeri Ýnceleme)
        // Objenin etrafýna görünmez bir küre çiziyoruz, içine giren adamlarý buluyoruz.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        bool caught = false;

        foreach (var hitCollider in hitColliders)
        {
            NPC_Roam npc = hitCollider.GetComponent<NPC_Roam>();

            // Eðer kürenin içindeki obje bir NPC ise...
            if (npc != null)
            {
                // 3. GÖRME AÇISI KONTROLÜ (Adamýn sýrtý mý dönük?)
                Vector3 directionToPlayer = (transform.position - npc.transform.position).normalized;
                float viewAngle = Vector3.Dot(npc.transform.forward, directionToPlayer);

                // Dot çarpýmý 0'dan büyükse adamýn önündeyiz, bizi %100 gördü demektir!
                if (viewAngle > 0.1f)
                {
                    Debug.Log($"<color=red>[!] {npc.npcID} hýrsýzlýðý GÖRDÜ ve peþine düþtü!</color>");
                    caught = true;

                    // UI'I HEMEN AÇMA! Sadece adamý peþimize takýyoruz.
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    npc.StartChasingPlayer(itemName, player.transform);

                    break; // Sadece bir kiþi peþimize düþsün
                }
            }
        }

        if (!caught)
        {
            Debug.Log("<color=green>Kimse görmedi, tereyaðýndan kýl çeker gibi çaldýk!</color>");
        }
    }

    // 4. AGA BÜYÜ BURADA: Llama'yý Kýþkýrtma Fonksiyonu
    private void ForceNpcReaction(NPC_Roam npc)
    {
        if (DialogSystem.Instance != null && NetworkManager.Instance != null)
        {
            // Sohbet arayüzünü açýyoruz
            DialogSystem.Instance.StartChat(npc);

            // Oyuncu mesajý YOK, ama eylem olarak HIRSIZLIK gönderiyoruz!
            string thePrompt = $"I just STOLE the {itemName} right in front of your eyes!";

            // Metin olarak boþ (""), ama action olarak thePrompt yolluyoruz
            NetworkManager.Instance.SendMessageToServer("", npc.npcID, thePrompt, npc.voiceModel);
        }
    }
}