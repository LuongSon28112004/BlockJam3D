using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public static bool CF_DefaultMusic
    {
        get => PlayerPrefs.GetInt("cf_default_music", 0) == 1;
        set => PlayerPrefs.SetInt("cf_default_music", value ? 1 : 0);
    }
    public static bool AudioMusicSetting
    {
        get => PlayerPrefs.GetInt("audio_music_setting", CF_DefaultMusic ? 1 : 0) == 1;
        set => PlayerPrefs.SetInt("audio_music_setting", value ? 1 : 0);
    }

    public static bool AudioSoundSetting
    {
        get => PlayerPrefs.GetInt("audio_sound_setting", 1) == 1;
        set => PlayerPrefs.SetInt("audio_sound_setting", value ? 1 : 0);
    }

    public static bool AudioVibrateSetting
    {
        get => PlayerPrefs.GetInt(/*GameHelper.KeyConfigVibrate*/"vibrate_setting", 1) == 1;
        set => PlayerPrefs.SetInt(/*GameHelper.KeyConfigVibrate*/"vibrate_setting", value ? 1 : 0);
    }
    public static AudioManager Instance;
    int playing;
    bool canPlay = true;

    [Header("SoundFX")] public AudioClip[] soundsFX;
    [Header("Music")] public AudioClip[] musics;

    public AudioSource musicSource;
    public AudioSource soundSource;

    private float musicPlayTime;
    private bool musicIsPlaying;

    [SerializeField] AudioMixer audioMixer;
    private AudioConfiguration audioConfiguration;
    [field: SerializeField] private List<AudioSource> listAudioSources = new List<AudioSource>();
    public float Ratio_Sound
    {
        get
        {
#if UNITY_ANDROID
            return PlayerPrefs.GetFloat("cf_ratio_sound", 1f);
#endif
            return PlayerPrefs.GetFloat("cf_ratio_sound", .5f);
        }
        set
        {
            PlayerPrefs.SetFloat("cf_ratio_sound", Mathf.Clamp(value, 0, 1));
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        listAudioSources.Clear();
        listAudioSources.Add(soundSource);
        for (int i = 0; i < 5; i++)
        {
            AudioSource sourceFX = Instantiate(soundSource, transform);
            listAudioSources.Add(sourceFX);
        }
        musicSource.volume = AudioMusicSetting ? 1 * Ratio_Sound : 0;
        soundSource.volume = AudioSoundSetting ? 1 * Ratio_Sound : 0;
        this.WaitUntil(() => audioMixer != null, () =>
        {
            FixVolumeSFX();
            FixVolumeMusic();
        });
    }

    private void Start()
    {
        audioConfiguration = AudioSettings.GetConfiguration();
    }

    private AudioSource GetAudioSource()
    {
        AudioSource audioSource = null;
        for (int i = 0; i < listAudioSources.Count; i++)
        {
            if (listAudioSources[i].isPlaying)
            {
                continue;
            }
            else
            {
                audioSource = listAudioSources[i];
                break;
            }
        }
        if (audioSource == null)
        {
            audioSource = listAudioSources[0];
        }
        return audioSource;
    }
    public void ChangeStateAudio()
    {
        if (AudioMusicSetting)
        {
            if (/*GameManager.GameSate == GameSate.None*/true)
            {
                PlayBGMusicMain();
            }
            else
            {
                PlayBGMusicInGame();
            }
        }
        else
        {
            StopMusic();
        }
    }

    public void PlayOneShotByClip(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            soundSource.clip = clip;
            soundSource.PlayOneShot(clip, volume);
        }

        soundSource.volume = AudioSoundSetting ? 1 : 0;
    }

    public void Play(string name, float volume, bool isloop = false)
    {
        // if (isTurnOnSound == false) return;
        AudioClip s = System.Array.Find(musics, sound => sound.name == name);
        if (s != null)
        {
            musicSource.clip = s;
            musicSource.volume = volume * Ratio_Sound;
            musicSource.loop = isloop;
            musicSource.Play();
        }

        musicSource.volume = AudioMusicSetting ? volume * Ratio_Sound : 0;
    }

    public void PlayBGMusicMain()
    {
        //Play(AUDIO_CLIP_NAME.MUSIC_Main_BG, 0.4f, true);
    }
    public void PlayBGMusicInGame()
    {
        //Play(AUDIO_CLIP_NAME.MUSIC_In_Game_BG, 0.4f, true);
    }

    public void PlayClip(AudioClip s, float volume, bool isloop = false)
    {
        // if (isTurnOnSound == false) return;
        if (s != null)
        {
            musicSource.clip = s;
            musicSource.volume = volume * Ratio_Sound;
            musicSource.loop = isloop;
            musicSource.Play();
        }

        musicSource.volume = AudioMusicSetting ? volume * Ratio_Sound : 0;
    }

    public void StopMusic() => musicSource.Stop();
    public void StopSFX() => soundSource.Stop();

    public void PlayOneShot(string name, float volume = 1, float delayPlay = 0)
    {
        // if (isTurnOnSound == false || !gameObject.activeSelf) return;
        if (playing > 10 && canPlay) return;
        AudioSource audioSource = GetAudioSource();
        StartCoroutine(PlayByName(audioSource, name, volume, delayPlay));
        canPlay = false;
        audioSource.volume = AudioSoundSetting ? volume * Ratio_Sound : 0;
    }

    public void PlayOneShot(AudioClip clip, float volume = 1, float delayPlay = 0)
    {
        // if (isTurnOnSound == false || !gameObject.activeSelf) return;
        if (playing > 10 && canPlay) return;
        AudioSource audioSource = GetAudioSource();
        StartCoroutine(PlayByClip(audioSource, clip, volume, delayPlay));
        canPlay = false;
        audioSource.volume = AudioSoundSetting ? volume * Ratio_Sound : 0;
    }

    IEnumerator PlayByName(AudioSource _audioSource, string _name, float _volume, float _delayPlay = 0)
    {
        AudioClip s = System.Array.Find(soundsFX, sound => sound.name == _name);
        yield return new WaitForSeconds(_delayPlay);
        _volume = _volume * Ratio_Sound;
        if (s != null)
        {
            playing++;
            canPlay = true;
            _audioSource.clip = s;
            _audioSource.PlayOneShot(s, _volume);
            yield return new WaitForSeconds(0.2f);
            playing--;
        }

        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator PlayByClip(AudioSource _audioSource, AudioClip clip, float _volume, float _delayPlay = 0)
    {
        yield return new WaitForSeconds(_delayPlay);
        _volume = _volume * Ratio_Sound;
        if (clip != null)
        {
            playing++;
            canPlay = true;
            _audioSource.clip = clip;
            _audioSource.PlayOneShot(clip, _volume);
            yield return new WaitForSeconds(Random.Range(0.1f, 0.2f));
            playing--;
        }

        yield return new WaitForSeconds(0.2f);
    }

    public void SetVolume(float volume)
    {
        musicSource.volume = volume;
    }

    public void PauseAudio()
    {
        AudioListener.pause = true;
    }

    public void ResumeAudio()
    {
        AudioListener.pause = false;
    }

    public void ResetAudio()
    {
        AudioSettings.Reset(audioConfiguration);
        FixVolumeMusic();
        if (musicIsPlaying)
        {
            musicSource.time = musicPlayTime;
            musicSource.Play();
        }
    }

    public void SetCacheAudio()
    {
        musicPlayTime = musicSource.time;
        musicIsPlaying = musicSource.isPlaying;
    }

    public void settingMusic(int volume)
    {
        musicSource.volume = volume;
    }

    public void settingSound(int volume)
    {
        soundSource.volume = volume;
    }

    public void FixVolumeSFX()
    {
        float vol = AudioSoundSetting ? 1 * Ratio_Sound : 0;
        float dB = Mathf.Log10(Mathf.Clamp(vol, 0.0001f, 1)) * 20;
        audioMixer.SetFloat("SFXVolume", dB);
    }

    public void FixVolumeMusic()
    {
        float vol = AudioMusicSetting ? 1 * Ratio_Sound : 0;
        float dB = Mathf.Log10(Mathf.Clamp(vol, 0.0001f, 1)) * 20;
        audioMixer.SetFloat("SFXMusic", dB);
    }

    public void PlayVibrate()
    {
        //GameHelper.Instance.Vibrate(type_Vibreate);
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate(); // Short default vibration
#endif
    }
}