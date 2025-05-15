using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using System.Collections.Generic; 

public class OthelloGameMain : MonoBehaviour
{
    public ModelAsset modelAsset;
    Worker real_Engine;

    const int BoardRows = 8;
    const int BoardCols = 8;

    Tensor<float> m_Data;
    Tensor<float> m_legalMoves;
    Tensor<float> m_MoveProbabilities = null;

    int[,] board = new int[BoardRows, BoardCols];
    GameObject[,] pieces = new GameObject[BoardRows, BoardCols];

    public Material blackMat, whiteMat, transparentMat;
    public Material hintMat;
    public GameObject PiecePrefab;
    public GameUIManager uiManager;

    private int currentTurn = 1;
    private bool aiScheduled = false;
    private float m_AIDifficultyTemperature = 0.1f;
    private Vector2Int? recommendedMove;

    void Start()
    {
        var AIModel = ModelLoader.Load(modelAsset);

        var graph = new FunctionalGraph();
        var inputs = graph.AddInputs(AIModel);
        var outputs = Functional.Forward(AIModel, inputs);
               
        var boardState = outputs[0];
        var select_policy = outputs[1];

        var legal = graph.AddInput(DataType.Float, new TensorShape(BoardRows * BoardCols + 1));

        select_policy = Functional.Exp(select_policy * m_AIDifficultyTemperature);
        select_policy = (0.0001f + select_policy) * legal;
        var redSum = Functional.ReduceSum(select_policy, new int[] { 1 }, true);
        select_policy /= redSum;

        var bestMoveModel = graph.Compile(boardState,select_policy);

        real_Engine = new Worker(bestMoveModel, BackendType.CPU);

        m_Data = new Tensor<float>(new TensorShape(1, 1, BoardRows, BoardCols));
        m_legalMoves = new Tensor<float>(new TensorShape(BoardRows * BoardCols + 1));


        CreateBoard();
        CreateBoardGraphics();
        UpdateBoardGraphics();
    }

    void Update()
    {
        if (currentTurn == -1 && !aiScheduled)
        {
            Invoke("RequestAI", 1f);
            aiScheduled = true;
        }

        if (currentTurn == 1 && !aiScheduled)
        {
            HighlightRecommendedMove();
        }

        UpdateBoardGraphics();
    }

    void CreateBoard()
    {
        board[3, 3] = -1;
        board[3, 4] = 1;
        board[4, 3] = 1;
        board[4, 4] = -1;
    }

    Vector3 GetWorldPosition(int x, int y)
    {
        float offset = 3.5f;
        return new Vector3(x - offset, 0f, -(y - offset));
    }

    void CreateBoardGraphics()
    {
        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                var piece = Instantiate(PiecePrefab, GetWorldPosition(x, y), Quaternion.identity);
                pieces[y, x] = piece;

                Piece pieceScript = piece.GetComponent<Piece>();
                pieceScript.StoneX = x;
                pieceScript.StoneY = y;
                pieceScript.game = this;
            }
        }
    }

    void UpdateBoardGraphics()
    {
        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                int CurrentState = board[y, x];
                Renderer PieceColor = pieces[y, x].GetComponent<Renderer>();

                if (recommendedMove.HasValue && recommendedMove.Value.x == x && recommendedMove.Value.y == y)
                {
                    PieceColor.material = hintMat;
                }
                else if (CurrentState == 1)
                    PieceColor.material = blackMat;
                else if (CurrentState == -1)
                    PieceColor.material = whiteMat;
                else
                    PieceColor.material = transparentMat;
            }
        }
    }

    void UpdateBoardTensor()
    {   
        /*
        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                if (board[y, x] == currentTurn)
                {
                    m_Data[0, 0, y, x] = 1f;
                    m_Data[0, 1, y, x] = 0f;
                }
                else if (board[y, x] == -currentTurn)
                {
                    m_Data[0, 0, y, x] = 0f;
                    m_Data[0, 1, y, x] = 1f;
                }
                else
                {
                    m_Data[0, 0, y, x] = 0f;
                    m_Data[0, 1, y, x] = 0f;
                }
            }
        }
        */

        //기존 sentis 모델은 채널 하나만 받음
        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
            if (board[y, x] == currentTurn)
                m_Data[0, 0, y, x] = 1f;
            else if (board[y, x] == -currentTurn)
                m_Data[0, 0, y, x] = -1f;
            else
                m_Data[0, 0, y, x] = 0f;
            }
        }
    }

    void UpdateLegalMovesTensor()
    {
        bool moveAvailable = false;

        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                bool legal = board[y, x] == 0 && FlipAllDirections(x, y, currentTurn, false);
                m_legalMoves[y * BoardCols + x] = legal ? 1f : 0f;
                if (legal) moveAvailable = true;
            }
        }

        m_legalMoves[BoardRows * BoardCols] = moveAvailable ? 0f : 1f;
    }

    void RequestAI()
    {
        aiScheduled = false;

        UpdateBoardTensor();
        UpdateLegalMovesTensor();

        real_Engine.Schedule(m_Data, m_legalMoves);

        using var latestBoard = (real_Engine.PeekOutput(0) as Tensor<float>).ReadbackAndClone();
        m_MoveProbabilities?.Dispose();
        m_MoveProbabilities = (real_Engine.PeekOutput(1) as Tensor<float>).ReadbackAndClone();
        
        float boardValue = latestBoard[0, 0];

        float bestValue = -1f;
        int bestIndex = -1;

        for (int i = 0; i < m_MoveProbabilities.count; i++)
        {
            if (m_MoveProbabilities[i] > bestValue)
            {
                bestValue = m_MoveProbabilities[i];
                bestIndex = i;
            }
        }

        if (bestIndex == 64 || bestIndex == -1)
        {
            Debug.Log("AI가 패스함.");
            NextTurn();
            return;
        }

        int y = bestIndex / BoardCols;
        int x = bestIndex % BoardCols;

        board[y, x] = -1;
        FlipAllDirections(x, y, -1, true);

        recommendedMove = null;  // AI 착수 후 추천 수 초기화
        UpdateBoardGraphics();
        Debug.Log($"{latestBoard.shape}");
        Debug.Log($"{m_MoveProbabilities.shape}");
        NextTurn();
    }


    void HighlightRecommendedMove()
    {
        recommendedMove = GetRecommendedMove();
    }

    Vector2Int? GetRecommendedMove()
    {
        UpdateBoardTensor();
        UpdateLegalMovesTensor();

        real_Engine.Schedule(m_Data, m_legalMoves);

        using var latestBoard = (real_Engine.PeekOutput(0) as Tensor<float>).ReadbackAndClone();
        m_MoveProbabilities?.Dispose();
        m_MoveProbabilities = (real_Engine.PeekOutput(1) as Tensor<float>).ReadbackAndClone();

        float boardValue = latestBoard[0, 0]; 

        float bestValue = float.MinValue;
        int bestIndex = -1;

        for (int i = 0; i < BoardRows * BoardCols; i++)
        {
            if (m_MoveProbabilities[i] > bestValue)
            {
                bestValue = m_MoveProbabilities[i];
                bestIndex = i;
            }
        }

        Debug.Log($"[추천 수 계산] 승률: {boardValue:F4}, 선택 index: {bestIndex}");

        if (bestIndex == -1)
            return null;

        int y = bestIndex / BoardCols;
        int x = bestIndex % BoardCols;

        return new Vector2Int(x, y);
    }


    public void OnPieceClicked(int x, int y)
    {
        if (board[y, x] != 0) return;

        bool valid = FlipAllDirections(x, y, currentTurn, false);
        if (!valid)
        {
            Debug.Log("여기에 수를 둘 수 없습니다");
            return;
        }

        board[y, x] = currentTurn;
        FlipAllDirections(x, y, currentTurn, true);

        recommendedMove = null;  // 착수 후 추천 수 초기화
        UpdateBoardGraphics();

        NextTurn();
    }


    void NextTurn()
    {
        int nextTurn = -currentTurn;

        if (HasAnyValidMove(nextTurn))
        {
            currentTurn = nextTurn;
        }
        else if (HasAnyValidMove(currentTurn))
        {
            Debug.Log("상대방은 착수할 수 없어 턴을 패스합니다.");
        }
        else
        {
            Debug.Log("양쪽 모두 착수 불가 → 게임 종료");

            int black = 0, white = 0;
            for (int y = 0; y < BoardRows; y++)
            {
                for (int x = 0; x < BoardCols; x++)
                {
                    if (board[y, x] == 1) black++;
                    else if (board[y, x] == -1) white++;
                }
            }

            uiManager.ShowGameOver(black, white);
        }
    }

    bool HasAnyValidMove(int player)
    {
        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                if (board[y, x] == 0 && FlipAllDirections(x, y, player, false))
                    return true;
            }
        }
        return false;
    }

    bool FlipAllDirections(int x, int y, int currentPlayer, bool actuallyFlip = true)
    {
        bool flippedAny = false;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                if (TryFlipInDirection(x, y, dx, dy, currentPlayer, actuallyFlip))
                    flippedAny = true;
            }
        }
        return flippedAny;
    }

    bool TryFlipInDirection(int startX, int startY, int dx, int dy, int currentPlayer, bool actuallyFlip)
    {
        int x = startX + dx;
        int y = startY + dy;
        int opponent = -currentPlayer;

        List<(int, int)> toFlip = new List<(int, int)>();

        while (x >= 0 && x < 8 && y >= 0 && y < 8 && board[y, x] == opponent)
        {
            toFlip.Add((x, y));
            x += dx;
            y += dy;
        }

        if (x >= 0 && x < 8 && y >= 0 && y < 8 && board[y, x] == currentPlayer && toFlip.Count > 0)
        {
            if (actuallyFlip)
            {
                foreach (var (fx, fy) in toFlip)
                    board[fy, fx] = currentPlayer;
            }
            return true;
        }

        return false;
    }
}
