using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttrTStrategyAsset", menuName = "Clues/Strategy/AttrT")]
public class AttrTStrategyAsset : SingleClueStrategyAsset
{
    private const string PatternKey = "ATTRT";
    private const string TokenCategory = "CAT";
    private const string TokenKeyword = "KW";

    public override void BuildRoundCandidates(RoundClueContext roundClueContext, List<Clue> target)
    {
        if (roundClueContext == null || target == null) return;

        Category[] categories = roundClueContext.ActiveCategories;
        for (int i = 0; i < categories.Length; i++)
        {
            Category category = categories[i];
            Keyword[] keywords = roundClueContext.GetKeywords(category);

            for (int j = 0; j < keywords.Length; j++)
            {
                Keyword keyword = keywords[j];
                if (keyword == null) continue;
                if (!roundClueContext.IsTrue(category, keyword)) continue;

                target.Add(CreateClue(PatternKey, ClueTruthState.Truth, ClueArgFactory.Category(TokenCategory, category), ClueArgFactory.Keyword(TokenKeyword, keyword)));
            }
        }
    }

    public override void BuildRoundLieCandidates(RoundClueContext roundClueContext, List<Clue> target)
    {
        if (!CanBeUsedByLieStrategy) return;
        if (roundClueContext == null || target == null) return;

        Category[] categories = roundClueContext.ActiveCategories;
        for (int i = 0; i < categories.Length; i++)
        {
            Category category = categories[i];
            Keyword[] keywords = roundClueContext.GetKeywords(category);

            for (int j = 0; j < keywords.Length; j++)
            {
                Keyword keyword = keywords[j];
                if (keyword == null) continue;
                if (roundClueContext.IsTrue(category, keyword)) continue;

                target.Add(CreateLieClue(PatternKey, ClueArgFactory.Category(TokenCategory, category), ClueArgFactory.Keyword(TokenKeyword, keyword)));
            }
        }
    }
}
