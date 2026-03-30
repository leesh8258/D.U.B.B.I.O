using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RoundClueDataIndexSO", menuName = "Clues/RoundClueDataIndexSO")]
public class RoundClueDataIndexSO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public GameLevel level;

        [Range(1, 7)] public int day;
        public RoundClueDataSO config;
    }

    [Header("Mappings")]
    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    public bool TryGet(StageInfo stage, out RoundClueDataSO config)
    {
        config = null;

        Entry[] list = entries;

        // 완전 level, day 가 완전 맞는 경우인가?
        for (int i = 0; i < list.Length; i++)
        {
            Entry entry = list[i];
            if (entry.config == null) continue;

            if (entry.level == stage.level && entry.day == stage.day)
            {
                config = entry.config;
                return true;
            }
        }
        return false;
    }
}
