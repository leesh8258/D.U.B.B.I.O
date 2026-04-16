using System;
using System.Collections.Generic;

public class RoundClueContext
{
    private readonly Category[] activeCategories;
    private readonly Dictionary<Category, Keyword[]> keywordsByCategory;
    private readonly Dictionary<Category, SuspectItem> answerByCategory;
    private readonly Dictionary<Category, Keyword[]> trueKeywordsByCategory;
    private readonly Dictionary<Category, HashSet<string>> trueKeywordIdSetByCategory;

    public RoundClueContext(
        Category[] activeCategories,
        Dictionary<Category, Keyword[]> keywordsByCategory,
        Dictionary<Category, SuspectItem> answerByCategory,
        Dictionary<Category, Keyword[]> trueKeywordsByCategory,
        Dictionary<Category, HashSet<string>> trueKeywordIdSetByCategory)
    {
        this.activeCategories = activeCategories ?? Array.Empty<Category>();
        this.keywordsByCategory = keywordsByCategory ?? new Dictionary<Category, Keyword[]>();
        this.answerByCategory = answerByCategory ?? new Dictionary<Category, SuspectItem>();
        this.trueKeywordsByCategory = trueKeywordsByCategory ?? new Dictionary<Category, Keyword[]>();
        this.trueKeywordIdSetByCategory = trueKeywordIdSetByCategory ?? new Dictionary<Category, HashSet<string>>();
    }

    public Category[] ActiveCategories => activeCategories;

    public Keyword[] GetKeywords(Category category)
    {
        if (keywordsByCategory.TryGetValue(category, out Keyword[] keywords))
        {
            return keywords ?? Array.Empty<Keyword>();
        }

        return Array.Empty<Keyword>();
    }

    public SuspectItem GetAnswer(Category category)
    {
        if (answerByCategory.TryGetValue(category, out SuspectItem answer))
        {
            return answer;
        }

        return null;
    }

    public bool IsTrue(Category category, Keyword keyword)
    {
        if (keyword == null)
        {
            return false;
        }

        if (trueKeywordsByCategory.TryGetValue(category, out Keyword[] trueKeywords) && trueKeywords != null)
        {
            for (int i = 0; i < trueKeywords.Length; i++)
            {
                if (ReferenceEquals(trueKeywords[i], keyword))
                {
                    return true;
                }
            }
        }

        string keywordId = NormalizeKey(keyword.keywordID);
        if (string.IsNullOrEmpty(keywordId))
        {
            return false;
        }

        if (trueKeywordIdSetByCategory.TryGetValue(category, out HashSet<string> idSet) && idSet != null)
        {
            return idSet.Contains(keywordId);
        }

        return false;
    }

    private static string NormalizeKey(string key)
    {
        if (key == null)
        {
            return null;
        }

        return key.Trim();
    }
}
