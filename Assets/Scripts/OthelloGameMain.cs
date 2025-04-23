using UnityEngine;
using Unity.Sentis;
using System.Collections.Generic;

public class OthelloGameMain : MonoBehaviour
{
    //모델
    public ModelAsset modelAsset;
    private Worker OthelloWorker;

    const int BoardRows = 8;
    const int BoardCols = 8;

    //마스킹용 텐서
    Tensor Masking = new Tensor<float>(new TensorShape(BoardRows * BoardCols + 1));

    //보드판
    int[,] board = new int[BoardRows, BoardCols];

    //그래픽용 보드판 배열
    GameObject[,] pieces = new GameObject[BoardRows, BoardCols];

    //돌 생성
    public Material blackMat, whiteMat, transparentMat;
    public GameObject PiecePrefab;

    //턴
    int currentTurn = 1;


    void Start()
    {
        //모델 불러오기
        Model model = ModelLoader.Load(modelAsset);
        OthelloWorker = new Worker(model, BackendType.GPUCompute);

        Tensor<float> Masking = new Tensor<float>(new TensorShape(BoardRows * BoardCols + 1));

        CreateBoard();
        CreateBoardGraphics();
        UpdateBoardGraphics();

        for (int i = 0; i < BoardRows * BoardCols; i++)
        {
            Masking[i] = 1f;
        }


        Masking[BoardRows * BoardCols] = 0f; //마스킹인데 지금 더미임. 지금 이 값 안들어감.
        //Invoke("RequestAI", 1f); //작동되나 확인하려고 start하자마자 ai착수시켜봄.
    }

    void Update()
    {
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

    //현재 보드판 정보 sentis로 넘겨주게 텐서로 만들기
    Tensor<float> BoardToTensor()
    {
        var tensor = new Tensor<float>(new TensorShape(1, 1, BoardRows, BoardCols));
        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                tensor[0, 0, y, x] = board[y, x];

            }
        }
        return tensor;
    }

    //AI호출
    void RequestAI()
    {
        Debug.Log("AI호출 확인용");

        using var boardTensor = BoardToTensor();
        using var legalTensor = Masking;

        OthelloWorker.Schedule(boardTensor);

        using var moveProbabilities = (OthelloWorker.PeekOutput(1) as Tensor<float>).ReadbackAndClone();

        int bestIndex = 0;
        float bestValue = -1f;

        for (int i=0; i<moveProbabilities.count; i++)
        {
            if (moveProbabilities[i]> bestValue)
            {
                bestValue = moveProbabilities[i];
                bestIndex = i;
            }
        }

        int y = bestIndex / BoardCols;
        int x = bestIndex % BoardCols;

        board[y, x] = -1;
        Debug.Log($"sentis가 ({x},{y}에 흰돌 놈");
        UpdateBoardGraphics();
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
