using System.Diagnostics;
using System.IO;
using UnityEngine;

public class PythonLauncher : MonoBehaviour
{
    // Tek bir Python sunucusu olsun diye singleton. Sahne degisse de yasamaya devam eder.
    public static PythonLauncher Instance { get; private set; }

    private Process pythonProcess;

    void Awake()
    {
        // Zaten bir launcher varsa (ornek: Main sahnesinde ikinci kopya) bu kopyayi yok et.
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject); // MainMenu -> Main gecisinde sunucu olmesin.

        StartPythonServer();
    }

    private void StartPythonServer()
    {
        // app_groq_voice.py'nin tam yolunu projeden bagimsiz hesapla.
        // Application.dataPath => .../Unity_Client/Assets
        // Iki ust klasor => .../Computer_Project ; oradan Python_Backend'e gir.
        string projectRoot = Directory.GetParent(Application.dataPath).Parent.FullName;
        string scriptPath = Path.Combine(projectRoot, "Python_Backend", "app_groq_voice.py");

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError(
                "[PythonLauncher] app_groq_voice.py bulunamadi! Beklenen yol:\n" + scriptPath);
            return;
        }

        try
        {
            pythonProcess = new Process();
            pythonProcess.StartInfo.FileName = "python";
            // Yolu tirnak icine al ki bosluklu klasor adlari (Computer Project) bozmasin.
            pythonProcess.StartInfo.Arguments = "\"" + scriptPath + "\"";
            pythonProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(scriptPath);
            pythonProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // Siyah ekran cikmasin
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.StartInfo.CreateNoWindow = true;
            pythonProcess.Start();
            UnityEngine.Debug.Log("<color=green>[*] Python Sunucusu arka planda ayakland!</color>");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("[PythonLauncher] Python baslatilamadi: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        // Oyun kapanirken (veya Unity Editorden cikarken) Python'un fisini cek!
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            UnityEngine.Debug.Log("<color=red>[*] Python Sunucusu kapatildi.</color>");
        }
    }
}
