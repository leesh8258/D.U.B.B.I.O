using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    private const string PREF_MUSIC = "SM_VOL_MUSIC";
    private const string PREF_SFX = "SM_VOL_SFX";

    [Header("Library")]
    [SerializeField] private SoundLibrarySO soundLibrary;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolInitialSize = 8;
    [SerializeField] private Transform sfxPoolParent;

    [Header("Default Volumes")]
    [Range(0f, 1f)][SerializeField] private float defaultMusicVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float defaultSfxVolume = 0.8f;

    private float musicVolume;
    private float sfxVolume;

    private SoundBGMPlayer bgmPlayer;
    private SoundSFXPlayer sfxPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureBgmSource();
        LoadVolumes();

        bgmPlayer = new SoundBGMPlayer(this, soundLibrary, bgmSource, GetMusicVolume);
        sfxPlayer = new SoundSFXPlayer(this, soundLibrary, GetPoolRoot(), sfxPoolInitialSize, GetSfxVolume);
    }

    private void LateUpdate()
    {
        if (sfxPlayer == null) return;
        sfxPlayer.LateUpdate();
    }

    public void AudioPauseAll()
    {
        AudioListener.pause = true;
    }

    public void AudioResumeAll()
    {
        AudioListener.pause = false;
    }

    public void PlayBGM(string key)
    {
        if (bgmPlayer == null) return;
        bgmPlayer.Play(key);
    }

    public void PlayBGM(string key, BGMPlayOptions options)
    {
        if (bgmPlayer == null) return;
        bgmPlayer.Play(key, options);
    }

    public void StopBGM()
    {
        if (bgmPlayer == null) return;
        bgmPlayer.Stop();
    }

    public void StopBGM(float fadeOutDuration)
    {
        if (bgmPlayer == null) return;
        bgmPlayer.Stop(fadeOutDuration);
    }

    public SoundHandle Play(string key)
    {
        if (sfxPlayer == null) return null;
        return sfxPlayer.Play(key);
    }

    public SoundHandle Play(string key, SFXPlayOptions options)
    {
        if (sfxPlayer == null) return null;
        return sfxPlayer.Play(key, options);
    }

    public SoundHandle PlayAt(string key, Vector3 position)
    {
        if (sfxPlayer == null) return null;
        return sfxPlayer.PlayAt(key, position);
    }

    public SoundHandle PlayAt(string key, Vector3 position, SFXPlayOptions options)
    {
        if (sfxPlayer == null) return null;
        return sfxPlayer.PlayAt(key, position, options);
    }

    public SoundHandle PlayAttached(string key, Transform target)
    {
        if (sfxPlayer == null) return null;
        return sfxPlayer.PlayAttached(key, target);
    }

    public SoundHandle PlayAttached(string key, Transform target, SFXPlayOptions options)
    {
        if (sfxPlayer == null) return null;
        return sfxPlayer.PlayAttached(key, target, options);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PREF_MUSIC, musicVolume);

        if (bgmPlayer == null) return;
        bgmPlayer.RefreshVolume();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PREF_SFX, sfxVolume);

        if (sfxPlayer == null) return;
        sfxPlayer.RefreshVolumes();
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetSfxVolume()
    {
        return sfxVolume;
    }

    private void EnsureBgmSource()
    {
        if (bgmSource != null)
        {
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
            bgmSource.volume = 0f;
            bgmSource.pitch = 1f;
            return;
        }

        GameObject go = new GameObject("BGM_Source");
        go.transform.SetParent(transform, false);

        AudioSource createdSource = go.AddComponent<AudioSource>();
        createdSource.playOnAwake = false;
        createdSource.loop = true;
        createdSource.spatialBlend = 0f;
        createdSource.volume = 0f;
        createdSource.pitch = 1f;

        bgmSource = createdSource;
    }

    private Transform GetPoolRoot()
    {
        return sfxPoolParent != null ? sfxPoolParent : transform;
    }

    private void LoadVolumes()
    {
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_MUSIC, defaultMusicVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_SFX, defaultSfxVolume));
    }
}
