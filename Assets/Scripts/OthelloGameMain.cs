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

    private Vector2Int? recommendedMove;


    void Start()
    {
        var AIModel = ModelLoader.Load(modelAsset);

        var graph = new FunctionalGraph();
        var inputs = graph.AddInputs(AIModel);
        var outputs = Functional.Forward(AIModel, inputs);
        var boardState = outputs[0];
        var bestMove = outputs[1];

        var legal = graph.AddInput(DataType.Float, new TensorShape(BoardRows * BoardCols + 1 ));

        bestMove = Functional.Exp(bestMove);
        bestMove = (0.0001f + bestMove) *legal;
        var redSum = Functional.ReduceSum(bestMove, new int[] {1}, true);
        bestMove /= redSum;

        var bestMoveModel = graph.Compile(boardState, bestMove);
        real_Engine = new Worker(bestMoveModel, BackendType.CPU);

        m_Data = new Tensor<float>(new TensorShape(1,1, BoardRows, BoardCols));
        m_legalMoves = new Tensor<float>(new TensorShape(BoardRows * BoardCols + 1));

        CreateBoard();
        CreateBoardGraphics();
        UpdateBoardGraphics();
    }

    void Update()
    {
        UpdateBoardGraphics();

        if (currentTurn == 1 && !aiScheduled) // 내 차례
        {
            HighlightRecommendedMove();
        }

        if (currentTurn == -1 && !aiScheduled)
        {
            Invoke("RequestAI", 1f);
            aiScheduled = true;
        }

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
        for(int y = 0; y < BoardRows; y++)
        {
            for(int x = 0; x < BoardCols; x++)
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

                if (CurrentState == 1)
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
        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                m_Data[0, 0, y, x] = board[y, x] * currentTurn;
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

        using var boardState = (real_Engine.PeekOutput(0) as Tensor<float>).ReadbackAndClone();    
        m_MoveProbabilities?.Dispose();
        m_MoveProbabilities = (real_Engine.PeekOutput(1) as Tensor<float>).ReadbackAndClone();

        float boardValue = boardState[0,0];
        int bestIndex = -1;

        for (int i = 0; i < m_MoveProbabilities.count; i++)
        {
            if (m_MoveProbabilities[i] > boardValue)
            {
                boardValue = m_MoveProbabilities[i];
                bestIndex = i;
            }
        }

        if (bestIndex < 0 || bestIndex >= BoardRows * BoardCols)
        {
        Debug.Log("AI는 둘 곳이 없습니다. 턴 넘기기 또는 종료 처리합니다.");
        NextTurn(); // 턴 넘김 또는 종료 판정
        return;
        }

        int y = bestIndex / BoardCols;
        int x = bestIndex % BoardCols;

        board[y, x] = -1;
        FlipAllDirections(x, y, -1, true);
        UpdateBoardGraphics();

        NextTurn();
    }

    public Vector2Int? GetRecommendedMove()
    {
        UpdateBoardTensor();
        UpdateLegalMovesTensor();

        real_Engine.Schedule(m_Data, m_legalMoves);

        var moveProb = (real_Engine.PeekOutput(1) as Tensor<float>).ReadbackAndClone();

        float bestValue = float.MinValue;
        int bestIndex = -1;

        for (int i = 0; i < BoardRows * BoardCols; i++)
        {
            if (moveProb[i] > bestValue)
            {
                bestValue = moveProb[i];
                bestIndex = i;
            }
        }

        moveProb.Dispose();

        if (bestIndex == -1)
            return null;

        int y = bestIndex / BoardCols;
        int x = bestIndex % BoardCols;
        return new Vector2Int(x, y);
    }


    void HighlightRecommendedMove()
    {
        recommendedMove = GetRecommendedMove();

        if (recommendedMove.HasValue)
        {
            int x = recommendedMove.Value.x;
            int y = recommendedMove.Value.y;

            if (board[y, x] == 0)
            {
                var renderer = pieces[y, x].GetComponent<Renderer>();
                renderer.material = hintMat;
            }
        }
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

        UpdateBoardGraphics();
        NextTurn();
    }

    // 턴을 넘길 때 호출되는 함수: 턴 패스 또는 게임 종료 처리 포함
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
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (board[y, x] == 1) black++;
                    else if (board[y, x] == -1) white++;
                }
            }

            uiManager.ShowGameOver(black, white);
        }
    }

    // 현재 플레이어가 둘 수 있는 수가 있는지 확인
    bool HasAnyValidMove(int player) 
    {
        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                // 빈 칸이면서 해당 플레이어가 착수 시 뒤집을 수 있으면 유효한 수
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
