using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    private class SfxInstance
    {
        public int instanceId;
        public AudioSource source;
        public SFXType type;
        public SFXEntry entry;
        public float runtimeGain;
        public Coroutine routine;
    }

    [Header("Libraries (SO)")]
    [SerializeField] private BGM_SO bgmLibrary;
    [SerializeField] private SFX_SO sfxLibrary;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX")]
    [SerializeField] private int sfxPoolInitialSize = 8;
    [SerializeField] private Transform sfxPoolParent;

    [Header("Player Volumes (Base)")]
    [Range(0f, 1f)][SerializeField] private float defaultMusicVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float defaultSfxVolume = 0.8f;

    private float musicVolume;
    private float sfxVolume;
    private float bgmReactiveGain = 1f;

    private const string PREF_MUSIC = "SM_VOL_MUSIC";
    private const string PREF_SFX = "SM_VOL_SFX";

    private readonly Dictionary<BGMType, BGMEntry> bgmEntryDict = new Dictionary<BGMType, BGMEntry>();
    private readonly Dictionary<SFXType, SFXEntry> sfxEntryDict = new Dictionary<SFXType, SFXEntry>();

    private BGMType? currentBgmType = null;
    private float bgmRuntimeGain = 1f;
    private Coroutine bgmFadeRoutine;

    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private readonly List<SfxInstance> activeSfxInstances = new List<SfxInstance>();
    private int _nextSfxInstanceId = 1;

    private readonly Dictionary<string, WorldSoundNode> world3DNodes = new Dictionary<string, WorldSoundNode>();
    private readonly List<WorldSoundNode> world3DNodeList = new List<WorldSoundNode>();
    private readonly HashSet<string> mutedWorldGroups = new HashSet<string>();

    #region Unity Lifecycle
    private void Awake()
    {
        EnsureBgmSource();
        BuildEntryDictionaries();
        BuildSfxPool();

        LoadVolumes();
        ApplyBgmVolume();

        if (Instance == null)
        {
            Instance = this;
        }
    }

    #endregion

    #region Build Helpers
    private void EnsureBgmSource()
    {
        if (bgmSource == null)
        {
            GameObject go = new GameObject("BGM_Source");
            go.transform.SetParent(transform, false);
            AudioSource audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.volume = 0f;
            audioSource.spatialBlend = 0f;
            audioSource.pitch = 1f;
            bgmSource = audioSource;
        }
    }

    private void BuildEntryDictionaries()
    {
        bgmEntryDict.Clear();
        sfxEntryDict.Clear();

        if (bgmLibrary != null && bgmLibrary.entries != null)
        {
            for (int i = 0; i < bgmLibrary.entries.Count; i++)
            {
                BGMEntry entry = bgmLibrary.entries[i];
                if (bgmEntryDict.ContainsKey(entry.type))
                {
                    Debug.LogWarning($"[SoundManager] Duplicate BGM key: {entry.type}. Overwriting.");
                }

                bgmEntryDict[entry.type] = entry;
            }
        }

        else
        {
            Debug.LogWarning("[SoundManager] BGM Library is not assigned.");
        }

        if (sfxLibrary != null && sfxLibrary.entries != null)
        {
            for (int i = 0; i < sfxLibrary.entries.Count; i++)
            {
                SFXEntry entry = sfxLibrary.entries[i];
                if (sfxEntryDict.ContainsKey(entry.type))
                {
                    Debug.LogWarning($"[SoundManager] Duplicate SFX key: {entry.type}. Overwriting.");
                }

                sfxEntryDict[entry.type] = entry;
            }
        }

        else
        {
            Debug.LogWarning("[SoundManager] SFX Library is not assigned.");
        }
    }

    private void BuildSfxPool()
    {
        for (int i = 0; i < sfxPoolInitialSize; i++)
        {
            AudioSource src = CreateSfxSource(i);
            sfxSources.Add(src);
        }
    }

    private AudioSource CreateSfxSource(int index)
    {
        GameObject go = new GameObject($"SFX_Source_{index:D2}");
        if (sfxPoolParent != null)
        {
            go.transform.SetParent(sfxPoolParent, false);
        }

        else
        {
            go.transform.SetParent(transform, false);
        }

        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 0f;
        audioSource.spatialBlend = 0f; // 2D
        return audioSource;
    }

    private AudioSource GetFreeSfxSource()
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            AudioSource src = sfxSources[i];
            if (!src.isPlaying)
            {
                return src;
            }
        }

        // 풀 확장
        AudioSource newSrc = CreateSfxSource(sfxSources.Count);
        sfxSources.Add(newSrc);
        return newSrc;
    }
    #endregion

    #region 일시정지 API

    public void AudioPauseAll()
    {
        AudioListener.pause = true;
    }

    public void AudioResumeAll()
    {
        AudioListener.pause = false;
    }
    #endregion

    #region BGM Public API
    public void PlayBGM(BGMType type)
    {
        if (!bgmEntryDict.TryGetValue(type, out BGMEntry entry) || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM entry not found or clip null: {type}");
            return;
        }

        if (currentBgmType.HasValue && currentBgmType.Value.Equals(type))
        {
            if (bgmSource.clip == entry.clip && bgmSource.isPlaying)
            {
                return;
            }
        }

        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = null;
        }

        bgmFadeRoutine = StartCoroutine(PlayBgmRoutine(entry));
    }

    public void StopBGM()
    {
        if (!currentBgmType.HasValue || bgmSource == null || bgmSource.clip == null)
        {
            return;
        }

        BGMEntry entry;
        bool hasEntry = bgmEntryDict.TryGetValue(currentBgmType.Value, out entry);

        if (!hasEntry)
        {
            if (bgmFadeRoutine != null)
            {
                StopCoroutine(bgmFadeRoutine);
                bgmFadeRoutine = null;
            }
            bgmSource.Stop();
            bgmSource.clip = null;
            currentBgmType = null;
            bgmRuntimeGain = 1f;
            ApplyBgmVolume();
            return;
        }

        StopMode stopMode = entry.stopMode;
        float fadeOut = entry.fadeOutDuration;

        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = null;
        }

        if (stopMode == StopMode.Immediate || fadeOut <= 0f)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            currentBgmType = null;
            bgmRuntimeGain = 1f;
            ApplyBgmVolume();
            return;
        }

        bgmFadeRoutine = StartCoroutine(BgmFadeOutAndStop(fadeOut));
    }
    #endregion

    #region 2D SFX Public API
    public void PlaySFX(SFXType type)
    {
        if (!sfxEntryDict.TryGetValue(type, out SFXEntry entry) || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX entry not found or clip null: {type}");
            return;
        }

        AudioSource audioSource = GetFreeSfxSource();
        ConfigureSfxSourceForPlay(audioSource, entry.clip);

        SfxInstance inst = new SfxInstance
        {
            instanceId = 0,
            source = audioSource,
            type = type,
            entry = entry,
            runtimeGain = (entry.startMode == PlayStartMode.FadeIn && entry.fadeInDuration > 0f) ? 0f : 1f,
            routine = null
        };

        ApplySfxVolume(inst);
        audioSource.Play();

        inst.routine = StartCoroutine(RunSfxLifecycle(inst));
        activeSfxInstances.Add(inst);
    }

    public void PlaySFXClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SoundManager] PlaySFXClip: clip is null.");
            return;
        }

        AudioSource audioSource = GetFreeSfxSource();
        ConfigureSfxSourceForPlay(audioSource, clip);

        SFXEntry entry = new SFXEntry
        {
            type = SFXType.SFX_Button,
            clip = clip,
            startMode = PlayStartMode.Immediate,
            fadeInDuration = 0f,
            stopMode = StopMode.Immediate,
            fadeOutDuration = 0f
        };

        SfxInstance inst = new SfxInstance
        {
            instanceId = 0,
            source = audioSource,
            type = entry.type,
            entry = entry,
            runtimeGain = 1f,
            routine = null
        };

        ApplySfxVolume(inst);
        audioSource.Play();

        inst.routine = StartCoroutine(RunSfxLifecycle(inst));
        activeSfxInstances.Add(inst);
    }

    public void StopAllSFX()
    {
        for (int i = 0; i < activeSfxInstances.Count; i++)
        {
            SfxInstance inst = activeSfxInstances[i];
            if (inst != null && inst.source != null)
            {
                if (inst.routine != null)
                {
                    StopCoroutine(inst.routine);
                }
                inst.source.Stop();
                inst.source.clip = null;
                inst.runtimeGain = 1f;
            }
        }
        activeSfxInstances.Clear();
    }

    public int PlaySFXLoop(SFXType type, float initialGain = 1f)
    {
        if (!sfxEntryDict.TryGetValue(type, out SFXEntry entry) || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] PlaySFXLoop: entry not found or clip null: {type}");
            return -1;
        }

        AudioSource src = GetFreeSfxSource();
        ConfigureSfxSourceForPlay(src, entry.clip);
        src.loop = true;

        SfxInstance inst = new SfxInstance
        {
            instanceId = _nextSfxInstanceId++,
            source = src,
            type = type,
            entry = entry,
            runtimeGain = Mathf.Clamp01(initialGain),
            routine = null
        };

        ApplySfxVolume(inst);
        src.Play();
        activeSfxInstances.Add(inst);
        return inst.instanceId;
    }

    public void SetSFXGain(int instanceId, float gain01)
    {
        if (instanceId < 0) return;
        SfxInstance inst = activeSfxInstances.Find(x => x.instanceId == instanceId);
        if (inst == null || inst.source == null) return;
        inst.runtimeGain = Mathf.Clamp01(gain01);
        ApplySfxVolume(inst);
    }

    public void StopSFXInstance(int instanceId, StopMode stopMode = StopMode.FadeOut, float fadeOutDuration = 0.15f)
    {
        if (instanceId < 0) return;
        SfxInstance inst = activeSfxInstances.Find(x => x.instanceId == instanceId);
        if (inst == null) return;

        StartCoroutine(StopSfxInstanceRoutine(inst, stopMode, fadeOutDuration));
    }
    #endregion

    #region 3D SFX Public API

    public void RegisterWorldNode(WorldSoundNode node)
    {
        if (node == null)
        {
            return;
        }

        string id = node.NodeId;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[SoundManager] WorldSoundNode register failed: empty id.");
            return;
        }

        world3DNodes[id] = node;
        if (!world3DNodeList.Contains(node))
        {
            world3DNodeList.Add(node);
        }

        // 현재 SFX 볼륨을 즉시 적용해서 옵션창 변화가 바로 먹도록
        node.ApplyGlobalVolume(sfxVolume);

        // 그룹이 뮤트되어 있으면 바로 뮤트
        string group = node.GroupName;
        if (!string.IsNullOrEmpty(group) && mutedWorldGroups.Contains(group))
        {
            node.SetMuted(true, sfxVolume);
        }
    }

    public void UnregisterWorldNode(WorldSoundNode node)
    {
        if (node == null)
        {
            return;
        }
        if (world3DNodes.ContainsKey(node.NodeId))
        {
            world3DNodes.Remove(node.NodeId);
        }
        if (world3DNodeList.Contains(node))
        {
            world3DNodeList.Remove(node);
        }
    }

    public void PlaySFX3D(string nodeId)
    {
        PlaySFX3D(nodeId, null, 0f);
    }

    public void PlaySFX3D(string nodeId, PlayStartMode? startModeOverride, float fadeInOverride)
    {
        WorldSoundNode node;
        if (!world3DNodes.TryGetValue(nodeId, out node) || node == null)
        {
            Debug.LogWarning($"[SoundManager] PlaySFX3D: node not found: {nodeId}");
            return;
        }

        SFXType sfxType = node.SfxType;
        SFXEntry entry;
        if (!sfxEntryDict.TryGetValue(sfxType, out entry) || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] PlaySFX3D: SFX entry not found or clip null: {sfxType}");
            return;
        }

        node.Play3D(entry, sfxVolume, startModeOverride, fadeInOverride);
    }

    public void StopSFX3D(string nodeId)
    {
        StopSFX3D(nodeId, null, 0f);
    }

    public void StopSFX3D(string nodeId, StopMode? stopModeOverride, float fadeOutOverride)
    {
        WorldSoundNode node;
        if (!world3DNodes.TryGetValue(nodeId, out node) || node == null)
        {
            return;
        }

        node.Stop3D(stopModeOverride, fadeOutOverride);
    }

    public void ChangeSFX3DClip(string nodeId, SFXType newType, bool playImmediately = false)
    {
        WorldSoundNode node;
        if (!world3DNodes.TryGetValue(nodeId, out node) || node == null)
        {
            return;
        }

        node.SetSfxType(newType);

        if (playImmediately)
        {
            PlaySFX3D(nodeId);
        }
    }

    public void Mute3DGroup(string groupName, bool mute)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            return;
        }

        if (mute)
        {
            mutedWorldGroups.Add(groupName);
        }
        else
        {
            mutedWorldGroups.Remove(groupName);
        }

        for (int i = 0; i < world3DNodeList.Count; i++)
        {
            WorldSoundNode node = world3DNodeList[i];
            if (node != null && node.GroupName == groupName)
            {
                node.SetMuted(mute, sfxVolume);
            }
        }
    }

    private void ApplyAllWorld3DVolumes()
    {
        for (int i = 0; i < world3DNodeList.Count; i++)
        {
            WorldSoundNode node = world3DNodeList[i];
            if (node != null)
            {
                node.ApplyGlobalVolume(sfxVolume);
            }
        }
    }

    #endregion

    #region Volumes
    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PREF_MUSIC, musicVolume);
        PlayerPrefs.Save();
        ApplyBgmVolume();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PREF_SFX, sfxVolume);
        PlayerPrefs.Save();

        ApplyAllSfxVolumes();
        ApplyAllWorld3DVolumes();
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetSfxVolume()
    {
        return sfxVolume;
    }
    #endregion

    #region Utilities
    private void ApplyBgmVolume()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.volume = musicVolume * Mathf.Clamp01(bgmRuntimeGain) * Mathf.Clamp01(bgmReactiveGain);
    }

    private void ApplySfxVolume(SfxInstance inst)
    {
        if (inst == null || inst.source == null)
        {
            return;
        }
        inst.source.volume = sfxVolume * Mathf.Clamp01(inst.runtimeGain);
    }

    private void ApplyAllSfxVolumes()
    {
        for (int i = 0; i < activeSfxInstances.Count; i++)
        {
            ApplySfxVolume(activeSfxInstances[i]);
        }
    }

    private void ConfigureSfxSourceForPlay(AudioSource src, AudioClip clip)
    {
        src.clip = clip;
        src.loop = false;
    }

    private void LoadVolumes()
    {
        musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC, defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat(PREF_SFX, defaultSfxVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
    }

    #endregion

    #region Coroutines
    private IEnumerator PlayBgmRoutine(BGMEntry entry)
    {
        // 기존 곡 정리
        if (bgmSource.isPlaying && bgmSource.clip != null)
        {
            StopMode stopMode = StopMode.Immediate;
            float fadeOut = 0f;

            if (currentBgmType.HasValue)
            {
                BGMEntry prevEntry;
                if (bgmEntryDict.TryGetValue(currentBgmType.Value, out prevEntry))
                {
                    stopMode = prevEntry.stopMode;
                    fadeOut = prevEntry.fadeOutDuration;
                }
            }

            if (stopMode == StopMode.FadeOut && fadeOut > 0f)
            {
                yield return BgmFadeOut(fadeOut);
                bgmSource.Stop();
            }
            else
            {
                bgmSource.Stop();
            }
        }

        // 새 곡
        bgmSource.clip = entry.clip;
        bgmSource.loop = entry.loop;
        currentBgmType = entry.type;
        bgmSource.pitch = 1.0f;

        bgmRuntimeGain = (entry.startMode == PlayStartMode.FadeIn && entry.fadeInDuration > 0f) ? 0f : 1f;
        ApplyBgmVolume();

        bgmSource.Play();

        if (entry.startMode == PlayStartMode.FadeIn && entry.fadeInDuration > 0f)
        {
            float duration = entry.fadeInDuration;
            float t = 0f;
            while (t < duration)
            {
                if (AudioListener.pause)
                {
                    yield return null;
                    continue;
                }

                t += Time.deltaTime;
                bgmRuntimeGain = Mathf.Clamp01(t / duration);
                ApplyBgmVolume();
                yield return null;
            }
            bgmRuntimeGain = 1f;
            ApplyBgmVolume();
        }

        bgmFadeRoutine = null;
    }

    private IEnumerator BgmFadeOut(float duration)
    {
        float start = bgmRuntimeGain;
        float t = 0f;
        while (t < duration)
        {
            if (AudioListener.pause)
            {
                yield return null;
                continue;
            }

            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            bgmRuntimeGain = start * k;
            ApplyBgmVolume();
            yield return null;
        }
        bgmRuntimeGain = 0f;
        ApplyBgmVolume();
    }

    private IEnumerator BgmFadeOutAndStop(float duration)
    {
        yield return BgmFadeOut(duration);
        bgmSource.Stop();
        bgmSource.clip = null;
        currentBgmType = null;
        bgmRuntimeGain = 1f;
        ApplyBgmVolume();
        bgmFadeRoutine = null;
    }

    private IEnumerator RunSfxLifecycle(SfxInstance inst)
    {
        // FadeIn
        if (inst.entry.startMode == PlayStartMode.FadeIn && inst.entry.fadeInDuration > 0f)
        {
            float tIn = 0f;
            float dIn = inst.entry.fadeInDuration;
            while (tIn < dIn)
            {
                if (AudioListener.pause)
                {
                    yield return null;
                    continue;
                }

                tIn += Time.deltaTime;
                inst.runtimeGain = Mathf.Clamp01(tIn / dIn);
                ApplySfxVolume(inst);
                yield return null;
            }
            inst.runtimeGain = 1f;
            ApplySfxVolume(inst);
        }

        // 본체 재생 시간 대기
        float clipLen = (inst.source.clip != null) ? inst.source.clip.length : 0f;
        float fadeOutDur = (inst.entry.stopMode == StopMode.FadeOut) ? Mathf.Max(0f, inst.entry.fadeOutDuration) : 0f;
        float playHold = Mathf.Max(0f, clipLen - fadeOutDur);
        float elapsed = 0f;
        while (elapsed < playHold && inst.source.isPlaying)
        {
            if (AudioListener.pause)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // FadeOut
        if (inst.entry.stopMode == StopMode.FadeOut && fadeOutDur > 0f && inst.source.isPlaying)
        {
            float start = inst.runtimeGain;
            float t = 0f;
            while (t < fadeOutDur)
            {
                if (AudioListener.pause)
                {
                    yield return null;
                    continue;
                }

                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / fadeOutDur);
                inst.runtimeGain = start * k;
                ApplySfxVolume(inst);
                yield return null;
            }
            inst.runtimeGain = 0f;
            ApplySfxVolume(inst);
        }

        // 정리
        inst.source.Stop();
        inst.source.clip = null;
        activeSfxInstances.Remove(inst);
    }

    private IEnumerator StopSfxInstanceRoutine(SfxInstance inst, StopMode stopMode, float fadeOutDuration)
    {
        if (inst == null || inst.source == null)
        {
            activeSfxInstances.Remove(inst);
            yield break;
        }

        if (stopMode == StopMode.FadeOut && fadeOutDuration > 0f && inst.source.isPlaying)
        {
            float startGain = inst.runtimeGain;
            float t = 0f;
            while (t < fadeOutDuration && inst.source != null)
            {
                if (AudioListener.pause) { yield return null; continue; }

                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / fadeOutDuration);
                inst.runtimeGain = startGain * k;
                ApplySfxVolume(inst);
                yield return null;
            }

            inst.runtimeGain = 0f;
            ApplySfxVolume(inst);
        }

        else
        {
            inst.runtimeGain = 0f;
            ApplySfxVolume(inst);
        }

        inst.source.Stop();
        inst.source.clip = null;

        activeSfxInstances.Remove(inst);
    }

    #endregion

}
