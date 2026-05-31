using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro kullanýyorsan bu kütüphane þart
using System.Collections.Generic;

public class SettingsController : MonoBehaviour
{
    [Header("Audio (Ses) Arayüz Objeleri")]
    public Slider musicSlider;
    public Slider voiceSlider;
    public Slider soundsSlider;
    public Toggle muteAllToggle;

    [Header("Display (Görüntü) Arayüz Objeleri")]
    public Slider brightnessSlider;
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] resolutions;

    void Start()
    {
        // 1. Bilgisayarýn desteklediði çözünürlükleri bul ve menüye ekle
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);

        // 2. Oyuncu daha önce ayar yaptýysa onlarý yükle, yapmadýysa varsayýlanlarý çek
        LoadSavedSettings(currentResIndex);
    }

    // ==========================================
    // EKRAN (DISPLAY) AYARLARI FONKSÝYONLARI
    // ==========================================
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution res = resolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    public void SetBrightness(float brightness)
    {
        PlayerPrefs.SetFloat("Brightness", brightness);
        // Not: Parlaklýk ayarý oyun sahnesinde Post-Processing ile uygulanýr.
    }

    // ==========================================
    // SES (AUDIO) AYARLARI FONKSÝYONLARI
    // ==========================================
    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetVoiceVolume(float volume)
    {
        PlayerPrefs.SetFloat("VoiceVolume", volume);
    }

    public void SetSoundsVolume(float volume)
    {
        PlayerPrefs.SetFloat("SoundsVolume", volume);
    }

    public void SetMuteAll(bool isMuted)
    {
        PlayerPrefs.SetInt("MuteAll", isMuted ? 1 : 0);
        AudioListener.volume = isMuted ? 0 : 1; // Oyundaki tüm sesi tek tuþla keser/açar
    }

    // ==========================================
    // KAYITLI AYARLARI ARAYÜZE (UI) YANSITMA
    // ==========================================
    private void LoadSavedSettings(int defaultResIndex)
    {
        // Görüntü Ayarlarýný Yükle
        int savedQuality = PlayerPrefs.GetInt("QualityLevel", 2);
        qualityDropdown.value = savedQuality;
        SetQuality(savedQuality);

        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        fullscreenToggle.isOn = isFullscreen;
        SetFullscreen(isFullscreen);

        int savedRes = PlayerPrefs.GetInt("ResolutionIndex", defaultResIndex);
        resolutionDropdown.value = savedRes;
        resolutionDropdown.RefreshShownValue();
        SetResolution(savedRes);

        brightnessSlider.value = PlayerPrefs.GetFloat("Brightness", 1f);

        // Ses Ayarlarýný Yükle
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        voiceSlider.value = PlayerPrefs.GetFloat("VoiceVolume", 1f);
        soundsSlider.value = PlayerPrefs.GetFloat("SoundsVolume", 1f);

        bool isMuted = PlayerPrefs.GetInt("MuteAll", 0) == 1;
        muteAllToggle.isOn = isMuted;
        SetMuteAll(isMuted);
    }
}