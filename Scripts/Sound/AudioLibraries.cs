using UnityEngine;

#region Enums
public enum BGMType
{
    MainMenu,
    Game
}

public enum SFXType
{
    SFX_Button,
    SFX_DES_EFFECT,
    SFX_MonitorTurn,
    SFX_Trasition_TAB,
}

public enum PlayStartMode
{
    Immediate,
    FadeIn
}

public enum StopMode
{
    Immediate,
    FadeOut
}
#endregion

#region Entries
[System.Serializable]
public struct BGMEntry
{
    public BGMType type;
    public AudioClip clip;
    public bool loop;

    [Header("In-Game Reactive")]
    public bool reactiveByTimer;

    [Header("Per-Clip Playback Behavior")]
    public PlayStartMode startMode;
    public float fadeInDuration;

    public StopMode stopMode;
    public float fadeOutDuration;
}

[System.Serializable]
public struct SFXEntry
{
    public SFXType type;
    public AudioClip clip;

    [Header("Per-Clip Playback Behavior")]
    public PlayStartMode startMode;
    public float fadeInDuration;

    public StopMode stopMode;
    public float fadeOutDuration;
}
#endregion
