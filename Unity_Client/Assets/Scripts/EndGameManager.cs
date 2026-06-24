using UnityEngine;
using UnityEngine.SceneManagement;

// Oyunun kazanma akisini yoneten merkezi singleton:
// 1) Suclu itiraf edince (NetworkManager -> OnConfession) kuyudaki gizli item belirir.
// 2) Oyuncu o item'i alinca (WellItem -> WinGame) kazanma ekrani acilir.
public class EndGameManager : MonoBehaviour
{
    public static EndGameManager Instance { get; private set; }

    [Header("Bitis Akisi Referanslari")]
    public GameObject hiddenItem;   // Kuyunun yanindaki odul item (basta KAPALI olmali)
    public GameObject winPanel;     // Kazanma ekrani paneli (basta KAPALI olmali)

    private bool revealed = false;  // Item bir kez belirsin, itiraf tekrarinda resetlenmesin

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Guvenlik: sahnede yanlislikla acik kalmis olabilir, basta kapatalim.
        if (hiddenItem != null) hiddenItem.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
    }

    // Suclu NPC itiraf ettiginde cagrilir: kuyudaki item'i ortaya cikarir.
    public void OnConfession()
    {
        if (revealed) return; // Tek seferlik
        revealed = true;

        if (hiddenItem != null)
        {
            hiddenItem.SetActive(true);
            Debug.Log("<color=lime>[EndGame] Itiraf alindi! Kuyunun yaninda item belirdi.</color>");
        }
        else
        {
            Debug.LogWarning("[EndGame] OnConfession cagrildi ama hiddenItem atanmamis!");
        }
    }

    // Oyuncu kuyudaki item'i aldiginda cagrilir: kazanma ekranini acar, oyunu dondurur.
    public void WinGame()
    {
        if (winPanel != null) winPanel.SetActive(true);
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("<color=yellow>[EndGame] Oyun kazanildi!</color>");
    }

    // --- KAZANMA EKRANI BUTONLARI ---

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitToDesktop()
    {
        Application.Quit();
    }
}
