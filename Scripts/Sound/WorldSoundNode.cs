using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WorldSoundNode : MonoBehaviour
{
    [SerializeField] private string nodeId = "WorldSound_01";
    [SerializeField] private SFXType sfxType = SFXType.SFX_Button;
    [SerializeField] private bool playOnEnable = false;
    [SerializeField] private bool loop = true;
    [SerializeField] private string groupName = "";

    private AudioSource audioSource;
    private Coroutine fadeRoutine;
    private bool mutedByManager = false;
    private float cachedGlobalVolume = 1f;

    public string NodeId { get { return nodeId; } }
    public SFXType SfxType { get { return sfxType; } }
    public string GroupName { get { return groupName; } }

    private SoundManager soundManager;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = loop;
            audioSource.spatialBlend = 1f;
        }
    }

    private void Start()
    {
        soundManager = SoundManager.Instance;
        soundManager.RegisterWorldNode(this);

        if (playOnEnable) soundManager.PlaySFX3D(nodeId);
    }

    private void OnEnable()
    {
        if (soundManager != null)
        {
            soundManager.RegisterWorldNode(this);

            if (playOnEnable)
            {
                soundManager.PlaySFX3D(nodeId);
            }
        }
    }

    private void OnDisable()
    {
        if (soundManager != null)
        {
            soundManager.UnregisterWorldNode(this);
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    // SoundManager가 실제 재생시킬 때 호출
    public void Play3D(SFXEntry entry, float globalVolume, PlayStartMode? startOverride, float fadeInOverride)
    {
        if (audioSource == null || entry.clip == null)
        {
            return;
        }

        cachedGlobalVolume = globalVolume;
        audioSource.clip = entry.clip;
        audioSource.loop = loop;

        // 뮤트 상태면 볼륨 0으로 시작
        float targetVol = mutedByManager ? 0f : cachedGlobalVolume;

        PlayStartMode modeToUse = startOverride.HasValue ? startOverride.Value : entry.startMode;
        float fadeToUse = startOverride.HasValue ? fadeInOverride : entry.fadeInDuration;

        if (modeToUse == PlayStartMode.FadeIn && fadeToUse > 0f)
        {
            audioSource.volume = 0f;
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }
            fadeRoutine = StartCoroutine(FadeInRoutine(fadeToUse, targetVol));
        }
        else
        {
            audioSource.volume = targetVol;
        }

        audioSource.Play();
    }

    public void Stop3D(StopMode? stopOverride, float fadeOutOverride)
    {
        if (audioSource == null) return;
        
        StopMode modeToUse = stopOverride.HasValue ? stopOverride.Value : StopMode.Immediate;
        float fadeToUse = stopOverride.HasValue ? fadeOutOverride : 0f;

        if (modeToUse == StopMode.FadeOut && fadeToUse > 0f && audioSource.isPlaying)
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }
            fadeRoutine = StartCoroutine(FadeOutRoutine(fadeToUse));
        }
        else
        {
            audioSource.Stop();
        }
    }

    public void ApplyGlobalVolume(float globalVolume)
    {
        cachedGlobalVolume = globalVolume;
        if (audioSource == null)
        {
            return;
        }
        if (mutedByManager)
        {
            audioSource.volume = 0f;
        }
        else
        {
            // 현재 재생 중이면 볼륨만 조절
            audioSource.volume = cachedGlobalVolume;
        }
    }

    public void SetMuted(bool mute, float globalVolume)
    {
        mutedByManager = mute;
        ApplyGlobalVolume(globalVolume);
    }

    public void SetSfxType(SFXType newType)
    {
        sfxType = newType;
    }

    private IEnumerator FadeInRoutine(float dur, float target)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            audioSource.volume = target * k;
            yield return null;
        }
        audioSource.volume = target;
        fadeRoutine = null;
    }

    private IEnumerator FadeOutRoutine(float dur)
    {
        float start = audioSource.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / dur);
            audioSource.volume = start * k;
            yield return null;
        }
        audioSource.Stop();
        fadeRoutine = null;
    }
}
