using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // <-- YENÝ ÝSÝSTEM KÜTÜPHANESÝ EKLENDÝ

public class PauseMenuController : MonoBehaviour
{
    [Header("Menü Panelleri")]
    public GameObject pauseMenuPanel;
    public GameObject settingsPanel;

    private bool isPaused = false;

    void Start()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        // YENÝ INPUT SÝSTEMÝNE GÖRE ESC TUÞU KONTROLÜ
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // --- BUTON FONKSÝYONLARI ---

    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // Oyuna dönerken fareyi tekrar gizle ve merkeze kilitle
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // Menü açýlýrken fareyi GÖRÜNÜR yap ve kilidini AÇ
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OpenSettings()
    {
        pauseMenuPanel.SetActive(false); // Pause menüsünü gizle
        settingsPanel.SetActive(true);   // Ayarlarý göster
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);  // Ayarlarý gizle
        pauseMenuPanel.SetActive(true);  // Pause menüsüne geri dön
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;

        Debug.Log("Kayýt alýndý. Ana Menüye dönülüyor...");
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitToDesktop()
    {
        Debug.Log("Kayýt alýndý. Masaüstüne çýkýlýyor...");
        Application.Quit();
    }
}