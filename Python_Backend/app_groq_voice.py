import json
import os
import uuid
import io
import tempfile
import getpass
import numpy as np
import soundfile as sf
import scipy.io.wavfile as wavfile
import chromadb
from datetime import datetime
from pathlib import Path
from flask import Flask, request, jsonify, send_file
from groq import Groq

app = Flask(__name__)

# Ses dosyasini DAIMA mutlak bir yola yaz/oku. Frozen exe'de goreceli "current_voice.wav"
# Flask tarafindan gecici _MEI klasorune gore aranip /get_audio'da 500 veriyordu.
# temp klasoru hem mutlak hem garanti yazilabilir -> hem editör hem build'de calisir.
SES_DOSYA_YOLU = os.path.join(tempfile.gettempdir(), "npc_current_voice.wav")

# ==========================================
# 1. YAPAY ZEKA VE API KONFİGÜRASYONU
# ==========================================
# API anahtarını önce ortam değişkeninden, yoksa apiKey.txt dosyasından oku.
# apiKey.txt .gitignore'da — repoya ASLA commit edilmez.
def _load_api_key() -> str:
    env_key = os.environ.get("GROQ_API_KEY")
    if env_key:
        return env_key.strip()

    import sys
    # PyInstaller ile .exe olunca __file__ gecici klasore isaret eder; bu yuzden
    # apiKey.txt'yi birkac olasi konumda ariyoruz (exe yani, kaynak yani, calisma dizini).
    aday_klasorler = []
    if getattr(sys, "frozen", False):
        aday_klasorler.append(Path(sys.executable).parent)  # exe'nin yanindaki klasor
    aday_klasorler.append(Path(__file__).parent)            # kaynak dosya yani (editör/dev)
    aday_klasorler.append(Path.cwd())                        # calisma dizini

    for klasor in aday_klasorler:
        key_file = klasor / "apiKey.txt"
        if key_file.exists():
            return key_file.read_text(encoding="utf-8").strip()

    raise RuntimeError(
        "Groq API anahtarı bulunamadı. GROQ_API_KEY ortam değişkenini ayarla "
        "ya da apiKey.txt dosyasini exe'nin (veya Python_Backend'in) yanina koy."
    )

client = Groq(api_key=_load_api_key())

# ==========================================
# 2. HAFIZA YÖNETİMİ AYARLARI
# ==========================================
gecici_hafiza = {}
MAX_MESAJ_SINIRI = 10  # 10 mesajda bir özetleyip kalıcı hafızaya atar

# Başlangıçta bunları boş bırakıyoruz, Unity'den komut gelince dolacaklar
chroma_istemci = None
bellek = None
aktif_save_klasoru = None

def veritabani_baglatisini_kur(save_folder_name):
    global chroma_istemci, bellek, aktif_save_klasoru
    
    # 1. KONTROL: Eğer geçici (default) kurulum yapılıyorsa Unity klasörüne DOKUNMA
    if save_folder_name == "default_save":
        db_path = os.path.join(os.getcwd(), "temp_gecici_bellek")
        print("\n[BİLGİ] Sunucu varsayılan (geçici) bellek ile başlatıldı.")
    
    # 2. GERÇEK KAYIT: Eğer Unity'den "save_01" gibi gerçek bir komut gelirse AppData'ya git
    else:
        kullanici_profili = os.environ.get('USERPROFILE')
        local_low_yolu = os.path.join(kullanici_profili, 'AppData', 'LocalLow')
        
        # --------------------------------------------------------------------------- ÖNEMLİ
        # --------------------------------------------------------------------------- ÖNEMLİ
        # --------------------------------------------------------------------------- ÖNEMLİ
        company_name = "BabanınYeri"        # Unity Player Settings > Company Name ile BIREBIR ayni
        # --------------------------------------------------------------------------- ÖNEMLİ
        # --------------------------------------------------------------------------- ÖNEMLİ
        # --------------------------------------------------------------------------- ÖNEMLİ
        game_name = "AI NPC Simulator"       # Unity Player Settings > Product Name ile BIREBIR ayni
        # --------------------------------------------------------------------------- ÖNEMLİ
        # --------------------------------------------------------------------------- ÖNEMLİ
        # --------------------------------------------------------------------------- ÖNEMLİ
        
        unity_save_path = os.path.join(local_low_yolu, company_name, game_name, "saves")
        db_path = os.path.join(unity_save_path, save_folder_name, "npc_bellek")
        
        print(f"\n[HİZALANDI] Python artık Unity'nin save klasörüne bakıyor.")
    
    # Ortak İşlemler (Klasörü oluştur ve ChromaDB'yi bağla)
    os.makedirs(db_path, exist_ok=True)
    
    chroma_istemci = chromadb.PersistentClient(path=db_path)
    bellek = chroma_istemci.get_or_create_collection(name="anilar")
    aktif_save_klasoru = save_folder_name
    
    print(f"Hedef Yol: {db_path}\n")

# Geliştirme aşamasında sunucu ilk açıldığında çökmemesi için varsayılan bir DB başlatalım
veritabani_baglatisini_kur("default_save")

# ==========================================
# 3. ÖZETLEME FONKSİYONU
# ==========================================
def anilari_ozetle_ve_kaydet(player_id, npc_id,gecmis_liste):
    if not gecmis_liste:
        return

    print(f"\n[{player_id}] için {len(gecmis_liste)} adet etkileşim özetleniyor...")
    birlesik_metin = "\n".join(gecmis_liste)

    ozet_prompt = f"""
    Summarize the following continuous interaction between a Player and an NPC into ONE concise sentence. 
    Focus on the main events, the player's attitude, and the NPC's reaction.
    Interactions:
    {birlesik_metin}
    Return ONLY the summary sentence, nothing else.
    """

    try:
        completion = client.chat.completions.create(
            messages=[{"role": "user", "content": ozet_prompt}],
            model="llama-3.3-70b-versatile",
            temperature=0.3
        )
        ozet_ani = completion.choices[0].message.content.strip()

        gercek_zaman = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        yeni_ani_id = str(uuid.uuid4())

        bellek.add(
            documents=[ozet_ani],
            metadatas=[{"zaman": gercek_zaman, "player_id": player_id, "npc_id": npc_id}],
            ids=[yeni_ani_id]
        )
        print(f"[+] Özet başarıyla ChromaDB'ye kaydedildi: {ozet_ani}\n")

    except Exception as e:
        print(f"HATA - Özetleme işlemi başarısız: {e}")


# ==========================================
# 4. ANA MERKEZ: NPC MANTIĞI (Birleştirilmiş)
# ==========================================
# Fonksiyona 3 yeni parametre ekledik!
def npc_beynini_calistir(player_id, npc_id, player_message, player_action, voice_model, npc_role, suspect_name, guilty_name, time_of_day):

    hafiza_anahtari = f"{player_id}_{npc_id}"
    if hafiza_anahtari not in gecici_hafiza:
        gecici_hafiza[hafiza_anahtari] = []

    # ==========================================
    # DİNAMİK ROL VE PROMPT SİSTEMİ (6 KOMBİNASYON)
    # ==========================================
    gizli_talimat = ""

    if npc_id == "Blacksmith":
        if npc_role == "Innocent":
            gizli_talimat = f"You are innocent. You didn't steal the Silver Goblet. You saw {suspect_name} acting very nervous near the well. Tell the player to question them."
        elif npc_role == "Suspect":
            gizli_talimat = f"You did NOT steal the Silver Goblet. BUT last night you were secretly forging illegal weapons. You are defensive. IF the player is polite, reveal you saw {guilty_name} sneaking around the crime scene. Otherwise, tell them to get out."
        elif npc_role == "Guilty":
            gizli_talimat = f"You ARE the thief who stole the Silver Goblet. Deny everything aggressively. BUT, IF the player mentions the clue from {suspect_name}, you MUST panic, confess to the crime, set your JSON npc_action to 'confess', and say 'I hid the goblet by the old well!'"

    elif npc_id == "Merchant":
        if npc_role == "Innocent":
            gizli_talimat = f"You are innocent. You don't know who stole the Goblet, but you noticed {suspect_name} looking very guilty. Advise the player to investigate them, while trying to sell junk."
        elif npc_role == "Suspect":
            gizli_talimat = f"You did NOT steal the Goblet. BUT last night you were smuggling untaxed silk. You are sweating and paranoid. IF the player promises to keep your secret, tell them you saw {guilty_name} acting suspicious. Otherwise, deny everything."
        elif npc_role == "Guilty":
            gizli_talimat = f"You ARE the thief. Play the role of an innocent merchant. BUT, IF the player mentions the clue from {suspect_name}, your facade breaks. You MUST confess, set your JSON npc_action to 'confess', and cry 'I hid it by the old well!'"

    elif npc_id == "Headman":
        if npc_role == "Innocent":
            gizli_talimat = f"You are innocent and stressed about the stolen Goblet. You noticed {suspect_name} acting strangely. Order the player to question them."
        elif npc_role == "Suspect":
            gizli_talimat = f"You did NOT steal the Goblet. BUT you have been embezzling village tax money. IF the player shows respect, admit you saw {guilty_name} sneaking around. Otherwise, threaten to banish the player."
        elif npc_role == "Guilty":
            gizli_talimat = f"You ARE the thief. Act authoritative and dismissive. BUT, IF the player brings up the clue from {suspect_name}, you break down in tears. You MUST confess, set your JSON npc_action to 'confess', and say 'I hid it by the old well!'"

    else:
        gizli_talimat = "You are a standard villager."

    # 1. UZUN SÜRELİ GEÇMİŞİ SORGULAMA (ChromaDB)
    arama_metni = f"Player action: {player_action}, Player message: {player_message}"
    eski_anilar = bellek.query(
        query_texts=[arama_metni],
        n_results=1,
        where={"npc_id": npc_id}  # ZIRH: Sadece kendi anılarını hatırlar!
    )

    hatirlanan_ani = "I have no clear memory of the past. I might be meeting this player for the first time."
    if eski_anilar['documents'] and len(eski_anilar['documents'][0]) > 0:
        hatirlanan_ani = eski_anilar['documents'][0][0]

    # 2. KISA SÜRELİ GEÇMİŞİ HAZIRLAMA (RAM)
    mevcut_sohbet_gecmisi = "\n".join(gecici_hafiza[hafiza_anahtari])
    if not mevcut_sohbet_gecmisi:
        mevcut_sohbet_gecmisi = "No recent conversation."

    print(f"[{npc_id} - Rol: {npc_role}] Yapay zeka dusunuyor (LLaMA 3.3)...")

    day_night = ""
    if time_of_day == "Night":
        day_night = "It is currently NIGHT TIME. You are tired, sleepy, and easily irritated. You find it highly suspicious that the player is walking around in the dark. Tell them to go to sleep."
    else:
        day_night = "It is currently DAY TIME. You are awake and busy."

    try:
        chat_completion = client.chat.completions.create(
            messages=[
                {
                    "role": "system",
                    "content": (
                        "You are an autonomous NPC living in a small MEDIEVAL FANTASY VILLAGE. "
                        f"{day_night}"
                        f"Your character instructions: {gizli_talimat}"
                        "React naturally and in-character. Use clear, simple English (but never reference the real modern world). "
                        # DUNYA KISITI: NPC sadece kendi cagindaki/koyundeki seyleri bilsin, modern seyleri bilmesin.
                        "WORLD RULE: You ONLY know what a simple villager of this medieval world would plausibly know "
                        "(your own trade, your neighbors, local rumors, this village and its surroundings). "
                        "You have NEVER heard of the modern world: no technology, internet, electricity, guns, cars, "
                        "countries, science, diseases like COVID, or famous people. If the player asks about anything "
                        "outside your world, react with genuine, simple confusion (e.g. 'I know not what you speak of, stranger') "
                        "and steer back to village matters. NEVER explain modern concepts and NEVER break character. "
                        "Do NOT invent facts about the theft or the village beyond your character instructions; "
                        "if you do not know something, simply say you do not know. "
                        # BÜYÜ BURADA: JSON formatına "confess" aksiyonunu öğrettik!
                        "YOU MUST RESPOND STRICTLY IN THE FOLLOWING JSON FORMAT: "
                        "{\"npc_message\": \"Your spoken response\", \"npc_emotion\": \"terrified, defeated, angry, suspicious, calm\", \"npc_action\": \"attack, retreat, confess, idle\"}"
                        " CRITICAL: The MOMENT you admit you are the thief and reveal that you hid it by the old well, "
                        "you MUST set npc_action EXACTLY to \"confess\" (not idle, not anything else). "
                        "Only use \"confess\" when you are truly admitting the crime."
                    )
                },
                {
                    "role": "user",
                    "content": (
                        f"Long-term memory:\n\"{hatirlanan_ani}\"\n\n"
                        f"Recent short-term conversation context:\n{mevcut_sohbet_gecmisi}\n\n"
                        f"Current situation:\n"
                        f"- Player's physical action: \"{player_action}\"\n"
                        f"- Player says: \"{player_message}\"\n\n"
                    )
                }
            ],
            model="llama-3.3-70b-versatile",
            response_format={"type": "json_object"},
            temperature=0.7
        )

        llm_ciktisi = json.loads(chat_completion.choices[0].message.content)
        print("LLM Yaniti:", llm_ciktisi)

        # ==========================================
        # DEĞİŞİKLİK 2: ORPHEUS TTS ENTEGRASYONU (SES ÇIKARTMA)
        # ==========================================
        npc_yanit_metni = llm_ciktisi.get("npc_message", "")
        npc_duygu = llm_ciktisi.get("npc_emotion", "calm")

        # Duyguyu Orpheus'un anladığı Vokal Etiketlere (Tags) çeviriyoruz
        vocal_direction = ""
        if npc_duygu == "angry":
            vocal_direction = "[shout] "
        elif npc_duygu == "terrified":
            vocal_direction = "[fast paced, trembling] "
        elif npc_duygu == "suspicious":
            vocal_direction = "[whisper] "
        elif npc_duygu == "defeated":
            vocal_direction = "[sad, sigh] "

        orpheus_metni = f"{vocal_direction}{npc_yanit_metni}"

        try:
            print("Orpheus seslendiriyor...")
            ses_cevabi = client.audio.speech.create(
                model="canopylabs/orpheus-v1-english",
                voice=voice_model,  # DİKKAT: "leo" yerine resmi Groq erkek sesi olan "troy" yazıyoruz. (Kadın için "autumn")
                input=orpheus_metni,
                response_format="wav"
            )

            # Orpheus'tan gelen sesi 16-bit PCM WAV'a çevir (Unity FMOD uyumlu)
            ham_bytes = ses_cevabi.read()
            with io.BytesIO(ham_bytes) as buf:
                data, samplerate = sf.read(buf, dtype='int16')

            wavfile.write(SES_DOSYA_YOLU, samplerate, data)
            print(f"[+] Ses 16-bit PCM olarak kaydedildi. Samplerate: {samplerate}")

            # Unity'e "Sesin adresi burada, gel al" diyoruz
            llm_ciktisi["audio_url"] = "http://127.0.0.1:8000/get_audio"
        except Exception as e:
            print(f"Orpheus Hatası: {e}")
            llm_ciktisi["audio_url"] = ""  # Hata olursa ses çalmaz ama oyun çökmez

        # 4. YENİ ETKİLEŞİMİ GEÇİCİ HAFIZAYA EKLEME
        npc_verdigi_cevap = llm_ciktisi.get("npc_message", "")
        yeni_etkilesim = f"Player did '{player_action}' and said '{player_message}'. I responded: '{npc_verdigi_cevap}'"
        gecici_hafiza[hafiza_anahtari].append(yeni_etkilesim)

        # 5. SINIR KONTROLÜ (10 mesaja ulaştıysa özetle)
        if len(gecici_hafiza[hafiza_anahtari]) >= MAX_MESAJ_SINIRI:
            anilari_ozetle_ve_kaydet(player_id, npc_id, gecici_hafiza[hafiza_anahtari])
            gecici_hafiza[hafiza_anahtari].clear()

        return llm_ciktisi, 200

    except Exception as e:
        print(f"KRİTİK HATA: {e}")
        return {
            "npc_message": "Give me a moment, my head hurts...",
            "npc_emotion": "fearful",
            "npc_action": "idle",
            "audio_url": ""
        }, 500


# ==========================================
# 5. API UÇ NOKTALARI (ENDPOINTS)
# ==========================================

@app.route('/ping', methods=['GET'])
def ping():
    # Unity tarafi sunucu hazir mi diye bunu yoklar. Hazirsa 200 doner.
    return jsonify({"status": "ok"}), 200


@app.route('/chat', methods=['POST'])
def metin_etkilesimi_isle():
    if not request.is_json:
        return jsonify({"error": "Gecersiz format."}), 400

    gelen_veri = request.json
    player_id = gelen_veri.get('player_id', 'default_player')
    npc_id = gelen_veri.get('npc_id', 'Default_NPC')
    player_message = gelen_veri.get('player_message', "")
    player_action = gelen_veri.get('player_action', "looking at you")
    voice_model = gelen_veri.get('voice_model', 'troy')

    npc_role = gelen_veri.get('npc_role', 'Innocent')
    suspect_name = gelen_veri.get('suspect_name', 'Unknown')
    guilty_name = gelen_veri.get('guilty_name', 'Unknown')
    time_of_day = gelen_veri.get('time_of_day', 'Day')  # Ses için request.form.get kullan

    yanit, durum_kodu = npc_beynini_calistir(player_id, npc_id, player_message, player_action, voice_model, npc_role, suspect_name, guilty_name, time_of_day)
    return jsonify(yanit), durum_kodu


@app.route('/chat/voice', methods=['POST'])
def sesli_etkilesimi_isle():
    if 'voice_file' not in request.files:
        return jsonify({"error": "Ses dosyasi bulunamadi."}), 400

    ses_dosyasi = request.files['voice_file']
    player_id = request.form.get('player_id', 'default_player')
    npc_id = request.form.get('npc_id', 'Default_NPC')  # YENİ
    player_action = request.form.get('player_action', 'looking at you')
    voice_model = request.form.get('voice_model', 'troy')

    # AGA BÜYÜ BURADA: Seste de rolleri çekiyoruz!
    npc_role = request.form.get('npc_role', 'Innocent')
    suspect_name = request.form.get('suspect_name', 'Unknown')
    guilty_name = request.form.get('guilty_name', 'Unknown')
    time_of_day = request.form.get('time_of_day', 'Day')  # Ses için request.form.get kullan

    try:
        audio_content = ses_dosyasi.read()
        transcription = client.audio.translations.create(
            file=(ses_dosyasi.filename, audio_content),
            model="whisper-large-v3",
            response_format="text",
            temperature=0.0
        )

        player_message = transcription
        if not player_message or len(player_message.strip()) < 2:
            return jsonify(
                {"npc_message": "What did you say?", "npc_emotion": "calm", "npc_action": "idle", "audio_url": ""}), 200

        yanit, durum_kodu = npc_beynini_calistir(player_id, npc_id, player_message, player_action, voice_model, npc_role, suspect_name, guilty_name,time_of_day)
        return jsonify(yanit), durum_kodu
    except Exception as e:
        print(f"SES ÇEVİRİ HATASI: {e}")
        return jsonify({"error": "Ses cozumlenemedi."}), 500


@app.route('/transcribe', methods=['POST'])
def sadece_sesi_metne_cevir():
    if 'voice_file' not in request.files:
        return jsonify({"error": "Dosya yok"}), 400

    ses_dosyasi = request.files['voice_file']
    audio_content = ses_dosyasi.read()

    try:
        transcription = client.audio.translations.create(
            file=(ses_dosyasi.filename, audio_content),
            model="whisper-large-v3",
            response_format="text",
            temperature=0.0
        )
        return jsonify({"transcribed_text": transcription}), 200
    except Exception as e:
        print(f"TRANSCRIBE HATASI: {e}")
        return jsonify({"error": "Ceviri basarisiz."}), 500


@app.route('/end_chat', methods=['POST'])
def sohbeti_bitir():
    gelen_veri = request.json
    player_id = gelen_veri.get('player_id', 'default_player')

    # Tüm hafızayı tarayıp sadece bu oyuncuya ait olanları bulup özetleyelim
    ozetlenen_sayisi = 0

    # Dictionary boyutu değişeceği için list() ile kopyasını alıp dönüyoruz
    for anahtar in list(gecici_hafiza.keys()):
        if anahtar.startswith(f"{player_id}_") and len(gecici_hafiza[anahtar]) > 0:
            # Anahtardan npc_id'yi çıkaralım. DİKKAT: player_id'nin kendisinde alt çizgi
            # olabilir (örn "default_player"), bu yüzden split("_")[1] YANLIS olur.
            # Onun yerine "player_id_" önekini kırpıyoruz: default_player_Demirci -> Demirci
            npc_id = anahtar[len(player_id) + 1:]

            anilari_ozetle_ve_kaydet(player_id, npc_id, gecici_hafiza[anahtar])
            gecici_hafiza[anahtar].clear()
            ozetlenen_sayisi += 1

    return jsonify({"status": f"Sohbet bitti. {ozetlenen_sayisi} farkli NPC ile olan sohbet ozetlendi."}), 200

# ==========================================
# DEĞİŞİKLİK 3: UNITY'NİN SESİ ÇEKECEĞİ KAPI
# ==========================================
@app.route('/get_audio', methods=['GET'])
def ses_dosyasini_gonder():
    return send_file(SES_DOSYA_YOLU, mimetype="audio/wav")

@app.route('/load_save', methods=['POST'])
def save_dosyasini_yukle():
    """Unity'den gelen Load Game isteğini karşılar ve veritabanı rotasını değiştirir."""
    gelen_veri = request.json
    save_folder = gelen_veri.get('save_folder') # Unity'den "save_01" gibi bir isim gelecek

    if not save_folder:
        return jsonify({"error": "Save klasörü belirtilmedi!"}), 400

    try:
        # 1. ChromaDB'yi yeni klasöre yönlendir
        veritabani_baglatisini_kur(save_folder)
        
        # 2. Önceki save dosyasından kalan RAM'deki (geçici) muhabbetleri temizle
        gecici_hafiza.clear()

        return jsonify({"status": "success", "message": f"Sistem {save_folder} için hazır."}), 200
        
    except Exception as e:
        print(f"Veritabanı değiştirme hatası: {e}")
        return jsonify({"error": "Veritabanı yuklenemedi."}), 500


@app.route('/reset_memory', methods=['POST'])
def hafizayi_sifirla():
    global bellek, gecici_hafiza

    try:
        # ChromaDB'deki 'anilar' koleksiyonunu tamamen sil ve sıfırdan yarat
        chroma_istemci.delete_collection("anilar")
        bellek = chroma_istemci.create_collection(name="anilar")

        # RAM'deki geçici sözlüğü de temizle
        gecici_hafiza.clear()

        print("\n[!!!] SISTEM RESTART YEDİ: Tüm NPC hafızaları başarıyla silindi. [!!!]\n")
        return jsonify({"status": "Memory wiped clean."}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/shutdown', methods=['POST'])
def sunucuyu_kapat():
    print("\n[BİLGİ] Unity'den kapatma sinyali geldi, sunucu intihar ediyor...\n")
    # Sunucuyu işletim sistemi seviyesinde acımasızca öldürür
    os._exit(0)
    return jsonify({"status": "kapanıyor"}), 200

def _parent_watchdog(parent_pid):
    """Unity (parent) sureci olunce backend KENDINI kapatir. Unity'nin kapanis kodlarina
    (OnApplicationQuit/taskkill) bagli kalmaz; crash / Alt+F4 / Gorev Yoneticisi dahil HER
    durumda calisir, boylece arkada zombi app_groq_voice.exe kalmaz."""
    import time, ctypes
    PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
    STILL_ACTIVE = 259
    kernel32 = ctypes.windll.kernel32
    while True:
        time.sleep(2)
        handle = kernel32.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, False, parent_pid)
        if not handle:
            os._exit(0)  # parent tamamen yok -> kendini kapat
        exit_code = ctypes.c_ulong()
        ok = kernel32.GetExitCodeProcess(handle, ctypes.byref(exit_code))
        kernel32.CloseHandle(handle)
        if ok and exit_code.value != STILL_ACTIVE:
            os._exit(0)  # parent cikti -> kendini kapat


if __name__ == '__main__':
    import sys, threading
    # Unity, kendi PID'ini ilk arguman olarak gecer (PythonLauncher). Varsa izlemeyi baslat.
    if len(sys.argv) > 1:
        try:
            _ppid = int(sys.argv[1])
            threading.Thread(target=_parent_watchdog, args=(_ppid,), daemon=True).start()
            print(f"[WATCHDOG] Unity PID {_ppid} izleniyor; o surec olunce backend kapanacak.")
        except ValueError:
            pass

    # debug=False: reloader'i kapatir (ikinci/zombi surec olmasin). Yayin/build icin dogru olan bu.
    app.run(debug=False, host='0.0.0.0', port=8000, use_reloader=False, threaded=True)