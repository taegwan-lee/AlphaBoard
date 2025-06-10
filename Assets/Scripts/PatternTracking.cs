using UnityEngine;
using System.Collections.Generic;
using System.Linq;


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
        // 일단 코너자리에 한번만 둬도 바로 모델 변경하게끔 해봄봄
        if ((pos.x == 0 || pos.x == 7) && (pos.y == 0 || pos.y == 7))
            return ModelType.Aggressive;
        if (pos.x == 0 || pos.x == 7 || pos.y == 0 || pos.y == 7)
            return ModelType.Neutral;
        return ModelType.Neutral;
    }

    public int CountCornerMoves()
    {
        return recentMoves.Count(m => m == ModelType.Aggressive);   
    }
}
