using System.Collections.Generic;

public class RoundClueGenerationSession
{
    public RoundClueContext RoundContext { get; }
    public RoundCluePlanSO RoundPlan { get; }

    private readonly Dictionary<ClueStrategyAsset, ClueStrategyRuntimeState> stateMap;

    public RoundClueGenerationSession(RoundClueContext roundContext, RoundCluePlanSO roundPlan, Dictionary<ClueStrategyAsset, ClueStrategyRuntimeState> stateMap)
    {
        RoundContext = roundContext;
        RoundPlan = roundPlan;
        this.stateMap = stateMap ?? new Dictionary<ClueStrategyAsset, ClueStrategyRuntimeState>();
    }

    public bool TryGetState(ClueStrategyAsset strategy, out ClueStrategyRuntimeState state)
    {
        state = null;

        if (strategy == null)
        {
            return false;
        }

        return stateMap.TryGetValue(strategy, out state);
    }
}
