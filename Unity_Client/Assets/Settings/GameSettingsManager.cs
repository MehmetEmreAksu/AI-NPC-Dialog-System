using UnityEngine;
using UnityEngine.Audio; // AudioMixer'i kontrol etmek için þart

public class GameSettingsManager : MonoBehaviour
{
    public AudioMixer mainMixer;
    public Light mainDirectionalLight; // Sahnenin ana güneþi

    void Awake()
    {
        // Awake metodu, sahne yüklenirken daha oyuncu gözünü açmadan önce çalýþýr.
        ApplyAudioSettings();
        ApplyBrightness();
    }

    private void ApplyAudioSettings()
    {
        // 1. Mute (Sustur) Kontrolü
        bool isMuted = PlayerPrefs.GetInt("MuteAll", 0) == 1;
        AudioListener.volume = isMuted ? 0f : 1f;

        // 2. Hafýzadaki ses seviyelerini çek 
        // (0.0001f yapýyoruz çünkü ses formülünde 0 deðeri hata verir)
        float musicVol = Mathf.Max(0.0001f, PlayerPrefs.GetFloat("MusicVolume", 1f));
        float voiceVol = Mathf.Max(0.0001f, PlayerPrefs.GetFloat("VoiceVolume", 1f));
        float soundsVol = Mathf.Max(0.0001f, PlayerPrefs.GetFloat("SoundsVolume", 1f));

        // 3. Slider'ýn 0-1 deðerini, Unity Mixer'in -80dB ile 0dB aralýðýna çevir ve uygula
        mainMixer.SetFloat("MusicVol", Mathf.Log10(musicVol) * 20f);
        mainMixer.SetFloat("VoiceVol", Mathf.Log10(voiceVol) * 20f);
        mainMixer.SetFloat("SoundsVol", Mathf.Log10(soundsVol) * 20f);
    }

    private void ApplyBrightness()
    {
        // Basit Parlaklýk Çözümü: Sahnedeki güneþin (Directional Light) þiddetini deðiþtirir
        float brightness = PlayerPrefs.GetFloat("Brightness", 1f);

        if (mainDirectionalLight != null)
        {
            // Slider 0 ile 2 arasýndaysa, ýþýk þiddetini buna göre ayarlar
            mainDirectionalLight.intensity = brightness;
        }
    }
}