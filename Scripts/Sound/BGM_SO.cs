using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BGM_SO", menuName = "Scriptable Objects/BGM_SO")]
public class BGM_SO : ScriptableObject
{
    public List<BGMEntry> entries = new List<BGMEntry>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 키 중복 경고(런타임 Dictionary 빌드 시 덮어쓰기 방지용)
        HashSet<BGMType> seen = new HashSet<BGMType>();
        for (int i = 0; i < entries.Count; i++)
        {
            if (seen.Contains(entries[i].type))
            {
                Debug.LogWarning($"[BgmLibrary] Duplicate key: {entries[i].type} at index {i}");
            }
            else
            {
                seen.Add(entries[i].type);
            }
        }
    }
#endif
}
