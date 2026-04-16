using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoundCluePlanSO", menuName = "Clues/RoundCluePlanSO")]
public class RoundCluePlanSO : ScriptableObject
{
    [Serializable]
    public struct StrategyWeightData
    {
        public ClueStrategyAsset strategy;
        [Min(0)] public int weight;
    }

    [Serializable]
    public struct ForcedStrategyRule
    {
        [Min(1)] public int everyNth;
        public ClueStrategyAsset[] allowedStrategies;
    }

    [Header("Strategy Weight")]
    [SerializeField] private StrategyWeightData[] strategyWeights = Array.Empty<StrategyWeightData>();

    [Header("Forced Rules")]
    [SerializeField] private ForcedStrategyRule[] forcedRules = Array.Empty<ForcedStrategyRule>();

    public StrategyWeightData[] GetStrategyWeights()
    {
        return strategyWeights ?? Array.Empty<StrategyWeightData>();
    }

    public ForcedStrategyRule[] GetForcedRules()
    {
        return forcedRules ?? Array.Empty<ForcedStrategyRule>();
    }

    public int GetWeight(ClueStrategyAsset strategy)
    {
        if (strategy == null)
        {
            return 0;
        }

        StrategyWeightData[] list = GetStrategyWeights();
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i].strategy == strategy)
            {
                return Mathf.Max(0, list[i].weight);
            }
        }

        return 0;
    }

    public bool IsEnabled(ClueStrategyAsset strategy)
    {
        return GetWeight(strategy) > 0;
    }

    public bool TryGetForcedRule(int oneBasedClueIndex, out ForcedStrategyRule matchedRule)
    {
        matchedRule = default;

        if (oneBasedClueIndex <= 0)
        {
            return false;
        }

        ForcedStrategyRule[] rules = GetForcedRules();
        for (int i = 0; i < rules.Length; i++)
        {
            ForcedStrategyRule rule = rules[i];
            if (rule.everyNth <= 0) continue;

            if (oneBasedClueIndex % rule.everyNth == 0)
            {
                matchedRule = rule;
                return true;
            }
        }

        return false;
    }

    public bool IsAllowedByForcedRule(int oneBasedClueIndex, ClueStrategyAsset strategy)
    {
        if (strategy == null)
        {
            return false;
        }

        if (!TryGetForcedRule(oneBasedClueIndex, out ForcedStrategyRule rule))
        {
            return true;
        }

        if (rule.allowedStrategies == null || rule.allowedStrategies.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < rule.allowedStrategies.Length; i++)
        {
            if (rule.allowedStrategies[i] == strategy)
            {
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        ValidateStrategyWeights();
        ValidateForcedRules();
    }

    private void ValidateStrategyWeights()
    {
        StrategyWeightData[] list = GetStrategyWeights();
        HashSet<ClueStrategyAsset> seenStrategies = new HashSet<ClueStrategyAsset>();

        for (int i = 0; i < list.Length; i++)
        {
            StrategyWeightData entry = list[i];

            if (entry.strategy == null)
            {
                if (entry.weight > 0)
                {
                    Debug.LogWarning($"[RoundCluePlanSO] strategyWeights[{i}] has weight {entry.weight} but strategy is null.", this);
                }

                continue;
            }

            if (!seenStrategies.Add(entry.strategy))
            {
                Debug.LogWarning($"[RoundCluePlanSO] Duplicate strategy in strategyWeights: {entry.strategy.name}", this);
            }
        }
    }

    private void ValidateForcedRules()
    {
        ForcedStrategyRule[] rules = GetForcedRules();

        for (int i = 0; i < rules.Length; i++)
        {
            ForcedStrategyRule rule = rules[i];

            if (rule.everyNth <= 0)
            {
                Debug.LogWarning($"[RoundCluePlanSO] forcedRules[{i}] has invalid everyNth: {rule.everyNth}", this);
                continue;
            }

            if (rule.allowedStrategies == null || rule.allowedStrategies.Length == 0)
            {
                Debug.LogWarning($"[RoundCluePlanSO] forcedRules[{i}] has no allowedStrategies. This rule will block generation on its turn.", this);
                continue;
            }

            HashSet<ClueStrategyAsset> seenStrategies = new HashSet<ClueStrategyAsset>();

            for (int j = 0; j < rule.allowedStrategies.Length; j++)
            {
                ClueStrategyAsset strategy = rule.allowedStrategies[j];
                if (strategy == null)
                {
                    Debug.LogWarning($"[RoundCluePlanSO] forcedRules[{i}].allowedStrategies[{j}] is null.", this);
                    continue;
                }

                if (!seenStrategies.Add(strategy))
                {
                    Debug.LogWarning($"[RoundCluePlanSO] forcedRules[{i}] contains duplicate strategy: {strategy.name}", this);
                }

                if (!IsEnabled(strategy))
                {
                    Debug.LogWarning($"[RoundCluePlanSO] forcedRules[{i}] contains strategy '{strategy.name}' that is not enabled in strategyWeights.", this);
                }
            }
        }
    }
}
