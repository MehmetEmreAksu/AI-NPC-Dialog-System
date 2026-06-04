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
    public AudioClip[] myVoiceClips; // Adam�n 1-2 saniyelik c�mle/m�r�ldanma kay�tlar�
    [Range(0.5f, 1.5f)] public float myVoicePitch = 0.8f;

    // ��TE B�Z�M FREN �ALTER�M�Z BU
    private bool isTalkingMode = false;

    [Header("NPC Identity")]
    public string npcID = "Demirci";
    public string voiceModel = "troy";

    [Header("Chase Settings (Kovalama)")]
    private bool isChasing = false;
    private Transform targetPlayer;
    private string stolenItemName; // Ne �al�nd���n� akl�nda tutsun ki laf edebilsin

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        timer = waitTime;
        startPosition = transform.position;

        // TESHIS: Bu NPC baked NavMesh (mavi alan) uzerinde mi? Degilse hic yuruyemez/kovalayamaz.
        // Console'da hangi NPC'nin sorunlu oldugunu net soyler (orn: Demirci, yoldaki kadin...).
        if (agent != null && !agent.isOnNavMesh)
            Debug.LogWarning($"[NavMesh] '{npcID}' NavMesh uzerinde DEGIL! Bu yuzden takiliyor. " +
                             "Onu mavi (baked) alanin uzerine tasi ve NavMeshSurface'i yeniden bake et.");
    }

    void Update()
    {
        // AGA K�L�T NOKTA BURASI!
        // E�er konu�ma modundaysak, alttaki y�r�me kodlar�n� H�� OKUMA (Direkt ��k)
        if (isTalkingMode == true)
        {
            return;
        }

        // --- KOVALAMA MODU ---
        if (isChasing && targetPlayer != null)
        {
            // Adam k�r gibi de�il, NavMesh �zerinde gidebildi�i en yak�n noktaya ko�sun
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(targetPlayer.position, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.SetDestination(targetPlayer.position);
            }

            // Yakalama mesafesini 2.5'ten 3.5'e ��kard�k. 
            // B�ylece sen ufak bir ta�a ��ksan bile NPC dibine gelince seni ensenden yakalayabilir!
            if (Vector3.Distance(transform.position, targetPlayer.position) <= 2.5f)
            {
                isChasing = false;
                agent.speed = 3.5f;
                PrepareForDialog();

                if (DialogSystem.Instance != null)
                {
                    // 1. Aray�z� a� ve karakteri dondur
                    DialogSystem.Instance.StartChat(this);

                    // 2. An�nda d�nmek yerine Smooth kameray� tetikle!
                    StartCoroutine(CatchAndTurnSmoothly());
                }
            }
            return;
        }

        // --- NORMAL VOLTA ATMA KODLARI (Sadece konu�muyorsak �al���r) ---
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
    // D�ALOG BA�LADI�INDA VE B�TT���NDE �ALI�ACAK S�H�RL� FONKS�YONLAR
    // =================================================================


    // 1. E'ye bas�ld���nda adam� dondurup sana bakt�racak fonksiyon
    public void PrepareForDialog()
    {
        isTalkingMode = true;       // Volta atmay� kes
        agent.isStopped = true;     // Yere �ivilen
        agent.velocity = Vector3.zero;

        animator.SetBool("isWalking", false);
        animator.SetBool("isTalking", false); // Sadece IDLE durup beklesin
    }

    // 2. Python'dan cevap geldi�inde (veya bitti�inde) elleri oynatacak fonksiyon
    public void SetTalkingAnimation(bool isTalkingMode, string emotion)
    {
        if (isTalkingMode)
        {
            // ZIRH 1: �nceki sohbetten kalan trigger'lar� temizle
            animator.ResetTrigger("angry");
            animator.ResetTrigger("suspicious");
            animator.ResetTrigger("defeated");
            animator.ResetTrigger("terrified");

            // B�Y� BURADA: E�er �zel bir duygu varsa normal isTalking'i EZ�YORUZ!
            switch (emotion)
            {
                case "angry":
                    animator.SetBool("isTalking", false); // Normal konu�may� kapa!
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
                    // SADECE "calm" geldi�inde normal konu�ma �alterini kald�r.
                    animator.SetBool("isTalking", true);
                    break;
            }
        }
        else
        {
            // Sohbet tamamen bitti (ESC'ye bas�ld�)
            animator.SetBool("isTalking", false);

            // ZIRH 2: Adam�n korkusu veya siniri y�z�nde tak�l� kalmas�n diye ZORLA Idle'a d�nd�r.
            animator.Play("Idle"); // Kendi idle animasyonunun ad�n� buraya yaz (�rn: "Idle_A")
        }
    }

    // 3. Sohbet tamamen bitip ESC'ye bas�ld���nda �al��acak fonksiyon
    public void EndDialog()
    {
        isTalkingMode = false;      // Volta atmaya geri d�n
        animator.SetBool("isTalking", false);
        agent.isStopped = false;    // Tasmas�n� ��z
        timer = waitTime;
    }

    public void StartChasingPlayer(string item, Transform playerTransform)
    {
        isTalkingMode = false; // Konu�ma modundaysa ��k
        isChasing = true;      // Avc� modunu a�!
        stolenItemName = item;
        targetPlayer = playerTransform;

        agent.isStopped = false;
        agent.speed = 5f; // AGA ADAM S�N�RL�, HIZLI KO�SUN! (Normal h�z� 3 ise bunu 5 yap)
        animator.SetBool("isWalking", true);
    }

    private System.Collections.IEnumerator CatchAndTurnSmoothly()
    {
        float time = 0f;
        float duration = 0.4f; // Saniyenin yar�s�nda kaymak gibi d�necek

        // Birbirinize bakaca��n�z y�nleri hesapla
        Vector3 playerToNpc = (transform.position - targetPlayer.position).normalized;
        Vector3 npcToPlayer = (targetPlayer.position - transform.position).normalized;

        playerToNpc.y = 0f; // Kafalar havaya kalkmas�n
        npcToPlayer.y = 0f;

        Quaternion playerTargetRot = Quaternion.LookRotation(playerToNpc);
        Quaternion npcTargetRot = Quaternion.LookRotation(npcToPlayer);

        // Smooth d�n�� d�ng�s�
        while (time < duration)
        {
            targetPlayer.rotation = Quaternion.Slerp(targetPlayer.rotation, playerTargetRot, time / duration);
            transform.rotation = Quaternion.Slerp(transform.rotation, npcTargetRot, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        // Tam hizala
        targetPlayer.rotation = playerTargetRot;
        transform.rotation = npcTargetRot;

        // D�N�� B�TT�KTEN SONRA Python'a "Beni yakalad�" kargosunu yolla!
        if (NetworkManager.Instance != null)
        {
            string thePrompt = $"I just STOLE the {stolenItemName} right in front of your eyes!";
            NetworkManager.Instance.SendMessageToServer("", npcID, thePrompt, voiceModel);
        }
    }
}

