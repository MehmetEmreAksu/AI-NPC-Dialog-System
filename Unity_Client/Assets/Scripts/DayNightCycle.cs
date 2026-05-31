using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Zaman Ayarlari")]
    public float dayDurationInSeconds = 120f;
    public string currentTimePhase = "Day";

    [Header("Gokyuzu Isiklari")]
    public Light sunLight;  // Gunes (Directional)
    public Light moonLight; // Ay (Directional)

    public float maxSunIntensity = 1f;
    public float maxMoonIntensity = 0.5f; // Ay cok parlamasin, loş lacivert versin

    [Header("Ortam (Ambient) Isigi")]
    // Gece simsiyah olmasin diye sahneye taban aydinlik veriyoruz.
    public Color dayAmbientColor = new Color(0.55f, 0.55f, 0.55f);   // gunduz gri-beyaz
    public Color nightAmbientColor = new Color(0.06f, 0.07f, 0.12f); // gece loş lacivert

    [Header("Gokyuzu (Skybox) Karartma")]
    // Skybox = arkadaki gokyuzunun kendisi. Gece kararmasi icin parlakligini suruyoruz.
    public bool controlSkybox = true;
    public float dayExposure = 1.3f;    // gunduz gokyuzu parlakligi
    public float nightExposure = 0.1f;  // gece gokyuzu parlakligi (0'a yaklasinca kapkara)

    private float rotationSpeed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        rotationSpeed = 360f / dayDurationInSeconds;

        // Inspector atamalarini kontrol et, eksikse net uyari ver (sessizce karartma).
        if (sunLight == null)
            Debug.LogError("[DayNightCycle] sunLight atanmamis! Inspector'dan Gunes Light'i surukle.");
        if (moonLight == null)
            Debug.LogError("[DayNightCycle] moonLight atanmamis! Inspector'dan Ay Light'i surukle.");

        // Ambient'i biz sureceğimiz icin Flat moda alalim ki Skybox ayarindan bagimsiz calissin.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    private void Update()
    {
        // 1. Gunesi DUNYA X ekseninde dondur (Space.World => baslangic yaw'i ne olursa olsun
        //    gunes ufkun altina tam iner, gece garanti tetiklenir).
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime, Space.World);

        if (sunLight == null) return; // sun yoksa faz hesabi yapamayiz

        // 2. FAZ TESPITI: euler okumak yerine gunesin GERCEK yonune bakiyoruz.
        // Directional light asagi bakiyorsa (forward.y < 0) gunes ufkun ustunde => GUNDUZ.
        // forward.y'yi 0..1 araliginda "gunduzluk faktoru"ne ceviriyoruz.
        float dayFactor = Mathf.Clamp01(-sunLight.transform.forward.y); // 1 = tam ogle, 0 = ufuk/gece
        float nightFactor = 1f - dayFactor;

        currentTimePhase = (dayFactor > 0f) ? "Day" : "Night";

        // 3. AY'I da dondur: surekli gunesin 180 derece tersinde dursun.
        //    Boylece gunes batarken ay doğar, gunes doğarken ay batar.
        if (moonLight != null)
            moonLight.transform.rotation = sunLight.transform.rotation * Quaternion.Euler(180f, 0f, 0f);

        // 4. Isik yogunluklari yonle orantili => dogal gun dogumu/batimi gecisi
        sunLight.intensity = dayFactor * maxSunIntensity;
        if (moonLight != null)
            moonLight.intensity = nightFactor * maxMoonIntensity;

        // 5. Ambient tabani: gece bile sahne(objeler) simsiyah olmasin
        RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, dayFactor);

        // 6. Skybox (arkadaki gokyuzu) gece kararsin: exposure'i dusur
        if (controlSkybox && RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
            RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(nightExposure, dayExposure, dayFactor));
    }
}
