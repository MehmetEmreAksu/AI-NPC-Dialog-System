import json
import uuid
import io
import numpy as np
import soundfile as sf
import scipy.io.wavfile as wavfile
import chromadb
from datetime import datetime
from flask import Flask, request, jsonify, send_file
from groq import Groq

app = Flask(__name__)

# ==========================================
# 1. YAPAY ZEKA VE API KONFİGÜRASYONU
# ==========================================
# UYARI: AGA ŞU KEY'İ GİT YENİLE, BURADA KABAK GİBİ DURMASIN!


################################## API KEY #####################################
################################## API KEY #####################################
################################## API KEY #####################################
client = Groq(api_key="Your API KEY")
################################## API KEY #####################################
################################## API KEY #####################################
################################## API KEY #####################################

# ==========================================
# 2. HAFIZA YÖNETİMİ AYARLARI
# ==========================================
gecici_hafiza = {}
MAX_MESAJ_SINIRI = 10  # 10 mesajda bir özetleyip kalıcı hafızaya atar

chroma_istemci = chromadb.PersistentClient(path="./npc_bellek")
bellek = chroma_istemci.get_or_create_collection(name="anilar")


# ==========================================
# 3. ÖZETLEME FONKSİYONU
# ==========================================
def anilari_ozetle_ve_kaydet(player_id, gecmis_liste):
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
            metadatas=[{"zaman": gercek_zaman}],
            ids=[yeni_ani_id]
        )
        print(f"[+] Özet başarıyla ChromaDB'ye kaydedildi: {ozet_ani}\n")

    except Exception as e:
        print(f"HATA - Özetleme işlemi başarısız: {e}")


# ==========================================
# 4. ANA MERKEZ: NPC MANTIĞI (Birleştirilmiş)
# ==========================================
def npc_beynini_calistir(player_id, player_message, player_action):
    # Oyuncu için geçici hafıza yoksa oluştur
    if player_id not in gecici_hafiza:
        gecici_hafiza[player_id] = []

    # 1. UZUN SÜRELİ GEÇMİŞİ SORGULAMA (ChromaDB)
    arama_metni = f"Player action: {player_action}, Player message: {player_message}"
    eski_anilar = bellek.query(query_texts=[arama_metni], n_results=1)

    hatirlanan_ani = "I have no clear memory of the past. I might be meeting this player for the first time."
    if eski_anilar['documents'] and len(eski_anilar['documents'][0]) > 0:
        hatirlanan_ani = eski_anilar['documents'][0][0]

    # 2. KISA SÜRELİ GEÇMİŞİ HAZIRLAMA (RAM)
    mevcut_sohbet_gecmisi = "\n".join(gecici_hafiza[player_id])
    if not mevcut_sohbet_gecmisi:
        mevcut_sohbet_gecmisi = "No recent conversation."

    print("Yapay zeka dusunuyor (LLaMA 3.3)...")

    try:
        # 3. YAPAY ZEKAYA PROMPT GÖNDERME
        chat_completion = client.chat.completions.create(
            messages=[
                {
                    "role": "system",
                    "content": (
                        "You are an autonomous NPC in a modern RPG game. "
                        "Your character: A cautious and pragmatic person trying to survive. "
                        "React naturally and in-character. "
                        "SPEAK IN PLAIN, MODERN ENGLISH. Do not use medieval or old-fashioned words (no 'thy', 'thou', 'hath'). "
                        "YOU MUST RESPOND STRICTLY IN THE FOLLOWING JSON FORMAT: "
                        "{\"npc_message\": \"Your spoken response to the player\", \"npc_emotion\": \"terrified, defeated, angry, suspicious, calm\", \"npc_action\": \"attack, retreat, idle\"}"
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
                voice="troy",  # DİKKAT: "leo" yerine resmi Groq erkek sesi olan "troy" yazıyoruz. (Kadın için "autumn")
                input=orpheus_metni,
                response_format="wav"
            )

            # Orpheus'tan gelen sesi 16-bit PCM WAV'a çevir (Unity FMOD uyumlu)
            ham_bytes = ses_cevabi.read()
            with io.BytesIO(ham_bytes) as buf:
                data, samplerate = sf.read(buf, dtype='int16')

            wavfile.write("current_voice.wav", samplerate, data)
            print(f"[+] Ses 16-bit PCM olarak kaydedildi. Samplerate: {samplerate}")

            # Unity'e "Sesin adresi burada, gel al" diyoruz
            llm_ciktisi["audio_url"] = "http://127.0.0.1:8000/get_audio"
        except Exception as e:
            print(f"Orpheus Hatası: {e}")
            llm_ciktisi["audio_url"] = ""  # Hata olursa ses çalmaz ama oyun çökmez

        # 4. YENİ ETKİLEŞİMİ GEÇİCİ HAFIZAYA EKLEME
        npc_verdigi_cevap = llm_ciktisi.get("npc_message", "")
        yeni_etkilesim = f"Player did '{player_action}' and said '{player_message}'. I responded: '{npc_verdigi_cevap}'"
        gecici_hafiza[player_id].append(yeni_etkilesim)

        # 5. SINIR KONTROLÜ (10 mesaja ulaştıysa özetle)
        if len(gecici_hafiza[player_id]) >= MAX_MESAJ_SINIRI:
            anilari_ozetle_ve_kaydet(player_id, gecici_hafiza[player_id])
            gecici_hafiza[player_id].clear()

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

@app.route('/chat', methods=['POST'])
def metin_etkilesimi_isle():
    if not request.is_json:
        return jsonify({"error": "Gecersiz format."}), 400

    gelen_veri = request.json
    player_id = gelen_veri.get('player_id', 'default_player')
    player_message = gelen_veri.get('player_message', "")
    player_action = gelen_veri.get('player_action', "")

    yanit, durum_kodu = npc_beynini_calistir(player_id, player_message, player_action)
    return jsonify(yanit), durum_kodu


@app.route('/chat/voice', methods=['POST'])
def sesli_etkilesimi_isle():
    if 'voice_file' not in request.files:
        return jsonify({"error": "Ses dosyasi bulunamadi."}), 400

    ses_dosyasi = request.files['voice_file']
    player_id = request.form.get('player_id', 'default_player')
    player_action = request.form.get('player_action', 'talking to you')

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

        yanit, durum_kodu = npc_beynini_calistir(player_id, player_message, player_action)
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

    if player_id in gecici_hafiza and len(gecici_hafiza[player_id]) > 0:
        anilari_ozetle_ve_kaydet(player_id, gecici_hafiza[player_id])
        gecici_hafiza[player_id].clear()
        return jsonify({"status": "Sohbet ozetlendi."}), 200

    return jsonify({"status": "Ozetlenecek sohbet yok."}), 200


# ==========================================
# DEĞİŞİKLİK 3: UNITY'NİN SESİ ÇEKECEĞİ KAPI
# ==========================================
@app.route('/get_audio', methods=['GET'])
def ses_dosyasini_gonder():
    return send_file("current_voice.wav", mimetype="audio/wav")


if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=8000)