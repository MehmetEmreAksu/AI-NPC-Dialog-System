# 🤖 AI-Powered Autonomous NPC Dialog System

Bu proje, modern bir RPG oyunundaki NPC'lerin önceden yazılmış metinleri okumak yerine; oyuncunun sesini ve mesajlarını anlayabilen, hafızasına kaydeden ve buna göre duygusal tepkiler (ses ve animasyon) verebilen **uçtan uca otonom bir yapay zeka mimarisidir**.

Sistem, **Unity (Client)** ve **Python (Backend)** hibrit yapısı üzerine kurulmuştur.

---

## 🚀 Öne Çıkan Özellikler

* **🎙️ Çift Yönlü Etkileşim (Dual-Input):** Oyuncu, klavye veya mikrofon (Bas-Konuş) ile NPC ile etkileşime girebilir.
* **🧠 Llama-3.3 Bilişsel Motor:** NPC'nin kişiliği, kararları ve duygusal durumları Llama-3.3 tarafından milisaniyeler içinde JSON formatında işlenir.
* **🎭 Duygu Odaklı Animasyon (Emotion-Driven):** Yapay zeka sadece metin değil, "öfke, korku, şüphe" gibi duygu parametreleri üreterek Unity'deki Animator'ı eşzamanlı tetikler.
* **🗣️ Dinamik Seslendirme (Orpheus TTS):** NPC, üretilen metni salt bir şekilde okumaz; duygusuna uygun vokal etiketlerle (bağırma, fısıldama) gerçekçi bir şekilde seslendirir.
* **💾 Uzun Süreli Hafıza (RAG & ChromaDB):** NPC, geçmiş konuşmaları hatırlar. Konuşmalar vektör veritabanına kaydedilir ve Llama tarafından otomatik olarak özetlenir.
* **⚡ Düşük Gecikmeli Senkronizasyon:** Sesin uzunluğuna göre dinamik olarak hesaplanan "Typewriter" (yazı akış) algoritması.

---

## 🛠️ Kullanılan Teknolojiler

### Client (İstemci) - Unity
* **Oyun Motoru:** Unity 3D (C#)
* **Ağ:** UnityWebRequest (HTTP/REST)
* **Sistemler:** State Machine Animator, Dinamik AudioClip Streaming

### Backend (Sunucu) - Python
* **Çatı:** Flask (REST API)
* **LLM & AI:** Groq Cloud SDK (Llama-3.3, Whisper-v3)
* **Ses Motoru:** Canopy Labs Orpheus (TTS), Soundfile, Scipy (16-bit PCM)
* **Veritabanı:** ChromaDB (Vektör Veritabanı)

---

## ⚙️ Kurulum ve Çalıştırma

### 1. Python Backend Hazırlığı
Terminali açın ve gerekli kütüphaneleri yükleyin:

```bash
pip install flask groq chromadb soundfile scipy numpy
```

`main.py` içerisindeki API key değişkenine kendi Groq anahtarınızı girin ve sunucuyu başlatın:

```bash
python main.py
```

### 2. Unity Client Ayarları
1. `Unity_Client` klasörünü Unity Hub ile açın.
2. Sahnede `NetworkManager` scriptinin bulunduğu objeyi seçin.
3. `API Url` kısmına `http://127.0.0.1:8000/chat` yazılı olduğundan emin olun.
4. Oyunu başlatın ve sohbet etmek için **V** tuşuna basılı tutun!

---

## 📐 Sistem Akış Mimarisi

1. **Girdi:** Oyuncu sesini (veya metnini) gönderir.
2. **İşleme (Python):** Sesi metne çevirir (STT), ChromaDB'den eski anıları çeker, LLM'e yollar.
3. **Karar (LLM):** Karakterin durumuna göre tepki (metin) ve duygu (JSON) üretir.
4. **Sentezleme:** Metin, duyguya uygun ses dalgasına (WAV) dönüştürülür ve PCM 16-bit olarak formatlanır.
5. **Çıktı (Unity):** JSON verisi alınır, ilgili animasyon tetiklenir, ses indirilir ve dudak senkronizasyonu ile ekrana yansıtılır.

---

**Geliştirici:** [Mehmet Emre Aksu](https://github.com/MehmetEmreAksu) | YTÜ Bilgisayar Mühendisliği
