using UnityEngine;
using UnityEngine.UI; // Buton kontrolü için gerekli
using System.IO;
using System.Linq;

public class MainMenuManager : MonoBehaviour
{
    [Header("Arayüz Elemanlarý")]
    // Tasarýmýndaki "Continue" butonunu buraya sürükleyeceðiz
    public Button btnContinue;

    void Start()
    {
        // Menü ilk açýldýðýnda kayýt var mý yok mu kontrol et
        CheckSaveFiles();
    }

    private void CheckSaveFiles()
    {
        string savesDirectory = Path.Combine(Application.persistentDataPath, "saves");

        // Kayýt klasörü yoksa veya içi tamamen boþsa Continue butonunu kapat
        if (!Directory.Exists(savesDirectory) || new DirectoryInfo(savesDirectory).GetDirectories().Length == 0)
        {
            btnContinue.interactable = false; // Buton soluklaþýr ve týklanamaz olur
        }
        else
        {
            btnContinue.interactable = true; // Kayýt bulundu, buton aktif
        }
    }

    // Bu metodu Continue butonunun OnClick() kýsmýna baðlayacaðýz
    public void LoadLatestSave()
    {
        string savesDirectory = Path.Combine(Application.persistentDataPath, "saves");

        if (Directory.Exists(savesDirectory))
        {
            DirectoryInfo dirInfo = new DirectoryInfo(savesDirectory);

            // Tüm save klasörlerini tarihe göre sýrala ve "FirstOrDefault()" ile sadece en baþtakini al
            DirectoryInfo latestSaveFolder = dirInfo.GetDirectories()
                                                    .OrderByDescending(d => d.LastWriteTime)
                                                    .FirstOrDefault();

            if (latestSaveFolder != null)
            {
                string folderName = latestSaveFolder.Name;
                Debug.Log($"[CONTINUE] En son kayýt bulundu: {folderName}. Python sunucusuna istek atýlýyor...");

                // TODO: Load Game kýsmýnda yapacaðýmýz gibi, bu 'folderName' bilgisini Python'a yollayýp sahneyi yükleyeceðiz.
            }
        }
    }
}