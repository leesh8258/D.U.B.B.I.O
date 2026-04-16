using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundSFXPlayer
{
    private class ActiveSound
    {
        public int id;
        public string key;
        public AudioSource source;
        public Transform followTarget;
        public bool loop;
        public bool isAttached;
        public bool useUnscaledTime;
        public float baseVolume;
        public float runtimeGain;
        public Coroutine lifecycleRoutine;
        public Coroutine fadeRoutine;
    }

    private readonly MonoBehaviour coroutineOwner;
    private readonly SoundLibrarySO soundLibrary;
    private readonly Transform poolRoot;
    private readonly System.Func<float> getSfxVolume;

    private readonly List<AudioSource> pooledSources = new List<AudioSource>();
    private readonly Dictionary<int, ActiveSound> activeSounds = new Dictionary<int, ActiveSound>();
    private readonly Dictionary<string, int> lastClipIndexByKey = new Dictionary<string, int>();

    private int nextSoundId = 1;

    public SoundSFXPlayer(
        MonoBehaviour coroutineOwner,
        SoundLibrarySO soundLibrary,
        Transform poolRoot,
        int initialPoolSize,
        System.Func<float> getSfxVolume)
    {
        this.coroutineOwner = coroutineOwner;
        this.soundLibrary = soundLibrary;
        this.poolRoot = poolRoot;
        this.getSfxVolume = getSfxVolume;

        BuildPool(initialPoolSize);
    }

    public void LateUpdate()
    {
        if (activeSounds.Count == 0) return;

        List<int> stopSoundIds = null;

        foreach (KeyValuePair<int, ActiveSound> pair in activeSounds)
        {
            ActiveSound sound = pair.Value;
            if (sound == null || sound.source == null) continue;

            if (sound.isAttached && sound.followTarget == null)
            {
                if (stopSoundIds == null)
                    stopSoundIds = new List<int>();

                stopSoundIds.Add(pair.Key);
                continue;
            }

            if (sound.followTarget == null) continue;

            sound.source.transform.position = sound.followTarget.position;
        }

        if (stopSoundIds == null) return;

        for (int i = 0; i < stopSoundIds.Count; i++)
        {
            ReleaseSound(stopSoundIds[i]);
        }
    }

    public void RefreshVolumes()
    {
        foreach (KeyValuePair<int, ActiveSound> pair in activeSounds)
            ApplySoundVolume(pair.Value);
    }

    public SoundHandle Play(string key)
    {
        return Play(key, null);
    }

    public SoundHandle Play(string key, SFXPlayOptions options)
    {
        SFXPlayOptions resolvedOptions = GetResolved2DOptions(options);
        return PlayInternal(key, resolvedOptions, Vector3.zero, null);
    }

    public SoundHandle PlayAt(string key, Vector3 position)
    {
        return PlayAt(key, position, null);
    }

    public SoundHandle PlayAt(string key, Vector3 position, SFXPlayOptions options)
    {
        SFXPlayOptions resolvedOptions = GetResolved3DOptions(options);
        return PlayInternal(key, resolvedOptions, position, null);
    }

    public SoundHandle PlayAttached(string key, Transform target)
    {
        return PlayAttached(key, target, null);
    }

    public SoundHandle PlayAttached(string key, Transform target, SFXPlayOptions options)
    {
        if (target == null)
        {
            Debug.LogWarning("[SoundSFXPlayer] PlayAttached failed: target is null.");
            return null;
        }

        SFXPlayOptions resolvedOptions = GetResolved3DOptions(options);
        return PlayInternal(key, resolvedOptions, target.position, target);
    }

    public void StopSound(int soundId, float fadeOutDuration)
    {
        if (activeSounds.TryGetValue(soundId, out ActiveSound sound) == false || sound == null) return;

        if (fadeOutDuration <= 0f)
        {
            ReleaseSound(sound.id);
            return;
        }

        if (sound.fadeRoutine != null)
        {
            coroutineOwner.StopCoroutine(sound.fadeRoutine);
            sound.fadeRoutine = null;
        }

        sound.fadeRoutine = coroutineOwner.StartCoroutine(FadeOutAndStopRoutine(sound.id, fadeOutDuration));
    }

    public void SetSoundVolume(int soundId, float volume01)
    {
        if (activeSounds.TryGetValue(soundId, out ActiveSound sound) == false || sound == null) return;

        sound.baseVolume = Mathf.Clamp01(volume01);
        ApplySoundVolume(sound);
    }

    public void SetSoundPosition(int soundId, Vector3 position)
    {
        if (activeSounds.TryGetValue(soundId, out ActiveSound sound) == false || sound == null || sound.source == null) return;

        sound.isAttached = false;
        sound.followTarget = null;
        sound.source.transform.position = position;
    }

    public void SetSoundFollowTarget(int soundId, Transform target)
    {
        if (activeSounds.TryGetValue(soundId, out ActiveSound sound) == false || sound == null || sound.source == null) return;

        sound.isAttached = target != null;
        sound.followTarget = target;
        if (target == null) return;

        sound.source.transform.position = target.position;
    }

    public void StopAllSounds()
    {
        if (activeSounds.Count == 0)
        {
            return;
        }

        int[] soundIds = new int[activeSounds.Count];
        activeSounds.Keys.CopyTo(soundIds, 0);

        for (int i = 0; i < soundIds.Length; i++)
        {
            ReleaseSound(soundIds[i]);
        }
    }

    private void BuildPool(int initialPoolSize)
    {
        for (int i = 0; i < initialPoolSize; i++)
            pooledSources.Add(CreateSource(i));
    }

    private AudioSource CreateSource(int index)
    {
        GameObject go = new GameObject($"SFX_Source_{index:D2}");
        go.transform.SetParent(poolRoot, false);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.volume = 0f;
        source.pitch = 1f;
        source.spatialBlend = 0f;

        return source;
    }

    private AudioSource GetFreeSource()
    {
        for (int i = 0; i < pooledSources.Count; i++)
        {
            AudioSource source = pooledSources[i];
            if (IsSourceInUse(source)) continue;

            ResetSource(source);
            return source;
        }

        AudioSource newSource = CreateSource(pooledSources.Count);
        pooledSources.Add(newSource);
        ResetSource(newSource);
        return newSource;
    }

    private bool IsSourceInUse(AudioSource source)
    {
        foreach (KeyValuePair<int, ActiveSound> pair in activeSounds)
        {
            ActiveSound sound = pair.Value;
            if (sound != null && sound.source == source)
                return true;
        }

        return false;
    }

    private void ResetSource(AudioSource source)
    {
        if (source == null) return;

        source.Stop();
        source.clip = null;
        source.loop = false;
        source.volume = 0f;
        source.pitch = 1f;
        source.spatialBlend = 0f;
        source.minDistance = 1f;
        source.maxDistance = 15f;
        source.transform.SetParent(poolRoot, false);
        source.transform.localPosition = Vector3.zero;
        source.transform.localRotation = Quaternion.identity;
    }

    private SoundHandle PlayInternal(string key, SFXPlayOptions options, Vector3 position, Transform followTarget)
    {
        if (TryGetClip(key, out AudioClip clip) == false) return null;

        AudioSource source = GetFreeSource();
        ConfigureSource(source, clip, options, position, followTarget);

        ActiveSound sound = new ActiveSound
        {
            id = nextSoundId++,
            key = key,
            source = source,
            followTarget = followTarget,
            loop = options.loop,
            isAttached = followTarget != null,
            useUnscaledTime = options.useUnscaledTime,
            baseVolume = Mathf.Clamp01(options.volume),
            runtimeGain = options.fadeInDuration > 0f ? 0f : 1f,
            lifecycleRoutine = null,
            fadeRoutine = null
        };

        activeSounds.Add(sound.id, sound);

        ApplySoundVolume(sound);
        source.Play();

        if (options.fadeInDuration > 0f)
            sound.fadeRoutine = coroutineOwner.StartCoroutine(FadeInRoutine(sound.id, options.fadeInDuration));

        if (options.loop == false)
            sound.lifecycleRoutine = coroutineOwner.StartCoroutine(OneShotLifecycleRoutine(sound.id));

        return new SoundHandle(this, sound.id);
    }

    private void ConfigureSource(AudioSource source, AudioClip clip, SFXPlayOptions options, Vector3 position, Transform followTarget)
    {
        ResetSource(source);

        source.clip = clip;
        source.loop = options.loop;
        source.pitch = GetResolvedPitch(options);
        source.spatialBlend = options.is3D ? Mathf.Clamp01(options.spatialBlend) : 0f;
        source.minDistance = Mathf.Max(0.01f, options.minDistance);
        source.maxDistance = Mathf.Max(source.minDistance, options.maxDistance);
        source.transform.position = followTarget != null ? followTarget.position : position;
    }

    private bool TryGetClip(string key, out AudioClip selectedClip)
    {
        selectedClip = null;

        if (soundLibrary == null)
        {
            Debug.LogWarning("[SoundSFXPlayer] SoundLibrarySO is not assigned.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[SoundSFXPlayer] Empty sound key.");
            return false;
        }

        var entries = soundLibrary.Entries;
        if (entries == null)
            return false;

        if (entries.TryGetValue(key, out SoundEntry entry) == false || entry == null)
        {
            Debug.LogWarning($"[SoundSFXPlayer] Sound key not found: {key}");
            return false;
        }

        if (entry.Category != SoundCategory.SFX)
        {
            Debug.LogWarning($"[SoundSFXPlayer] Sound key category mismatch: {key}");
            return false;
        }

        selectedClip = SelectRandomClip(key, entry);
        if (selectedClip == null)
        {
            Debug.LogWarning($"[SoundSFXPlayer] No valid clip found for key: {key}");
            return false;
        }

        return true;
    }

    private AudioClip SelectRandomClip(string key, SoundEntry entry)
    {
        AudioClip[] clips = entry.Clips;
        if (clips == null || clips.Length == 0) return null;

        lastClipIndexByKey.TryGetValue(key, out int lastIndex);

        int validCount = 0;
        int validExceptLastCount = 0;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null) continue;

            validCount++;

            if (i != lastIndex)
                validExceptLastCount++;
        }

        if (validCount == 0) return null;

        int chosenIndex = -1;

        if (validExceptLastCount > 0)
        {
            int pick = Random.Range(0, validExceptLastCount);

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null || i == lastIndex) continue;

                if (pick == 0)
                {
                    chosenIndex = i;
                    break;
                }

                pick--;
            }
        }
        else
        {
            int pick = Random.Range(0, validCount);

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null) continue;

                if (pick == 0)
                {
                    chosenIndex = i;
                    break;
                }

                pick--;
            }
        }

        if (chosenIndex < 0) return null;

        lastClipIndexByKey[key] = chosenIndex;
        return clips[chosenIndex];
    }

    private SFXPlayOptions GetResolved2DOptions(SFXPlayOptions options)
    {
        SFXPlayOptions resolved = options != null ? options.Clone() : SFXPlayOptions.Default2D();
        resolved.is3D = false;
        resolved.spatialBlend = 0f;
        return resolved;
    }

    private SFXPlayOptions GetResolved3DOptions(SFXPlayOptions options)
    {
        SFXPlayOptions resolved = options != null ? options.Clone() : SFXPlayOptions.Default3D();
        resolved.is3D = true;
        resolved.spatialBlend = Mathf.Clamp01(resolved.spatialBlend <= 0f ? 1f : resolved.spatialBlend);
        resolved.minDistance = Mathf.Max(0.01f, resolved.minDistance);
        resolved.maxDistance = Mathf.Max(resolved.minDistance, resolved.maxDistance);
        return resolved;
    }

    private float GetResolvedPitch(SFXPlayOptions options)
    {
        float pitch = options.pitch;

        if (options.pitchRandomRange != Vector2.zero)
        {
            float minPitch = Mathf.Min(options.pitchRandomRange.x, options.pitchRandomRange.y);
            float maxPitch = Mathf.Max(options.pitchRandomRange.x, options.pitchRandomRange.y);
            pitch += Random.Range(minPitch, maxPitch);
        }

        return Mathf.Max(0.01f, pitch);
    }

    private IEnumerator OneShotLifecycleRoutine(int soundId)
    {
        while (activeSounds.TryGetValue(soundId, out ActiveSound sound))
        {
            if (sound == null || sound.source == null) break;
            if (sound.source.isPlaying == false) break;

            yield return null;
        }

        ReleaseSound(soundId);
    }

    private IEnumerator FadeInRoutine(int soundId, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (activeSounds.TryGetValue(soundId, out ActiveSound sound) == false || sound == null)
                yield break;

            if (AudioListener.pause)
            {
                yield return null;
                continue;
            }

            elapsed += GetDeltaTime(sound.useUnscaledTime);
            sound.runtimeGain = Mathf.Clamp01(elapsed / duration);
            ApplySoundVolume(sound);
            yield return null;
        }

        if (activeSounds.TryGetValue(soundId, out ActiveSound finalSound) == false || finalSound == null)
            yield break;

        finalSound.runtimeGain = 1f;
        ApplySoundVolume(finalSound);
        finalSound.fadeRoutine = null;
    }

    private IEnumerator FadeOutAndStopRoutine(int soundId, float duration)
    {
        if (activeSounds.TryGetValue(soundId, out ActiveSound sound) == false || sound == null)
            yield break;

        float startGain = sound.runtimeGain;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (activeSounds.TryGetValue(soundId, out ActiveSound currentSound) == false || currentSound == null)
                yield break;

            if (AudioListener.pause)
            {
                yield return null;
                continue;
            }

            elapsed += GetDeltaTime(currentSound.useUnscaledTime);
            float t = Mathf.Clamp01(elapsed / duration);
            currentSound.runtimeGain = Mathf.Lerp(startGain, 0f, t);
            ApplySoundVolume(currentSound);
            yield return null;
        }

        ReleaseSound(soundId);
    }

    private void ReleaseSound(int soundId)
    {
        if (activeSounds.TryGetValue(soundId, out ActiveSound sound) == false || sound == null) return;

        if (sound.lifecycleRoutine != null)
        {
            coroutineOwner.StopCoroutine(sound.lifecycleRoutine);
            sound.lifecycleRoutine = null;
        }

        if (sound.fadeRoutine != null)
        {
            coroutineOwner.StopCoroutine(sound.fadeRoutine);
            sound.fadeRoutine = null;
        }

        if (sound.source != null)
            ResetSource(sound.source);

        activeSounds.Remove(soundId);
    }

    private void ApplySoundVolume(ActiveSound sound)
    {
        if (sound == null || sound.source == null) return;

        sound.source.volume = getSfxVolume() * Mathf.Clamp01(sound.baseVolume) * Mathf.Clamp01(sound.runtimeGain);
    }

    private float GetDeltaTime(bool useUnscaledTime)
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
