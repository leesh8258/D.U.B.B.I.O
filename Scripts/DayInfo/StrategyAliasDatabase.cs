using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StrategyAliasDatabase", menuName = "Clues/Strategy Alias Database")]
public class StrategyAliasDatabase : ScriptableObject
{
    public StrategyAliasEntry[] entries = Array.Empty<StrategyAliasEntry>();

    public bool TryGetAliases(ClueType type, out string[] aliases)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                StrategyAliasEntry entry = entries[i];
                if (entry != null && entry.type == type)
                {
                    aliases = entry.aliases;
                    return true;
                }
            }
        }

        aliases = Array.Empty<string>();
        return false;
    }
}

[Serializable]
public class StrategyAliasEntry
{
    public ClueType type;

    [Tooltip("검색 매칭에 사용할 별칭들")]
    public string[] aliases = Array.Empty<string>();
}
