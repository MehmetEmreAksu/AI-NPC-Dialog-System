using System.Diagnostics;
using System.IO;
using UnityEngine;

public class PythonLauncher : MonoBehaviour
{
    // Tek bir backend sunucusu olsun diye singleton. Sahne degisse de yasamaya devam eder.
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
        string fileName;
        string arguments;
        string workingDir;
        bool showWindow;

        // Unity'nin (bu surecin) PID'i. Backend'e arguman olarak gecilir; backend bu surec
        // olunce (crash/force-close dahil) kendini kapatir. Boylece zombi exe kalmaz.
        int unityPid = Process.GetCurrentProcess().Id;

        if (Application.isEditor)
        {
            // EDITOR/DEV: kaynak .py dosyasini python ile calistir (gelistirici makinesinde Python var).
            string projectRoot = Directory.GetParent(Application.dataPath).Parent.FullName;
            string scriptPath = Path.Combine(projectRoot, "Python_Backend", "app_groq_voice.py");

            if (!File.Exists(scriptPath))
            {
                UnityEngine.Debug.LogError("[PythonLauncher] app_groq_voice.py bulunamadi! Beklenen yol:\n" + scriptPath);
                return;
            }

            fileName = "python";
            arguments = "\"" + scriptPath + "\" " + unityPid; // script + izlenecek Unity PID
            workingDir = Path.GetDirectoryName(scriptPath);
            showWindow = true; // dev'de terminali gor (debug kolayligi)
        }
        else
        {
            // BUILD: StreamingAssets/Backend icindeki paketlenmis exe'yi calistir (hedef PC'de Python GEREKMEZ).
            string backendDir = Path.Combine(Application.streamingAssetsPath, "Backend");
            string exePath = Path.Combine(backendDir, "app_groq_voice.exe");

            if (!File.Exists(exePath))
            {
                UnityEngine.Debug.LogError("[PythonLauncher] Backend exe bulunamadi! Beklenen yol:\n" + exePath);
                return;
            }

            fileName = exePath;
            arguments = unityPid.ToString(); // izlenecek Unity PID
            workingDir = backendDir; // apiKey.txt burada okunur
            showWindow = false;      // oyuncuya siyah terminal gosterme
        }

        try
        {
            pythonProcess = new Process();
            pythonProcess.StartInfo.FileName = fileName;
            pythonProcess.StartInfo.Arguments = arguments;
            pythonProcess.StartInfo.WorkingDirectory = workingDir;
            pythonProcess.StartInfo.UseShellExecute = true;
            pythonProcess.StartInfo.CreateNoWindow = !showWindow;
            pythonProcess.StartInfo.WindowStyle = showWindow ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;

            pythonProcess.Start();
            UnityEngine.Debug.Log("<color=green>[*] Backend sunucusu baslatildi!</color>");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("[PythonLauncher] Backend baslatilamadi: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        // 1. ZIRH: Senkron HTTP Sinyali (Oyun kapanmadan 0.1 sn �nce paketi zorla �akar)
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendShutdownSignal();
        }

        // 2. ZIRH: ��letim Sistemi Seviyesinde Tree Kill (A�a� Katliam�)
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            try
            {
                // PyInstaller'�n yaratt��� zombi alt programlar� silmek i�in /T (Tree) komutu at�l�r
                Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/PID {pythonProcess.Id} /T /F", // /T = T�m s�laleyi kes, /F = Zorla
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                UnityEngine.Debug.Log("<color=red>[*] Backend s�lalesi taskkill ile kaz�nd�.</color>");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning("Taskkill at�lamad�, normal kill deneniyor: " + e.Message);
                pythonProcess.Kill();
            }
        }
    }

    // Unity Play tusunu aniden keserse (veya obje yok olursa) zombi sunucu kalmasin
    void OnDestroy()
    {
        if (Instance == this && pythonProcess != null)
        {
            try
            {
                if (!pythonProcess.HasExited)
                {
                    // 1. ZIRH: Sinyali ate�le
                    if (NetworkManager.Instance != null)
                    {
                        NetworkManager.Instance.SendShutdownSignal();
                    }

                    // 2. ZIRH: Process'i �ld�r
                    pythonProcess.Kill();
                    pythonProcess.Dispose();
                    UnityEngine.Debug.Log("<color=red>[*] Backend sunucusu OnDestroy ile kapatildi.</color>");
                }
            }
            catch (System.Exception)
            {
                // Terminali biz elle kapattiysak Unity hata vermesin diye sessizce yutuyoruz
            }
        }
    }
}
