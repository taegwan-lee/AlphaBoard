using UnityEngine;
using System.Collections.Generic;

public enum ModelType
{
    Aggressive,
    Neutral
}

public class PatternTracking : MonoBehaviour
{
    private Queue<ModelType> recentMoves = new Queue<ModelType>();
    private const int maxHistory = 5;

    public void RecordMove(Vector2Int pos)
    {
        ModelType type = ClassifyMove(pos);
        if (recentMoves.Count >= maxHistory)
            recentMoves.Dequeue();
        recentMoves.Enqueue(type);
    }

    private ModelType ClassifyMove(Vector2Int pos)
    {
        // 코너는 공격적인 수, 가장자리는 수비적인 수로 분류
        if ((pos.x == 0 || pos.x == 7) && (pos.y == 0 || pos.y == 7))
            return ModelType.Aggressive;
        if (pos.x == 0 || pos.x == 7 || pos.y == 0 || pos.y == 7)
            return ModelType.Neutral;
        return ModelType.Neutral;
    }

    public bool ShouldSwitchToAggressiveModel()
    {
        int aggressive = 0;
        foreach (var move in recentMoves)
            if (move == ModelType.Aggressive) aggressive++;

        return aggressive >= 1; // 최근 5턴 중 3턴 이상 공격적이면
    }

    public ModelType GetRecommendedModelType()
    {
        return ShouldSwitchToAggressiveModel() ? ModelType.Aggressive : ModelType.Neutral;
    }
}
