using System.Diagnostics;
using UnityEngine;

public class PythonLauncher : MonoBehaviour
{
    private Process pythonProcess;

    void Awake()
    {
        // Python'u görünmez bir konsol olarak arka planda baþlat
        pythonProcess = new Process();
        // Aga buraya kendi Python.exe yolunu ve app_groq_voice.py dosyanýn tam yolunu yazacaksýn
        pythonProcess.StartInfo.FileName = "python";
        pythonProcess.StartInfo.Arguments = Application.dataPath + "C:\\Users\\qwert\\UnityProjects\\Computer_Project\\Python_Backendapp_groq_voice.py";
        pythonProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // Siyah ekran çýkmasýn
        pythonProcess.StartInfo.UseShellExecute = false;
        pythonProcess.Start();
        UnityEngine.Debug.Log("<color=green>[*] Python Sunucusu arka planda ayaklandý!</color>");
    }

    void OnApplicationQuit()
    {
        // Oyun kapanýrken (veya Unity Editörden çýkarken) Python'un fiþini çek!
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            UnityEngine.Debug.Log("<color=red>[*] Python Sunucusu kapatýldý.</color>");
        }
    }
}