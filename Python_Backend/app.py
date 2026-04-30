import json
import uuid
import chromadb
from datetime import datetime  # ZAMAN İÇİN BUNU EKLEDİK
import google.generativeai as genai
from flask import Flask, request, jsonify
from groq import Groq

app = Flask(__name__)

# --- YAPAY ZEKA AYARLARI ---
client = Groq(api_key="")

# Yeni gemini-3-flash-preview modelini kullanmanız harika, JSON konusunda çok daha zekidir!
model = genai.GenerativeModel(
    'gemini-3-flash-preview',
    generation_config={"response_mime_type": "application/json"}
)

# --- HAFIZA (VERİTABANI) AYARLARI ---
chroma_istemci = chromadb.PersistentClient(path="./npc_bellek")
bellek = chroma_istemci.get_or_create_collection(name="anilar")


@app.route('/chat', methods=['POST'])
def etkilesimi_isle():
    if not request.is_json:
        return jsonify({"error": "Gecersiz format. Lutfen JSON gonderin."}), 400

    gelen_veri = request.json

    player_message = gelen_veri.get('player_message') or ""
    player_action = gelen_veri.get('player_action') or ""

    # 1. GEÇMİŞİ SORGULAMA (Arama yaparken sadece oyuncunun o anki durumuna bakıyoruz)
    arama_metni = f"Oyuncu eylemi: {player_action}, Oyuncu mesaji: {player_message}"

    eski_anilar = bellek.query(
        query_texts=[arama_metni],
        n_results=1
    )

    hatirlanan_ani = "Gecmise dair net bir anim yok, bu oyuncuyla ilk defa karsilasiyor olabilirim."

    if eski_anilar['documents'] and len(eski_anilar['documents'][0]) > 0:
        hatirlanan_ani = eski_anilar['documents'][0][0]
        # Hangi tarihteki anıyı hatırladığını da konsola yazdıralım
        ani_zamani = eski_anilar['metadatas'][0][0].get('zaman', 'Bilinmeyen zaman')
        print(f"[*] NPC Hatirladi ({ani_zamani}): {hatirlanan_ani}")
    else:
        print("[*] NPC'nin bu duruma benzer bir anisi yok.")

    # 2. YAPAY ZEKAYA PROMPT GÖNDERME
    prompt = f"""
        Sen bir oyun içinde otonom bir NPC'sin. Karakterin: Orta çağda yaşayan temkinli bir köylü.

        Şu anki durum:
        - Oyuncunun fiziksel eylemi: "{player_action}"
        - Oyuncunun sana söylediği: "{player_message}"

        Geçmişten hatırladıkların (Daha önceki sohbetleriniz): 
        "{hatirlanan_ani}"

        Buna göre anlık bir tepki ver.
        YANITINI SADECE AŞAĞIDAKİ JSON FORMATINDA VER:
        {{
            "npc_message": "Oyuncuya soyleyecegin cumle",
            "npc_emotion": "fearful, happy, angry, calm",
            "npc_action": "attack, retreat, idle"
        }}
        """
    print("Yapay zeka dusunuyor...")

    try:
        response = model.generate_content(prompt)
        llm_ciktisi = json.loads(response.text)
        print("LLM Yaniti:", llm_ciktisi)

        # ---------------------------------------------------------
        # 3. YENİ ANIYI "CEVABIYLA BİRLİKTE" KAYDETME (DÜZELTİLEN YER)
        # ---------------------------------------------------------
        npc_verdigi_cevap = llm_ciktisi.get("npc_message", "")
        # Artık anının içinde NPC'nin kendi cevabı da var!
        tam_ani_metni = f"Oyuncu bana '{player_message}' dedi (Eylem: {player_action}). Ben de ona '{npc_verdigi_cevap}' diye karşılık verdim."

        gercek_zaman = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        yeni_ani_id = str(uuid.uuid4())

        bellek.add(
            documents=[tam_ani_metni],
            metadatas=[{"zaman": gercek_zaman}],
            ids=[yeni_ani_id]
        )

        return jsonify(llm_ciktisi)

    except Exception as e:
        print(f"KRİTİK HATA: {e}")

        varsayilan_kurtarma_yaniti = {
            "npc_message": "Su an kafam cok karisik, bana biraz zaman ver.",
            "npc_emotion": "fearful",
            "npc_action": "idle"
        }
        return jsonify(varsayilan_kurtarma_yaniti), 500


if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=8000)