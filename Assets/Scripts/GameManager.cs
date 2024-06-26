using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class GameManager : MonoBehaviour
{
    public enum DeathReason
    {
        Generic,
        TooManyQuacks,
        TooLongJackBox,
        WrongFurnaceItem,
        SixthRevolverChamber,
        WrongPhonePickup,
    }


    public int coins;
    [SerializeField] public Canvas playerCanvas;

    [SerializeField] public bool startGunSequence;
    [SerializeField] AudioClip bellClip;
    [SerializeField] AudioClip errorClip;
    [SerializeField] AudioClip succesClip;
    [SerializeField] AudioClip scaryMusicClip;
    [SerializeField] AudioClip spawnItemClip;
    [SerializeField] public float secondsInHalfDay = 100f;
    [SerializeField] public Transform generalItemSpawnPoint;
    [SerializeField] public GameObject generalItemPrefab;
    [SerializeField] public Transform ritualItemSpawnPoint;
    [SerializeField] public GameObject ritualItemPrefab;
    [SerializeField] public GameObject surpriseDecal;
    [SerializeField] public GameObject spawnVFX;
    [SerializeField] private Vignette vignette;


    [SerializeField] public GameObject gunPrefab;
    [SerializeField] public Transform gunSpawnPoint;

    public HoveringObject draggedObject;

    public DeathReason currentDeathReason;
    public bool isDead;
    public bool isBleeding;
    public bool isInGunSequence;
    public bool isTurnedAround;
    public float gunSequenceInBetweenDelay;

    public float timer;

    private bool startedGunSequence;
    private bool revealedDecal;
    private AudioSource source;
    private Vector3 ritualStartPosition;
    private AudioSource[] allAudioSources;

    public static GameManager Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        if (vignette == null)
        {
            vignette = FindObjectOfType<Vignette>();
        }
    }
    private void Start()
    {
        surpriseDecal.SetActive(false);
        source = GetComponent<AudioSource>();
        ritualStartPosition = ritualItemPrefab.transform.position;
        ritualItemPrefab.transform.position = Vector3.zero;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (startGunSequence)
        {
            startGunSequence = false;
            Instance.StartGunSequence(true, 0f, DeathReason.Generic);
        }

        if (isTurnedAround && !revealedDecal)
        {
            revealedDecal = true;
            StartCoroutine(RevealDecalSequence());
        }
    }

    public void StopAllAudio()
    {
        allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            source.Stop();
        }
    }

    public void SetVignetteRoundness(float roundness)
    {
        vignette.roundness = roundness;
    }

    private IEnumerator RevealDecalSequence()
    {
        yield return new WaitForSeconds(0.3f);
        if (!isTurnedAround)
        {
            revealedDecal = false;
            yield break;
        }
        
        while(isTurnedAround)
        {
            yield return null;
        }
        yield return new WaitForSeconds(0.35f);
        PlayBellSound();
        surpriseDecal.SetActive(true);
        StartCoroutine(PulseDecalEmission(surpriseDecal));
    }

    private IEnumerator PulseDecalEmission(GameObject decal)
    {
        Renderer renderer = decal.GetComponent<Renderer>();

        if (renderer != null && renderer.materials.Length > 0)
        {
            Material material = renderer.materials[0]; // Assuming only one material is used
            Color baseColor = material.GetColor("_EmissionColor");
            float minEmission = 0f;
            float maxEmission = 5f;
            float duration = 0.5f; // duration of pulsating effect

            //set emission to 0
            //material.SetColor("_EmissionColor", baseColor * Mathf.LinearToGammaSpace(0));
            yield return new WaitForSeconds(0.2f);

            float t = 0f;
            while (t < duration)
            {
                float emission = Mathf.Lerp(minEmission, maxEmission, Mathf.PingPong(t / duration, 1));
                Color finalColor = baseColor * Mathf.LinearToGammaSpace(emission);
                material.SetColor("_EmissionColor", finalColor);

                t += Time.deltaTime;
                yield return null;
            }

            // Smoothly transition back to the original emission color
            t = 0f;
            while (t < duration * 3)
            {
                float emission = Mathf.Lerp(maxEmission, minEmission, Mathf.PingPong(t / (duration * 3), 1));
                Color finalColor = baseColor * Mathf.LinearToGammaSpace(emission);
                material.SetColor("_EmissionColor", finalColor);

                t += Time.deltaTime;
                yield return null;
            }

            // Reset emission color after pulsating
            //material.SetColor("_EmissionColor", baseColor);
        }
        else
        {
            Debug.LogWarning("Renderer or materials not found on the provided decal GameObject.");
        }
    }

    public void AddCoins(int amount)
    {
        coins += amount;
    }

    public void PlayBellSound()
    {
        source.PlayOneShot(bellClip);
    }

    public void PlayErrorSound()
    {
        source.PlayOneShot(errorClip);
    }

    public void PlaySuccesSound()
    {
        source.PlayOneShot(succesClip, 0.4f);
    }

    public void PlaySpawnSound()
    {
        source.PlayOneShot(spawnItemClip, 1f);
    }

    public void PlayScaryMusic()
    {
        source.clip = scaryMusicClip;
        source.Play();
    }

    public void StopScaryMusic()
    {
        source.clip = null;
        source.Stop();
    }

    public void SpawnGeneralItem()
    {
        StartCoroutine(SpawnItemSequence(generalItemPrefab, generalItemSpawnPoint));
    }

    public IEnumerator SpawnItemSequence(GameObject spawnedItem, Transform point)
    {
        GameObject fx = Instantiate(spawnVFX, point);
        fx.GetComponent<DestroyAfterSeconds>().enabled = true;
        PlaySpawnSound();
        yield return new WaitForSeconds(1f);
        GameObject item = Instantiate(spawnedItem, point);
        item.transform.parent = null;
    }

    public void SpawnRitualItem()
    {
        StartCoroutine(SpawnItemSequence(ritualItemPrefab, ritualItemSpawnPoint));
        RingPhoneForSeconds(10f, 10f);
        TarotManager.Instance.CompleteRitualTarot(1.5f);
    }

    public void ToggleUIVisibility(bool visible)
    {
        playerCanvas.enabled = visible;
    }

    public float GetMinutesPassed()
    {
        return (timer / secondsInHalfDay) * 720f; // 12 hours * 60 minutes
    }

    /// <summary>
    /// gets the minute counter of the digital time. <br></br> example: <br></br> At 6:45, returns 45
    /// </summary>
    /// <returns></returns>
    public float GetMinuteTime()
    {
        float minutesPassed = GetMinutesPassed();
        return minutesPassed % 60f;
    }

    public void StartGunSequence(bool loaded, float delay, DeathReason deathReason)
    {
        currentDeathReason = deathReason;
        if (isInGunSequence) { return; }
        isInGunSequence = true;
        StartCoroutine(GunSequence(loaded, delay));
    }

    public void RingPhoneForSeconds(float seconds, float startDelay)
    {
        StartCoroutine(RingSequence(seconds, startDelay));
    }

    private IEnumerator RingSequence(float seconds, float startDelay)
    {
        yield return new WaitForSeconds(startDelay);
        PhoneScript phoneScript = FindObjectOfType<PhoneScript>();
        phoneScript.StartRing();
        yield return new WaitForSeconds(seconds);
        phoneScript.StopRing();
    }

    private IEnumerator GunSequence(bool loaded, float delay)
    {
        yield return new WaitForSeconds(delay);
        LookAroundCamera lookAroundCamera = FindObjectOfType<LookAroundCamera>();
        lookAroundCamera.MoveToDefaultPosition();

        yield return new WaitForSeconds(gunSequenceInBetweenDelay);
        ItemManager.Instance.DropHeldItem();
        yield return new WaitForSeconds(gunSequenceInBetweenDelay);

        PlayBellSound();
        GameObject gun = Instantiate(gunPrefab, gunSpawnPoint);
        gun.GetComponentInChildren<RevolverScript>().isLoaded = loaded;

    }

    public string GetTimeString()
    {
        float minutesPassed = GetMinutesPassed();
        // Calculate the hour and minute parts of the time
        int hours = Mathf.FloorToInt(minutesPassed / 60f) % 12; // Hours in 12-hour format
        int minutes = Mathf.FloorToInt(minutesPassed % 60f); // Minutes

        // Convert 0 hours (midnight) to 12
        if (hours == 0)
        {
            hours = 12;
        }

        // Get word representation for the hour
        string hourWord = ConvertNumberToWord(hours);
        string minuteWord = ConvertNumberToWord(minutes);

        // Construct the time string according to English norms
        string timeString = "";

        // Check if it's exactly on the hour
        if (minutes == 0)
        {
            timeString = $"{hourWord} o'clock";
        }
        else if (minutes == 15)
        {
            timeString = $"quarter past {hourWord}";
        }
        else if (minutes == 30)
        {
            timeString = $"half past {hourWord}";
        }
        else if (minutes == 45)
        {
            // Get word representation for the next hour
            string nextHourWord = ConvertNumberToWord((hours % 12) + 1);
            timeString = $"quarter to {nextHourWord}";
        }
        else if (minutes < 30)
        {
            timeString = $"{minuteWord} past {hourWord}";
        }
        else // minutes > 30
        {
            // Get word representation for the next hour
            string nextHourWord = ConvertNumberToWord((hours % 12) + 1);
            timeString = $"{ConvertNumberToWord(60 - minutes)} to {nextHourWord}";
        }

        return timeString;
    }

    private static string ConvertNumberToWord(int number)
    {
        if (number < 0 || number > 59)
        {
            // Handle numbers outside the range (0-12) if needed
            return "Invalid";
        }

        switch (number)
        {
            case 0:
                return "twelve";
            case 1:
                return "one";
            case 2:
                return "two";
            case 3:
                return "three";
            case 4:
                return "four";
            case 5:
                return "five";
            case 6:
                return "six";
            case 7:
                return "seven";
            case 8:
                return "eight";
            case 9:
                return "nine";
            case 10:
                return "ten";
            case 11:
                return "eleven";
            case 12:
                return "twelve";
            case 13:
                return "thirteen";
            case 14:
                return "fourteen";
            case 15:
                return "fifteen";
            case 16:
                return "sixteen";
            case 17:
                return "seventeen";
            case 18:
                return "eighteen";
            case 19:
                return "nineteen";
            case 20:
                return "twenty";
            case 21:
                return "twenty-one";
            case 22:
                return "twenty-two";
            case 23:
                return "twenty-three";
            case 24:
                return "twenty-four";
            case 25:
                return "twenty-five";
            case 26:
                return "twenty-six";
            case 27:
                return "twenty-seven";
            case 28:
                return "twenty-eight";
            case 29:
                return "twenty-nine";
            case 30:
                return "thirty";
            case 31:
                return "thirty-one";
            case 32:
                return "thirty-two";
            case 33:
                return "thirty-three";
            case 34:
                return "thirty-four";
            case 35:
                return "thirty-five";
            case 36:
                return "thirty-six";
            case 37:
                return "thirty-seven";
            case 38:
                return "thirty-eight";
            case 39:
                return "thirty-nine";
            case 40:
                return "forty";
            case 41:
                return "forty-one";
            case 42:
                return "forty-two";
            case 43:
                return "forty-three";
            case 44:
                return "forty-four";
            case 45:
                return "forty-five";
            case 46:
                return "forty-six";
            case 47:
                return "forty-seven";
            case 48:
                return "forty-eight";
            case 49:
                return "forty-nine";
            case 50:
                return "fifty";
            case 51:
                return "fifty-one";
            case 52:
                return "fifty-two";
            case 53:
                return "fifty-three";
            case 54:
                return "fifty-four";
            case 55:
                return "fifty-five";
            case 56:
                return "fifty-six";
            case 57:
                return "fifty-seven";
            case 58:
                return "fifty-eight";
            case 59:
                return "fifty-nine";


            default:
                Debug.Log("tried to convert number: " + number);
                return "Invalid";
        }
    }

}

