using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SFX_SO", menuName = "Scriptable Objects/SFX_SO")]
public class SFX_SO : ScriptableObject
{
    public List<SFXEntry> entries = new List<SFXEntry>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        HashSet<SFXType> seen = new HashSet<SFXType>();
        for (int i = 0; i < entries.Count; i++)
        {
            if (seen.Contains(entries[i].type))
            {
                Debug.LogWarning($"[SfxLibrary] Duplicate key: {entries[i].type} at index {i}");
            }
            else
            {
                seen.Add(entries[i].type);
            }
        }
    }
#endif
}
