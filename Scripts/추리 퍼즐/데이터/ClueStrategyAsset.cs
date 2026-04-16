using System.Collections.Generic;
using UnityEngine;

public abstract class ClueStrategyAsset : ScriptableObject, IClueBatchStrategy
{
    [Header("Identity")]
    [SerializeField] private string strategyKey;

    [Header("Selection")]
    [SerializeField, Min(1)] private int defaultSlotCost = 1;
    [SerializeField] private bool canAppearInForcedSlot = true;

    [Header("Lie")]
    [SerializeField] private bool canBeUsedByLieStrategy = false;

    public string StrategyKey => string.IsNullOrWhiteSpace(strategyKey) ? name : strategyKey;
    public bool CanAppearInForcedSlot => canAppearInForcedSlot;
    public bool CanBeUsedByLieStrategy => canBeUsedByLieStrategy;

    public virtual int GetRequiredSlotCost(ClueBuildRequest request)
    {
        return Mathf.Max(1, defaultSlotCost);
    }

    public abstract void BuildRoundCandidates(RoundClueContext roundClueContext, List<Clue> target);

    public virtual void BuildRoundLieCandidates(RoundClueContext roundClueContext, List<Clue> target) { }

    public abstract bool HasAvailableCandidates(ClueStrategyRuntimeState state, ClueBuildRequest request);

    public abstract bool TryBuildClues(ClueStrategyRuntimeState state, ClueBuildRequest request, out ClueBuildResult result);

    protected Clue CreateClue(string templateKey, ClueTruthState truthState, params ClueArg[] args)
    {
        return new Clue
        {
            strategyKey = StrategyKey,
            templateTableName = ClueLanguageTables.Patterns,
            templateKey = templateKey,
            truthState = truthState,
            ageState = ClueAgeState.New,
            isLie = false,
            isMeta = false,
            args = args != null ? new List<ClueArg>(args) : new List<ClueArg>(0)
        };
    }

    protected Clue CreateLieClue(string templateKey, params ClueArg[] args)
    {
        return new Clue
        {
            strategyKey = StrategyKey,
            templateTableName = ClueLanguageTables.Patterns,
            templateKey = templateKey,
            truthState = ClueTruthState.Lie,
            ageState = ClueAgeState.New,
            isLie = true,
            isMeta = false,
            args = args != null ? new List<ClueArg>(args) : new List<ClueArg>(0)
        };
    }

    protected Clue CreateMetaClue(string tableName, string templateKey, params ClueArg[] args)
    {
        return new Clue
        {
            strategyKey = StrategyKey,
            templateTableName = tableName,
            templateKey = templateKey,
            truthState = ClueTruthState.Meta,
            ageState = ClueAgeState.New,
            isLie = false,
            isMeta = true,
            args = args != null ? new List<ClueArg>(args) : new List<ClueArg>(0)
        };
    }

    protected virtual void OnValidate()
    {
        if (defaultSlotCost < 1)
        {
            defaultSlotCost = 1;
        }

        if (string.IsNullOrWhiteSpace(strategyKey)) return;

        string trimmed = strategyKey.Trim();
        if (!string.Equals(strategyKey, trimmed, System.StringComparison.Ordinal))
        {
            strategyKey = trimmed;
        }
    }
}
