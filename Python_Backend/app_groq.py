import json
import uuid
import chromadb
from datetime import datetime
from flask import Flask, request, jsonify
from groq import Groq

app = Flask(__name__)

# --- YAPAY ZEKA AYARLARI (GROQ) ---
# UYARI: BURADAKİ KEY'İ SİL VE YENİSİNİ AL!
client = Groq(api_key="")

# --- HAFIZA (VERİTABANI) AYARLARI ---
chroma_istemci = chromadb.PersistentClient(path="./npc_bellek")
bellek = chroma_istemci.get_or_create_collection(name="anilar")


# ==========================================
# ANA MERKEZ: NPC MANTIĞI VE HAFIZA SİSTEMİ
# ==========================================
def npc_beynini_calistir(player_message, player_action):
    # 1. GEÇMİŞİ SORGULAMA
    arama_metni = f"Oyuncu eylemi: {player_action}, Oyuncu mesaji: {player_message}"
    eski_anilar = bellek.query(query_texts=[arama_metni], n_results=1)

    hatirlanan_ani = "I have no clear memory of the past. I might be meeting this player for the first time."

    if eski_anilar['documents'] and len(eski_anilar['documents'][0]) > 0:
        hatirlanan_ani = eski_anilar['documents'][0][0]
        ani_zamani = eski_anilar['metadatas'][0][0].get('zaman', 'Unknown time')
        print(f"[*] NPC Hatirladi ({ani_zamani}): {hatirlanan_ani}")
    else:
        print("[*] NPC'nin bu duruma benzer bir anisi yok.")

    print("Yapay zeka dusunuyor (LLaMA 3.3)...")

    try:
        # 2. YAPAY ZEKAYA PROMPT GÖNDERME
        chat_completion = client.chat.completions.create(
            messages=[
                {
                    "role": "system",
                    "content": (
                        "You are an autonomous NPC in a medieval RPG game. "
                        "Your character: A cautious, slightly paranoid medieval peasant trying to survive. "
                        "React naturally and in-character to the player's physical actions and spoken words. "
                        "YOU MUST RESPOND STRICTLY AND ONLY IN THE FOLLOWING JSON FORMAT: "
                        "{\"npc_message\": \"Your spoken response to the player in medieval English\", \"npc_emotion\": \"fearful, happy, angry, calm\", \"npc_action\": \"attack, retreat, idle\"}"
                    )
                },
                {
                    "role": "user",
                    "content": (
                        f"Current situation:\n"
                        f"- Player's physical action: \"{player_action}\"\n"
                        f"- Player says: \"{player_message}\"\n\n"
                        f"Memories of past interactions with this player:\n"
                        f"\"{hatirlanan_ani}\""
                    )
                }
            ],
            model="llama-3.3-70b-versatile",
            response_format={"type": "json_object"},
            temperature=0.7
        )

        npc_yanit_metni = chat_completion.choices[0].message.content
        llm_ciktisi = json.loads(npc_yanit_metni)
        print("LLM Yaniti:", llm_ciktisi)

        # 3. YENİ ANIYI KAYDETME
        npc_verdigi_cevap = llm_ciktisi.get("npc_message", "")
        tam_ani_metni = f"Oyuncu bana '{player_message}' dedi (Eylem: {player_action}). Ben de ona '{npc_verdigi_cevap}' diye karşılık verdim."

        gercek_zaman = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        yeni_ani_id = str(uuid.uuid4())

        bellek.add(
            documents=[tam_ani_metni],
            metadatas=[{"zaman": gercek_zaman}],
            ids=[yeni_ani_id]
        )

        return llm_ciktisi, 200

    except Exception as e:
        print(f"KRİTİK HATA: {e}")
        varsayilan_kurtarma_yaniti = {
            "npc_message": "My head hurts, pray give me a moment to think...",
            "npc_emotion": "fearful",
            "npc_action": "idle"
        }
        return varsayilan_kurtarma_yaniti, 500


# ==========================================
# ROUTE 1: KLAVYE İLE YAZI YAZINCA BURASI ÇALIŞIR
# ==========================================
@app.route('/chat', methods=['POST'])
def metin_etkilesimi_isle():
    if not request.is_json:
        return jsonify({"error": "Gecersiz format. Lutfen JSON gonderin."}), 400

    gelen_veri = request.json
    player_message = gelen_veri.get('player_message', "")
    player_action = gelen_veri.get('player_action', "")

    yanit, durum_kodu = npc_beynini_calistir(player_message, player_action)
    return jsonify(yanit), durum_kodu


# ==========================================
# ROUTE 2: MİKROFON İLE KONUŞUNCA BURASI ÇALIŞIR (YENİ)
# ==========================================
@app.route('/chat/voice', methods=['POST'])
def sesli_etkilesimi_isle():
    # Unity'den gelen dosyanın varlığını kontrol et
    if 'voice_file' not in request.files:
        return jsonify({"error": "Ses dosyasi bulunamadi (voice_file eksik)."}), 400

    ses_dosyasi = request.files['voice_file']

    # İsteğe bağlı olarak Unity'den anlık aksiyon da (örn: 'sword_drawn') gelebilir
    player_action = request.form.get('player_action', 'talking to you')

    print(f"[*] Unity'den ses dosyası geldi, Groq Whisper ile çözülüyor...")

    try:
        # Sesi RAM üzerinden (kaydetmeden) direkt Groq'a fırlatıyoruz (Işık hızı için)
        audio_content = ses_dosyasi.read()
        transcription = client.audio.translations.create(
            file=(ses_dosyasi.filename, audio_content),
            model="whisper-large-v3",
            response_format="text",
            ##language="en"  # İngilizce olduğunu belirttik
        )

        player_message = transcription
        print(f"[*] Oyuncunun Söylediği (Whisper): {player_message}")

        # Eğer ses boşsa veya anlaşılamadıysa
        if not player_message or len(player_message.strip()) < 2:
            return jsonify({
                "npc_message": "What did you say? Speak up, my ears deceive me.",
                "npc_emotion": "confused",
                "npc_action": "idle"
            }), 200

        # Metni aldık, şimdi asıl NPC beynine yolluyoruz
        yanit, durum_kodu = npc_beynini_calistir(player_message, player_action)
        return jsonify(yanit), durum_kodu

    except Exception as e:
        print(f"SES ÇEVİRİ HATASI: {e}")
        return jsonify({"error": "Ses cozumlenemedi."}), 500


if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=8000)