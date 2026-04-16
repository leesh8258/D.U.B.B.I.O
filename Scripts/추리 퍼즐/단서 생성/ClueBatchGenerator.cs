using System.Collections.Generic;

public enum ClueBatchGenerateStatus
{
    Success,
    InvalidInput,
    NoAvailableStrategy,
    Partial
}

public class ClueBatchGenerator
{
    private struct StrategyCandidate
    {
        public ClueStrategyAsset strategy;
        public int weight;
    }

    private readonly List<StrategyCandidate> candidates = new List<StrategyCandidate>(16);
    private readonly List<Clue> output = new List<Clue>(32);

    public ClueBatchGenerateStatus GenerateBatch(RoundClueGenerationSession session, int clueBudget, int globalGeneratedContentClueCount, out List<Clue> resultClues)
    {
        resultClues = new List<Clue>();

        if (session == null || session.RoundPlan == null || clueBudget <= 0)
        {
            return ClueBatchGenerateStatus.InvalidInput;
        }

        output.Clear();

        int remainingSlots = clueBudget;
        int consumedSlots = 0;
        int localGeneratedContentClueCount = 0;

        while (remainingSlots > 0)
        {
            int oneBasedClueIndex = globalGeneratedContentClueCount + localGeneratedContentClueCount + 1;
            bool isForcedPick = session.RoundPlan.TryGetForcedRule(oneBasedClueIndex, out _);

            ClueBuildRequest request = new ClueBuildRequest(remainingSlots, consumedSlots, localGeneratedContentClueCount, isForcedPick);

            if (!TryPickAndBuild(session, oneBasedClueIndex, request, out ClueBuildResult buildResult))
            {
                resultClues = new List<Clue>(output);

                if (output.Count > 0)
                {
                    return ClueBatchGenerateStatus.Partial;
                }

                return ClueBatchGenerateStatus.NoAvailableStrategy;
            }

            AppendBuildResult(buildResult);

            remainingSlots -= buildResult.consumedSlots;
            consumedSlots += buildResult.consumedSlots;
            localGeneratedContentClueCount += CountContentClues(buildResult.clues);
        }

        resultClues = new List<Clue>(output);
        return ClueBatchGenerateStatus.Success;
    }

    private bool TryPickAndBuild(RoundClueGenerationSession session, int oneBasedClueIndex, ClueBuildRequest request, out ClueBuildResult buildResult)
    {
        buildResult = ClueBuildResult.Fail();

        BuildCandidateList(session, oneBasedClueIndex, request);

        if (candidates.Count == 0)
        {
            return false;
        }

        List<StrategyCandidate> workingPool = new List<StrategyCandidate>(candidates);

        while (workingPool.Count > 0)
        {
            int pickedIndex = PickWeightedIndex(workingPool);
            if (pickedIndex < 0 || pickedIndex >= workingPool.Count)
            {
                return false;
            }

            StrategyCandidate candidate = workingPool[pickedIndex];
            ClueStrategyAsset strategy = candidate.strategy;

            if (strategy == null)
            {
                workingPool.RemoveAt(pickedIndex);
                continue;
            }

            if (!session.TryGetState(strategy, out ClueStrategyRuntimeState state))
            {
                workingPool.RemoveAt(pickedIndex);
                continue;
            }

            if (!strategy.TryBuildClues(state, request, out ClueBuildResult candidateResult))
            {
                workingPool.RemoveAt(pickedIndex);
                continue;
            }

            if (!candidateResult.IsValid)
            {
                workingPool.RemoveAt(pickedIndex);
                continue;
            }

            if (candidateResult.consumedSlots > request.remainingSlots)
            {
                workingPool.RemoveAt(pickedIndex);
                continue;
            }

            buildResult = candidateResult;
            return true;
        }

        return false;
    }

    private void BuildCandidateList(RoundClueGenerationSession session, int oneBasedClueIndex, ClueBuildRequest request)
    {
        candidates.Clear();

        RoundCluePlanSO.StrategyWeightData[] entries = session.RoundPlan.GetStrategyWeights();
        for (int i = 0; i < entries.Length; i++)
        {
            ClueStrategyAsset strategy = entries[i].strategy;
            int weight = entries[i].weight;

            if (strategy == null || weight <= 0) continue;
            if (!session.RoundPlan.IsAllowedByForcedRule(oneBasedClueIndex, strategy)) continue;
            if (request.isForcedPick && !strategy.CanAppearInForcedSlot) continue;
            if (!session.TryGetState(strategy, out ClueStrategyRuntimeState state)) continue;

            int requiredSlots = strategy.GetRequiredSlotCost(request);
            if (requiredSlots <= 0 || requiredSlots > request.remainingSlots) continue;
            if (!strategy.HasAvailableCandidates(state, request)) continue;

            StrategyCandidate candidate;
            candidate.strategy = strategy;
            candidate.weight = weight;
            candidates.Add(candidate);
        }
    }

    private int PickWeightedIndex(List<StrategyCandidate> pool)
    {
        int totalWeight = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            int weight = pool[i].weight;
            if (weight > 0)
            {
                totalWeight += weight;
            }
        }

        if (totalWeight <= 0)
        {
            return -1;
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int sum = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            int weight = pool[i].weight;
            if (weight <= 0) continue;

            sum += weight;
            if (randomValue < sum)
            {
                return i;
            }
        }

        return pool.Count - 1;
    }

    private void AppendBuildResult(ClueBuildResult buildResult)
    {
        if (buildResult.clues == null || buildResult.clues.Length == 0) return;

        for (int i = 0; i < buildResult.clues.Length; i++)
        {
            Clue clue = buildResult.clues[i];
            if (clue == null) continue;

            clue.displayOrder = output.Count + 1;

            if (clue.args == null)
            {
                clue.args = new List<ClueArg>(0);
            }

            output.Add(clue);
        }
    }

    private int CountContentClues(Clue[] clues)
    {
        if (clues == null || clues.Length == 0)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < clues.Length; i++)
        {
            Clue clue = clues[i];
            if (clue == null || clue.isMeta) continue;

            count++;
        }

        return count;
    }
}
