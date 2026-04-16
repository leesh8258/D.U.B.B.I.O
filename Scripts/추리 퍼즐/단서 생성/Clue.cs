using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Clue
{
    [TextArea] public string text;
    public int clueId;

    public string strategyKey;
    public string templateTableName;
    public string templateKey;
    public List<ClueArg> args = new List<ClueArg>(4);

    public ClueAgeState ageState = ClueAgeState.New;
    public ClueTruthState truthState = ClueTruthState.Unknown;
    public bool isMeta;
    public bool isLie;
    public int batchIndex;
    public int displayOrder;
}
