using System.Collections.Generic;

public static class RoundClueGenerationSessionBuilder
{
    public static bool Build(
        RoundClueContext roundContext,
        RoundCluePlanSO roundPlan,
        out RoundClueGenerationSession session)
    {
        session = null;

        if (roundContext == null || roundPlan == null)
        {
            return false;
        }

        Dictionary<ClueStrategyAsset, ClueStrategyRuntimeState> stateMap = new Dictionary<ClueStrategyAsset, ClueStrategyRuntimeState>();

        RoundCluePlanSO.StrategyWeightData[] entries = roundPlan.GetStrategyWeights();

        for (int i = 0; i < entries.Length; i++)
        {
            ClueStrategyAsset strategy = entries[i].strategy;
            if (strategy == null || entries[i].weight <= 0) continue;

            List<Clue> candidates = new List<Clue>(64);
            List<Clue> lieCandidates = new List<Clue>(32);

            if (!(strategy is LieStrategyAsset))
            {
                strategy.BuildRoundCandidates(roundContext, candidates);
            }

            strategy.BuildRoundLieCandidates(roundContext, lieCandidates);

            stateMap[strategy] = new ClueStrategyRuntimeState(strategy, candidates, lieCandidates);
        }

        foreach (KeyValuePair<ClueStrategyAsset, ClueStrategyRuntimeState> pair in stateMap)
        {
            if (!(pair.Key is LieStrategyAsset)) continue;

            ClueStrategyRuntimeState lieState = pair.Value;

            foreach (KeyValuePair<ClueStrategyAsset, ClueStrategyRuntimeState> sourcePair in stateMap)
            {
                ClueStrategyAsset sourceStrategy = sourcePair.Key;
                if (sourceStrategy == pair.Key) continue;
                if (!sourceStrategy.CanBeUsedByLieStrategy) continue;

                List<Clue> sourceLieCandidates = sourcePair.Value.RemainingLieCandidates;
                if (sourceLieCandidates == null || sourceLieCandidates.Count == 0) continue;

                lieState.RemainingCandidates.AddRange(sourceLieCandidates);
            }
        }

        session = new RoundClueGenerationSession(roundContext, roundPlan, stateMap);
        return true;
    }
}
