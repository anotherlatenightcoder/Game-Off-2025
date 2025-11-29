using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
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
    [Range(0f, 1f)]
    public float musicVolume = 1f;

    [Header("Sound Effects")]
    public List<NamedSFX> soundEffects = new List<NamedSFX>();

    private AudioSource musicSource;
    private AudioSource[] sfxSources;
    private const int SFX_SOURCE_COUNT = 5;

    private Dictionary<string, AudioClip> _lookup;
    
    private string _exclusiveKey = "KNOB_TURNING";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupChildren();
        BuildLookup();
        
        if (backgroundMusic)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void Update()
    {
        musicSource.volume = musicVolume;
        
        for (int i = 0; i < sfxSources.Length; i++)
        {
            if (!sfxSources[i].isPlaying)
            {
                sfxSources[i].clip = null;
            }
        }
    }
    
    private void BuildLookup()
    {
        _lookup = new Dictionary<string, AudioClip>();
        foreach (var sfx in soundEffects)
        {
            if (!string.IsNullOrEmpty(sfx.key) && sfx.clip != null)
            {
                _lookup[sfx.key] = sfx.clip;
            }
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
    
        // Lets only allow one knob turn to play
        // No idea how long the clip
        if (key == _exclusiveKey)
        {
            for (int i = 0; i < sfxSources.Length; i++)
            {
                if (sfxSources[i].isPlaying && sfxSources[i].clip == clip)
                {
                    // It's already playing so we ignore this request
                    return;
                }
            }
        }

        // all the other shit we need to play
        for (int i = 0; i < sfxSources.Length; i++)
        {
            if (!sfxSources[i].isPlaying)
            {
                sfxSources[i].clip = clip;
                sfxSources[i].volume = volume;
                sfxSources[i].Play();
                return;
            }
        }

        // !important
        sfxSources[0].clip = clip;
        sfxSources[0].volume = volume;
        sfxSources[0].Play();
    }
}