using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // <-- YEN� �S�STEM K�T�PHANES� EKLEND�

public class PauseMenuController : MonoBehaviour
{
    [Header("Men� Panelleri")]
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
        // YENI INPUT SISTEMINE GORE ESC TUSU KONTROLU (tek elden)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 1) Diyalog acikken ESC -> pause ACMA, sadece diyalogu kapat.
            if (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogOpen)
            {
                DialogSystem.Instance.StopDialog();
            }
            // 2) Zaten pause'daysak -> oyuna don.
            else if (isPaused)
            {
                ResumeGame();
            }
            // 3) Diyalog yok, pause da yok -> pause menusunu ac.
            else
            {
                PauseGame();
            }
        }
    }

    // --- BUTON FONKS�YONLARI ---

    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // Oyuna d�nerken fareyi tekrar gizle ve merkeze kilitle
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // Men� a��l�rken fareyi G�R�N�R yap ve kilidini A�
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OpenSettings()
    {
        pauseMenuPanel.SetActive(false); // Pause men�s�n� gizle
        settingsPanel.SetActive(true);   // Ayarlar� g�ster
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);  // Ayarlar� gizle
        pauseMenuPanel.SetActive(true);  // Pause men�s�ne geri d�n
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;

        Debug.Log("Kay�t al�nd�. Ana Men�ye d�n�l�yor...");
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitToDesktop()
    {
        Debug.Log("Kay�t al�nd�. Masa�st�ne ��k�l�yor...");
        Application.Quit();
    }
}