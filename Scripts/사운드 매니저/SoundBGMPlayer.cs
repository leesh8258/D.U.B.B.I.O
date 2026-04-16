using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundBGMPlayer
{
    private readonly MonoBehaviour coroutineOwner;
    private readonly SoundLibrarySO soundLibrary;
    private readonly AudioSource bgmSource;
    private readonly System.Func<float> getMusicVolume;

    private readonly Dictionary<string, int> lastClipIndexByKey = new Dictionary<string, int>();

    private string currentBgmKey;
    private float bgmRuntimeGain = 1f;
    private float bgmOptionVolume = 1f;
    private bool bgmUseUnscaledTime;
    private Coroutine bgmRoutine;

    public SoundBGMPlayer(
        MonoBehaviour coroutineOwner,
        SoundLibrarySO soundLibrary,
        AudioSource bgmSource,
        System.Func<float> getMusicVolume)
    {
        this.coroutineOwner = coroutineOwner;
        this.soundLibrary = soundLibrary;
        this.bgmSource = bgmSource;
        this.getMusicVolume = getMusicVolume;
    }

    public void Play(string key)
    {
        Play(key, null);
    }

    public void Play(string key, BGMPlayOptions options)
    {
        if (TryGetClip(key, out AudioClip clip) == false) return;

        BGMPlayOptions resolvedOptions = options != null ? options.Clone() : BGMPlayOptions.Default();

        if (bgmSource != null && bgmSource.isPlaying && currentBgmKey == key && bgmSource.clip == clip)
            return;

        if (bgmRoutine != null)
        {
            coroutineOwner.StopCoroutine(bgmRoutine);
            bgmRoutine = null;
        }

        bgmRoutine = coroutineOwner.StartCoroutine(PlayRoutine(key, clip, resolvedOptions));
    }

    public void Stop()
    {
        Stop(0f);
    }

    public void Stop(float fadeOutDuration)
    {
        if (bgmSource == null || bgmSource.clip == null) return;

        if (bgmRoutine != null)
        {
            coroutineOwner.StopCoroutine(bgmRoutine);
            bgmRoutine = null;
        }

        if (fadeOutDuration <= 0f)
        {
            StopImmediate();
            return;
        }

        bgmRoutine = coroutineOwner.StartCoroutine(StopRoutine(fadeOutDuration, bgmUseUnscaledTime));
    }

    public void RefreshVolume()
    {
        ApplyVolume();
    }

    private bool TryGetClip(string key, out AudioClip selectedClip)
    {
        selectedClip = null;

        if (soundLibrary == null)
        {
            Debug.LogWarning("[SoundBGMPlayer] SoundLibrarySO is not assigned.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[SoundBGMPlayer] Empty sound key.");
            return false;
        }

        var entries = soundLibrary.Entries;
        if (entries == null)
            return false;

        if (entries.TryGetValue(key, out SoundEntry entry) == false || entry == null)
        {
            Debug.LogWarning($"[SoundBGMPlayer] Sound key not found: {key}");
            return false;
        }

        if (entry.Category != SoundCategory.BGM)
        {
            Debug.LogWarning($"[SoundBGMPlayer] Sound key category mismatch: {key}");
            return false;
        }

        selectedClip = SelectRandomClip(key, entry);
        if (selectedClip == null)
        {
            Debug.LogWarning($"[SoundBGMPlayer] No valid clip found for key: {key}");
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

    private IEnumerator PlayRoutine(string key, AudioClip clip, BGMPlayOptions options)
    {
        float switchFadeOutDuration = options.crossFadeDuration > 0f ? options.crossFadeDuration : options.fadeOutDuration;

        if (bgmSource.isPlaying && bgmSource.clip != null)
        {
            if (switchFadeOutDuration > 0f)
                yield return FadeOutRoutine(switchFadeOutDuration, options.useUnscaledTime);

            bgmSource.Stop();
        }

        bgmSource.clip = clip;
        bgmSource.loop = options.loop;
        bgmSource.pitch = Mathf.Max(0.01f, options.pitch);

        currentBgmKey = key;
        bgmOptionVolume = Mathf.Clamp01(options.volume);
        bgmUseUnscaledTime = options.useUnscaledTime;
        bgmRuntimeGain = options.fadeInDuration > 0f ? 0f : 1f;

        ApplyVolume();
        bgmSource.Play();

        if (options.fadeInDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < options.fadeInDuration)
            {
                if (AudioListener.pause)
                {
                    yield return null;
                    continue;
                }

                elapsed += GetDeltaTime(options.useUnscaledTime);
                bgmRuntimeGain = Mathf.Clamp01(elapsed / options.fadeInDuration);
                ApplyVolume();
                yield return null;
            }

            bgmRuntimeGain = 1f;
            ApplyVolume();
        }

        bgmRoutine = null;
    }

    private IEnumerator StopRoutine(float fadeOutDuration, bool useUnscaledTime)
    {
        yield return FadeOutRoutine(fadeOutDuration, useUnscaledTime);
        StopImmediate();
        bgmRoutine = null;
    }

    private IEnumerator FadeOutRoutine(float duration, bool useUnscaledTime)
    {
        float start = bgmRuntimeGain;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (AudioListener.pause)
            {
                yield return null;
                continue;
            }

            elapsed += GetDeltaTime(useUnscaledTime);
            float t = Mathf.Clamp01(elapsed / duration);
            bgmRuntimeGain = Mathf.Lerp(start, 0f, t);
            ApplyVolume();
            yield return null;
        }

        bgmRuntimeGain = 0f;
        ApplyVolume();
    }

    private void StopImmediate()
    {
        if (bgmSource == null) return;

        bgmSource.Stop();
        bgmSource.clip = null;
        currentBgmKey = null;
        bgmRuntimeGain = 1f;
        bgmOptionVolume = 1f;
        bgmUseUnscaledTime = false;
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        if (bgmSource == null) return;

        bgmSource.volume = getMusicVolume() * Mathf.Clamp01(bgmOptionVolume) * Mathf.Clamp01(bgmRuntimeGain);
    }

    private float GetDeltaTime(bool useUnscaledTime)
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
