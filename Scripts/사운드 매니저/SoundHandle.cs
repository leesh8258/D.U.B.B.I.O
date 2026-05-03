using UnityEngine;

public class SoundHandle
{
    private readonly SoundSFXPlayer soundPlayer;
    private readonly int soundId;

    public SoundHandle(SoundSFXPlayer soundPlayer, int soundId)
    {
        this.soundPlayer = soundPlayer;
        this.soundId = soundId;
    }

    public void Stop()
    {
        if (soundPlayer == null) return;
        soundPlayer.StopSound(soundId, 0f);
    }

    public void Stop(float fadeOutDuration)
    {
        if (soundPlayer == null) return;
        soundPlayer.StopSound(soundId, fadeOutDuration);
    }

    public void SetVolume(float volume)
    {
        if (soundPlayer == null) return;
        soundPlayer.SetSoundVolume(soundId, volume);
    }

    public void SetPosition(Vector3 position)
    {
        if (soundPlayer == null) return;
        soundPlayer.SetSoundPosition(soundId, position);
    }

    public void SetFollowTarget(Transform target)
    {
        if (soundPlayer == null) return;
        soundPlayer.SetSoundFollowTarget(soundId, target);
    }
}
