using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using System.Collections.Generic; 

public class OthelloGameMain : MonoBehaviour
{
    //모델
    public ModelAsset modelAsset;
    Worker real_Engine;

    const int BoardRows = 8;
    const int BoardCols = 8;

    //텐서값
    Tensor<float> m_Data;
    Tensor<float> m_legalMoves;
    Tensor<float> m_MoveProbabilities = null;

    //보드판정
    int[,] board = new int[BoardRows, BoardCols];

    //그래픽용 보드판 배열
    GameObject[,] pieces = new GameObject[BoardRows, BoardCols];

    //돌 생성
    public Material blackMat, whiteMat, transparentMat;
    public GameObject PiecePrefab;

    //턴용
    private int currentTurn = 1;
    private bool aiScheduled = false;
    private float m_AIDifficultyTemperature = 0.1f;

    void Start()
    {
        var AIModel = ModelLoader.Load(modelAsset);

        var graph = new FunctionalGraph();
        var inputs = graph.AddInputs(AIModel);
        var outputs = Functional.Forward(AIModel, inputs);
               
        var select_policy = outputs[0];
        var boardState = outputs[1];

        var legal = graph.AddInput(DataType.Float, new TensorShape(BoardRows * BoardCols +1));

        select_policy = Functional.Exp(select_policy * m_AIDifficultyTemperature);
        select_policy = (0.0001f + select_policy) * legal;
        var redSum = Functional.ReduceSum(select_policy, new int[] {1}, true);
        select_policy /= redSum;

        var bestMoveModel = graph.Compile(select_policy, boardState);

        real_Engine = new Worker(bestMoveModel, BackendType.CPU);

        m_Data = new Tensor<float>(new TensorShape(1,2, BoardRows, BoardCols));
        m_legalMoves = new Tensor<float>(new TensorShape(BoardRows * BoardCols + 1));

        CreateBoard();
        CreateBoardGraphics();
        UpdateBoardGraphics();
    }

    void Update()
    {
        if(currentTurn==-1 && !aiScheduled){
            Invoke("RequestAI",1f);
            aiScheduled = true;
        }
        UpdateBoardGraphics();
    }

    //초기 보드판 배열
    void CreateBoard()
    {
        board[3, 3] = -1;
        board[3, 4] = 1;
        board[4, 3] = 1;
        board[4, 4] = -1;
    }

    //보드좌표 그래픽 만들게 월드좌표로
    Vector3 GetWorldPosition(int x, int y)
    {
        float offset = 3.5f; // 보드를 가운데 정렬하기 위한 오프셋
        return new Vector3(x - offset, 0f, -(y - offset));
    }
       

    //보드판 생성 그래픽용
    void CreateBoardGraphics()
    {
        for(int y=0; y<BoardRows; y++)
        {
            for(int x = 0; x<BoardCols; x++)
            {
                //그래픽용으로 만들때 월드좌표 할당 및 게임 로직용으로 프리팹마다 x,y 좌표값 넣어둘거
                var piece = Instantiate(PiecePrefab, GetWorldPosition(x, y), Quaternion.identity);
                pieces[y, x] = piece;

                Piece pieceScript = piece.GetComponent<Piece>();
                pieceScript.StoneX = x;
                pieceScript.StoneY = y;
                //하이라키에서 안하게 메인에서 game오브젝트 할당(piece스크립트)
                pieceScript.game = this;
            }
        }
    }

    //그래픽 보드판 업데이트
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


    //보드현황 tensor 변환   
    void UpdateBoardTensor()
    {
         for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                    if (board[y, x] == currentTurn)
                    {
                        m_Data[0, 0, y, x] = 1f;  // 내 돌
                        m_Data[0, 1, y, x] = 0f;
                    }
                    else if (board[y, x] == -currentTurn)
                    {
                        m_Data[0, 0, y, x] = 0f;
                        m_Data[0, 1, y, x] = 1f;  // 상대 돌
                    }
                    else
                    {   
                        m_Data[0, 0, y, x] = 0f;
                        m_Data[0, 1, y, x] = 0f;
                    }
            }
        }
    }

    //마스킹 텐서화
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

        // 패스 허용 여부
        m_legalMoves[BoardRows * BoardCols] = moveAvailable ? 0f : 1f;
    }


    //AI호출
    void RequestAI()
    {
        aiScheduled = false;

        UpdateBoardTensor();
        UpdateLegalMovesTensor();

        real_Engine.Schedule(m_Data, m_legalMoves);

        m_MoveProbabilities?.Dispose();
        m_MoveProbabilities = (real_Engine.PeekOutput(0) as Tensor<float>).ReadbackAndClone();
        using var latestBoard = (real_Engine.PeekOutput(1) as Tensor<float>).ReadbackAndClone();    


        float boardValue = latestBoard[0,0];

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

        if (bestIndex == 64)
        {
            Debug.Log("AI가 패스함.");
            currentTurn = -currentTurn;
            return;
        }

        int y = bestIndex / BoardCols;
        int x = bestIndex % BoardCols;

        board[y, x] = -1;
        FlipAllDirections(x, y, -1, true);
        UpdateBoardGraphics();

        Debug.Log($"현재 승률 : {boardValue}");
        float raw = latestBoard[0, 0];
        Debug.Log($"[승률] 값: {raw:F4}  (보드 변화 감지용)");
        Debug.Log($"현재 턴: {currentTurn}");
        currentTurn = -currentTurn;
    }

    //돌 클릭 이벤트
    public void OnPieceClicked(int x, int y)
    {
        if (board[y, x] != 0) return;         // 이미 돌 있으면 무시

        bool valid = FlipAllDirections(x, y, currentTurn, false); // 실제 뒤집지 않고 검사

        if (!valid)
        {
            Debug.Log("여기에 수를 둘수 없습니다");
            return;
        }

        // 유효한 착수면 돌 놓고 뒤집기
        board[y, x] = currentTurn;
        FlipAllDirections(x, y, currentTurn, true);

        UpdateBoardGraphics();

        currentTurn = -currentTurn; // 턴 넘기기
    }

    //false일때 검사, true일때 뒤집기
    bool FlipAllDirections(int x, int y, int currentPlayer, bool actuallyFlip = true)
    {
        bool flippedAny = false; //돌을 하나라도 뒤집었는지 체크

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

        //뒤집을 돌들 넣기위한 리스트
        List<(int, int)> toFlip = new List<(int, int)>();

        // 상대 돌이 이어져 있는지 체크
        while (x >= 0 && x < 8 && y >= 0 && y < 8 && board[y, x] == opponent)
        {
            toFlip.Add((x, y));
            x += dx;
            y += dy;
        }

        // 끝에 내 돌이 있으면 뒤집기 수행
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
