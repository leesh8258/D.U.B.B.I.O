using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ClueDatasetIndexSO", menuName = "Clue/Dataset Index")]
public class ClueDatasetIndexSO : ScriptableObject
{
    [Serializable]
    public class DatasetEntry
    {
        public GameLevel level;
        public int day;

        [Header("10 suspects (행)")]
        public SuspectItem[] suspects = Array.Empty<SuspectItem>();

        [Header("12 keywords (열)")]
        public Keyword[] keywords = Array.Empty<Keyword>();
    }

    [SerializeField] private DatasetEntry[] entries = Array.Empty<DatasetEntry>();

    public bool TryGet(GameLevel level, int day, out SuspectItem[] suspects, out Keyword[] keywords)
    {
        suspects = Array.Empty<SuspectItem>();
        keywords = Array.Empty<Keyword>();

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e == null) continue;
            if (e.level == level && e.day == day)
            {
                suspects = e.suspects ?? Array.Empty<SuspectItem>();
                keywords = e.keywords ?? Array.Empty<Keyword>();
                return true;
            }
        }
        return false;
    }
}
