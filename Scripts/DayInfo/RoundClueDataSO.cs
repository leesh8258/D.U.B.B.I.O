using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RoundClueDataSO", menuName = "Clues/RoundClueDataSO")]
public class RoundClueDataSO : ScriptableObject
{
    [Header("기본 단서")]
    public BaseStrategy[] baseStrategies = Array.Empty<BaseStrategy>();

    [Header("특정 단서")]
    public ForcedStrategy[] forcedSlots = Array.Empty<ForcedStrategy>();

    [Serializable]
    public struct BaseStrategy
    {
        public ClueType type;
        [Min(0)] public int weight;
    }

    [Serializable]
    public struct ForcedStrategy
    {
        [Min(0)] public int triggerEmitIndex;
        public ClueType[] allowedTypes;
    }

    public BaseStrategy[] GetBaseStrategies()
    {
        return baseStrategies;
    }

    public ForcedStrategy[] GetForcedSlots()
    {
        return forcedSlots;
    }
}
