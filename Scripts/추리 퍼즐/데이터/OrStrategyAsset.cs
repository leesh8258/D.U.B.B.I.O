using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OrStrategyAsset", menuName = "Clues/Strategy/Or")]
public class OrStrategyAsset : SingleClueStrategyAsset
{
    private const string PatternKey = "OR";
    private const string TokenCategory = "CAT";
    private const string TokenKeywordA = "KW_A";
    private const string TokenKeywordB = "KW_B";

    public override void BuildRoundCandidates(RoundClueContext roundClueContext, List<Clue> target)
    {
        if (roundClueContext == null || target == null) return;

        Category[] categories = roundClueContext.ActiveCategories;

        List<Keyword> trueKeywords = new List<Keyword>(8);
        List<Keyword> falseKeywords = new List<Keyword>(8);

        for (int i = 0; i < categories.Length; i++)
        {
            Category category = categories[i];
            Keyword[] keywords = roundClueContext.GetKeywords(category);

            trueKeywords.Clear();
            falseKeywords.Clear();

            for (int j = 0; j < keywords.Length; j++)
            {
                Keyword keyword = keywords[j];
                if (keyword == null) continue;

                if (roundClueContext.IsTrue(category, keyword))
                {
                    trueKeywords.Add(keyword);
                }
                else
                {
                    falseKeywords.Add(keyword);
                }
            }

            if (trueKeywords.Count == 0 || falseKeywords.Count == 0) continue;

            for (int t = 0; t < trueKeywords.Count; t++)
            {
                Keyword trueKeyword = trueKeywords[t];

                for (int f = 0; f < falseKeywords.Count; f++)
                {
                    Keyword falseKeyword = falseKeywords[f];

                    ResolvePairOrder(trueKeyword, falseKeyword, out Keyword keywordA, out Keyword keywordB);

                    target.Add(CreateClue(PatternKey, ClueTruthState.Truth, ClueArgFactory.Category(TokenCategory, category), ClueArgFactory.Keyword(TokenKeywordA, keywordA), ClueArgFactory.Keyword(TokenKeywordB, keywordB)));
                }
            }
        }
    }

    public override void BuildRoundLieCandidates(RoundClueContext roundClueContext, List<Clue> target)
    {
        if (!CanBeUsedByLieStrategy) return;

        if (roundClueContext == null || target == null) return;

        Category[] categories = roundClueContext.ActiveCategories;

        List<Keyword> trueKeywords = new List<Keyword>(8);
        List<Keyword> falseKeywords = new List<Keyword>(8);

        for (int i = 0; i < categories.Length; i++)
        {
            Category category = categories[i];
            Keyword[] keywords = roundClueContext.GetKeywords(category);

            trueKeywords.Clear();
            falseKeywords.Clear();

            for (int j = 0; j < keywords.Length; j++)
            {
                Keyword keyword = keywords[j];
                if (keyword == null) continue;

                if (roundClueContext.IsTrue(category, keyword))
                {
                    trueKeywords.Add(keyword);
                }
                else
                {
                    falseKeywords.Add(keyword);
                }
            }

            AddLiePairsFromSameTruthState(target, category, falseKeywords);
            AddLiePairsFromSameTruthState(target, category, trueKeywords);
        }
    }

    private void AddLiePairsFromSameTruthState(List<Clue> target, Category category, List<Keyword> source)
    {
        if (target == null || source == null || source.Count < 2) return;

        for (int a = 0; a < source.Count; a++)
        {
            for (int b = a + 1; b < source.Count; b++)
            {
                ResolvePairOrder(source[a], source[b], out Keyword keywordA, out Keyword keywordB);

                target.Add(CreateLieClue(PatternKey, ClueArgFactory.Category(TokenCategory, category), ClueArgFactory.Keyword(TokenKeywordA, keywordA), ClueArgFactory.Keyword(TokenKeywordB, keywordB)));
            }
        }
    }

    private void ResolvePairOrder(Keyword keywordA, Keyword keywordB, out Keyword orderedA, out Keyword orderedB)
    {
        if (Random.value < 0.5f)
        {
            orderedA = keywordA;
            orderedB = keywordB;
        }
        else
        {
            orderedA = keywordB;
            orderedB = keywordA;
        }
    }
}
