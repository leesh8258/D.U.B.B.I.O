using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("아이템 리스트")]
    [SerializeField] private List<ItemSO> allItems = new List<ItemSO>();

    private int itemFlag;

    public int ItemFlag => itemFlag;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }

    // Load 호출 시 사용
    public void SetItemFlag(int newFlag)
    {
        itemFlag = newFlag;
    }

    
    public bool AcquireItem(ItemType type)
    {
        int mask = 1 << (int)type;

        if ((itemFlag & mask) != 0) return false;
        itemFlag |= mask;
        return true;
    }

    public bool RemoveItem(ItemType type)
    {
        int mask = 1 << (int)type;

        if ((itemFlag & mask) == 0) return false;
        itemFlag &= ~mask;
        return true;
    }

    public bool HasItem(ItemType type)
    {
        int mask = 1 << (int)type;
        return (itemFlag & mask) != 0;
    }

    public List<ItemSO> GetHavingItems()
    {
        List<ItemSO> result = new List<ItemSO>();
        if (allItems == null) return result;

        for (int i = 0; i < allItems.Count; i++)
        {
            ItemSO item = allItems[i];
            if (item == null) continue;

            if (HasItem(item.type))
                result.Add(item);
        }

        return result;
    }

    public string[] GetHavingItemsPreview(int flag)
    {
        List<string> result = new List<string>();
        if (allItems == null) return result.ToArray();

        for (int i = 0; i < allItems.Count; i++)
        {
            ItemSO item = allItems[i];
            if (item == null) continue;

            int mask = 1 << (int)item.type;
            if ((flag & mask) != 0)
            {
                result.Add(item.type.ToString());
            }
        }

        return result.ToArray();
    }
}
