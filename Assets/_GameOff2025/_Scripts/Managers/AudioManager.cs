using System.Collections;
using System.Collections.Generic;
using Route24.Core;
using UnityEngine;

public class AudioManager : MonoBehaviour, IService, IInitializable
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class NamedSFX
    {
        public string key;
        public AudioClip clip;
    }

    [Header("Background Music")]
    public AudioClip backgroundMusic;
    public AudioClip endDayMusic;

    [Range(0f, 1f)]
    public float musicVolume = 1f;
    
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("Sound Effects")]
    public List<NamedSFX> soundEffects = new List<NamedSFX>();

    private AudioSource musicSource;
    private AudioSource[] sfxSources;
    private const int SFX_SOURCE_COUNT = 5;

    private Dictionary<string, AudioClip> _lookup;
    private string _exclusiveKey = "KNOB_TURNING";

    private EventHub _eventHub;
    private Coroutine _fadeRoutine;

    public int InitializationPriority => 50;

    private bool _hasInitialized = false;

    public void Initialize()
    {
        if (Instance == null) Instance = this;

        SetupChildren();
        BuildLookup();

        _eventHub = ServiceLocator.Get<EventHub>();
        _eventHub.Subscribe<TutorialStartedEvent>(OnTutorialStarted);
        _eventHub.Subscribe<DayStartedEvent>(OnDayStarted);
        _eventHub.Subscribe<DayEndedEvent>(OnDayEnded);

        _hasInitialized = true;
    }

    private void Update()
    {
        if (!_hasInitialized)
            return;
        
        // Always clamp the music source to the current user volume
        musicSource.volume = Mathf.Clamp(musicSource.volume, 0f, musicVolume);

        for (int i = 0; i < sfxSources.Length; i++)
        {
            if (!sfxSources[i].isPlaying)
                sfxSources[i].clip = null;
        }
    }

    // -------------------------------------------------------------------------
    // EVENT HOOKS
    // -------------------------------------------------------------------------
    
    private void OnTutorialStarted(TutorialStartedEvent obj)
    {
        PlayMusic(backgroundMusic);
    }

    private void OnDayStarted(DayStartedEvent e)
    {
        PlayMusic(backgroundMusic);
    }

    private void OnDayEnded(DayEndedEvent e)
    {
        PlayMusic(endDayMusic);
    }

    // -------------------------------------------------------------------------
    // PUBLIC API
    // -------------------------------------------------------------------------

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);

        // Adjust immediately but not above the new cap
        if (musicSource != null)
            musicSource.volume = Mathf.Min(musicSource.volume, musicVolume);
    }
    
    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);

        // Update all internal SFX sources
        for (int i = 0; i < sfxSources.Length; i++)
        {
            if (sfxSources[i] != null)
                sfxSources[i].volume = sfxVolume;
        }

        // Notify external listeners, which in our case is the hacky oscillator 
        _eventHub?.Publish(new SfxVolumeChangedEvent(sfxVolume));
    }

    // -------------------------------------------------------------------------
    // MUSIC LOGIC
    // -------------------------------------------------------------------------

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeToClip(clip));
    }

    private IEnumerator FadeToClip(AudioClip newClip)
    {
        float duration = 2f;

        // Step 1 — Fade out current music
        float startVol = musicSource.volume;
        float t = 0;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        // Step 2 — Swap the clip
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        // Step 3 — Fade in to musicVolume
        t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t);
            yield return null;
        }

        musicSource.volume = musicVolume;
        _fadeRoutine = null;
    }

    // -------------------------------------------------------------------------
    // SFX LOGIC
    // -------------------------------------------------------------------------

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, AudioClip>();
        foreach (var sfx in soundEffects)
        {
            if (!string.IsNullOrEmpty(sfx.key) && sfx.clip != null)
                _lookup[sfx.key] = sfx.clip;
        }
    }

    private void SetupChildren()
    {
        Transform musicChild = transform.Find("MusicSource");
        if (musicChild == null)
        {
            GameObject m = new GameObject("MusicSource");
            m.transform.SetParent(transform);
            musicSource = m.AddComponent<AudioSource>();
        }
        else
        {
            musicSource = musicChild.GetComponent<AudioSource>();
        }

        sfxSources = new AudioSource[SFX_SOURCE_COUNT];
        for (int i = 0; i < SFX_SOURCE_COUNT; i++)
        {
            Transform sfxChild = transform.Find("SFXSource_" + i);

            if (sfxChild == null)
            {
                GameObject s = new GameObject("SFXSource_" + i);
                s.transform.SetParent(transform);
                sfxSources[i] = s.AddComponent<AudioSource>();
                sfxSources[i].playOnAwake = false;
            }
            else
            {
                sfxSources[i] = sfxChild.GetComponent<AudioSource>();
            }
        }
    }

    public void PlaySFX(string key, float volume = 1f)
    {
        if (!_lookup.ContainsKey(key))
        {
            Debug.LogWarning("SFX key not found: " + key);
            return;
        }

        AudioClip clip = _lookup[key];
        if (clip == null)
        {
            Debug.LogWarning("SFX clip missing for key: " + key);
            return;
        }

        // Exclusive clip (knob turning)
        if (key == _exclusiveKey)
        {
            for (int i = 0; i < sfxSources.Length; i++)
            {
                if (sfxSources[i].isPlaying && sfxSources[i].clip == clip)
                    return; // Already playing
            }
        }

        // Normal SFX logic
        for (int i = 0; i < sfxSources.Length; i++)
        {
            if (!sfxSources[i].isPlaying)
            {
                sfxSources[i].clip = clip;
                sfxSources[i].volume = volume * sfxVolume;
                sfxSources[i].Play();
                return;
            }
        }

        // Fallback: play on the first source
        sfxSources[0].clip = clip;
        sfxSources[0].volume = volume * sfxVolume;
        sfxSources[0].Play();
    }
}
