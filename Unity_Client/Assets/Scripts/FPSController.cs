using UnityEngine;
using UnityEngine.InputSystem;

public class FPSController : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    public float hiz = 5f;
    public float fareHassasiyeti = 0.1f;
    public float yercekimi = -15f; // AGA YERÇEKÝMÝNÝ BURAYA EKLEDÝK

    private CharacterController controller;
    private Transform kamera;
    private float xRotasyon = 0f;

    // Y eksenindeki düþüþ hýzýmýzý aklýnda tutacak deðiþken
    private Vector3 dususHizi;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        kamera = GetComponentInChildren<Camera>().transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        KameraHareketiniHesapla();
        YurumeHareketiniHesapla();
    }

    private void KameraHareketiniHesapla()
    {
        if (Mouse.current == null) return;

        Vector2 fareFarki = Mouse.current.delta.ReadValue();
        float mouseX = fareFarki.x * fareHassasiyeti;
        float mouseY = fareFarki.y * fareHassasiyeti;

        xRotasyon -= mouseY;
        xRotasyon = Mathf.Clamp(xRotasyon, -90f, 90f);

        kamera.localRotation = Quaternion.Euler(xRotasyon, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void YurumeHareketiniHesapla()
    {
        if (Keyboard.current == null) return;

        // 1. AGA ÖNCE YERDE MÝYÝZ ONU KONTROL EDÝYORUZ
        // Eðer yerdeysek ve düþüþ hýzýmýz eksiye gidiyorsa, onu sýfýrlýyoruz (zemine tam yapýþmak için -2f verilir)
        if (controller.isGrounded && dususHizi.y < 0)
        {
            dususHizi.y = -2f;
        }

        // 2. YÜRÜME ÝÞLEMÝ (Ýleri-Geri / Saða-Sola)
        float x = 0f;
        float z = 0f;

        if (Keyboard.current.wKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;

        Vector3 hareket = transform.right * x + transform.forward * z;
        controller.Move(hareket * hiz * Time.deltaTime);

        // 3. YERÇEKÝMÝ ÝÞLEMÝ (Sürekli aþaðý doðru hýzlanma)
        dususHizi.y += yercekimi * Time.deltaTime;
        controller.Move(dususHizi * Time.deltaTime); // Düþüþü ayrýca uyguluyoruz
    }
}