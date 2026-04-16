using UnityEngine;

public abstract class SingleClueStrategyAsset : ClueStrategyAsset
{
    public override bool HasAvailableCandidates(ClueStrategyRuntimeState state, ClueBuildRequest request)
    {
        if (state == null)
        {
            return false;
        }

        if (request.remainingSlots < GetRequiredSlotCost(request))
        {
            return false;
        }

        return state.RemainingCandidates != null && state.RemainingCandidates.Count > 0;
    }

    public override bool TryBuildClues(ClueStrategyRuntimeState state, ClueBuildRequest request, out ClueBuildResult result)
    {
        result = ClueBuildResult.Fail();

        if (!HasAvailableCandidates(state, request))
        {
            return false;
        }

        int index = Random.Range(0, state.RemainingCandidates.Count);
        Clue clue = state.RemainingCandidates[index];
        state.RemainingCandidates.RemoveAt(index);

        result.clues = new[] { clue };
        result.consumedSlots = GetRequiredSlotCost(request);
        return true;
    }
}
