using UnityEngine;
using UnityEngine.SceneManagement; // Sahneler arasý geçiþ için bu kütüphane þart

public class MainMenuController : MonoBehaviour
{
    [Header("Menü Panelleri")]
    public GameObject mainMenuPanel;  // Ana menü ekranýn
    public GameObject settingsPanel;  // Ayarlar ekranýn
    public GameObject loadGamePanel;  // Load Game ekranýn

    // CONTINUE veya START GAME butonu için
    public void StartGame()
    {
        // Asýl projene aktardýðýnda yüklenecek sahnenin adýný buraya yazacaksýn
        Debug.Log("Oyun Baþlatýlýyor! AI NPC sistemi yükleniyor...");

        SceneManager.LoadScene(1); // "GameScene" yerine kendi sahne adýný yazmalýsýn
    }

    // SETTINGS butonu için
    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);  // Ana menüyü gizle
        settingsPanel.SetActive(true);   // Ayarlarý göster
    }

    // BACK butonuna týklandýðýnda çalýþacak fonksiyon
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);  // Ayarlarý gizle
        mainMenuPanel.SetActive(true);   // Ana menüyü göster
    }

    public void OpenLoadGame()
    {
        mainMenuPanel.SetActive(false);  // Ana menüyü gizle
        loadGamePanel.SetActive(true);   // Load Game göster
    }

    public void CloseLoadGame()
    {
        loadGamePanel.SetActive(false);  // Load Game gizle
        mainMenuPanel.SetActive(true);   // Ana menüyü göster
    }

    // EXIT butonu için
    public void QuitGame()
    {
        Debug.Log("Oyundan çýkýlýyor...");
        Application.Quit(); // Not: Bu kod Unity editöründe çalýþmaz, oyunu .exe yapýp açtýðýnda çalýþýr.
    }
}
