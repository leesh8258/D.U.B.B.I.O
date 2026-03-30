using UnityEngine;

public enum Category
{
    Region,
    Weapon
}

[CreateAssetMenu(fileName = "CategoryStyle", menuName = "Scriptable Objects/Clue/Category Style")]
public class CategoryStyleSO : ScriptableObject, ILocalizationData
{
    [Header("Category")]
    public Category category;

    [Header("Localization ID")]
    public string displayNameID;

    [Header("Color")]
    public Color frameColor = Color.white;

    [Header("Color Hex")]
    public string descriptionHex = "#FFFFFF";

    public TableName TableName { get { return TableName.Category; } }
    public string LocalizationKey { get { return displayNameID; } }
}
