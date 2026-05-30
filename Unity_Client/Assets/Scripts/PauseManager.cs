using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (DialogSystem.Instance != null && DialogSystem.Instance.dialogPanel.activeSelf)
            {
                DialogSystem.Instance.StopDialog();
                return;
            }
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // Zamaný normal akýþýna al
        isPaused = false;

        // Oyuna dönünce fareyi tekrar gizle ve ortaya kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // Zamaný dondur
        isPaused = true;

        // Menüde týklayabilmek için fareyi serbest býrak
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SaveGame()
    {
        // Aga buraya þimdilik dummy (kukla) kod koyuyoruz
        Debug.Log("Oyun kaydedildi! (Altyapý hazýr)");
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f; // Çýkarken zamaný düzeltmeyi unutma, yoksa menü de donar!
        SceneManager.LoadScene(0); // 0 numaralý sahneye (Ana Menü) dön
    }
}