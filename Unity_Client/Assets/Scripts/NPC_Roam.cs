using UnityEngine;
using UnityEngine.AI;

public class NPC_Roam : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public float roamRadius = 10f;
    public float waitTime = 3f;

    private float timer;
    private Vector3 startPosition;

    [Header("Voice Settings (Phrase Based)")]
    public AudioClip[] myVoiceClips; // Adamýn 1-2 saniyelik cümle/mýrýldanma kayýtlarý
    [Range(0.5f, 1.5f)] public float myVoicePitch = 0.8f;

    // ÝÞTE BÝZÝM FREN ÞALTERÝMÝZ BU
    private bool isTalkingMode = false;

    [Header("NPC Identity")]
    public string npcID = "Demirci";
    public string voiceModel = "troy";

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        timer = waitTime;
        startPosition = transform.position;
    }

    void Update()
    {
        // AGA KÝLÝT NOKTA BURASI!
        // Eðer konuþma modundaysak, alttaki yürüme kodlarýný HÝÇ OKUMA (Direkt çýk)
        if (isTalkingMode == true)
        {
            return;
        }

        // --- NORMAL VOLTA ATMA KODLARI (Sadece konuþmuyorsak çalýþýr) ---
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            timer += Time.deltaTime;
            animator.SetBool("isWalking", false);

            if (timer >= waitTime)
            {
                SetNewRandomDestination();
                timer = 0;
            }
        }
        else
        {
            animator.SetBool("isWalking", true);
        }
    }

    void SetNewRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection += startPosition;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, roamRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
    }

    // =================================================================
    // DÝALOG BAÞLADIÐINDA VE BÝTTÝÐÝNDE ÇALIÞACAK SÝHÝRLÝ FONKSÝYONLAR
    // =================================================================


    // 1. E'ye basýldýðýnda adamý dondurup sana baktýracak fonksiyon
    public void PrepareForDialog()
    {
        isTalkingMode = true;       // Volta atmayý kes
        agent.isStopped = true;     // Yere çivilen
        agent.velocity = Vector3.zero;

        animator.SetBool("isWalking", false);
        animator.SetBool("isTalking", false); // Sadece IDLE durup beklesin
    }

    // 2. Python'dan cevap geldiðinde (veya bittiðinde) elleri oynatacak fonksiyon
    public void SetTalkingAnimation(bool isTalkingMode, string emotion)
    {
        if (isTalkingMode)
        {
            // ZIRH 1: Önceki sohbetten kalan trigger'larý temizle
            animator.ResetTrigger("angry");
            animator.ResetTrigger("suspicious");
            animator.ResetTrigger("defeated");
            animator.ResetTrigger("terrified");

            // BÜYÜ BURADA: Eðer özel bir duygu varsa normal isTalking'i EZÝYORUZ!
            switch (emotion)
            {
                case "angry":
                    animator.SetBool("isTalking", false); // Normal konuþmayý kapa!
                    animator.SetTrigger("angry");         // Duyguyu patlat!
                    break;
                case "suspicious":
                    animator.SetBool("isTalking", false);
                    animator.SetTrigger("suspicious");
                    break;
                case "defeated":
                    animator.SetBool("isTalking", false);
                    animator.SetTrigger("defeated");
                    break;
                case "terrified":
                    animator.SetBool("isTalking", false);
                    animator.SetTrigger("terrified");
                    break;
                case "calm":
                default:
                    // SADECE "calm" geldiðinde normal konuþma þalterini kaldýr.
                    animator.SetBool("isTalking", true);
                    break;
            }
        }
        else
        {
            // Sohbet tamamen bitti (ESC'ye basýldý)
            animator.SetBool("isTalking", false);

            // ZIRH 2: Adamýn korkusu veya siniri yüzünde takýlý kalmasýn diye ZORLA Idle'a döndür.
            animator.Play("Idle"); // Kendi idle animasyonunun adýný buraya yaz (Örn: "Idle_A")
        }
    }

    // 3. Sohbet tamamen bitip ESC'ye basýldýðýnda çalýþacak fonksiyon
    public void EndDialog()
    {
        isTalkingMode = false;      // Volta atmaya geri dön
        animator.SetBool("isTalking", false);
        agent.isStopped = false;    // Tasmasýný çöz
        timer = waitTime;
    }
}

