using UnityEngine;

// Kuyunun yanindaki odul item'e eklenir. Oyuncu E ile alinca oyunu kazandirir.
// NOT: Bu objede bir Collider olmali ki PlayerInteraction raycast'i carpsin.
public class WellItem : MonoBehaviour, IInteractable
{
    public string GetPrompt() => "[E] Al";

    public void Interact()
    {
        // Item alindi -> gizle ve kazanma ekranini ac.
        gameObject.SetActive(false);

        if (EndGameManager.Instance != null)
            EndGameManager.Instance.WinGame();
        else
            Debug.LogWarning("[WellItem] EndGameManager bulunamadi! Kazanma ekrani acilamadi.");
    }
}
