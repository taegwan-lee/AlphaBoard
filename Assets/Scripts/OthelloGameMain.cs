using UnityEngine;

using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using DG.Tweening;
using UnityEngine.SceneManagement;

public enum AIType
{
    AggressiveAIType,
    NeutralAIType
}

public class OthelloGameMain : MonoBehaviour
{
    public Unity.InferenceEngine.ModelAsset modelAsset;

    public Unity.InferenceEngine.ModelAsset aggressiveModelAsset; //공격적인 모델
    public TMP_Text turnText;
    Unity.InferenceEngine.Worker real_Engine;
    Unity.InferenceEngine.Worker real_EngineAgg; //공격적인 모델용 워커


    //패턴 파악
    private PatternTracking patternTracker = new PatternTracking();
    private AIType currentType = AIType.NeutralAIType;

    const int BoardRows = 8;
    const int BoardCols = 8;

    Unity.InferenceEngine.Tensor<float> m_Data;
    Unity.InferenceEngine.Tensor<float> m_legalMoves;
    Unity.InferenceEngine.Tensor<float> m_MoveProbabilities = null;

    int[,] board = new int[BoardRows, BoardCols];
    GameObject[,] pieces = new GameObject[BoardRows, BoardCols];

    public Material blackMat, whiteMat, transparentMat;
    public Material hintMat;
    public GameObject PiecePrefab;
    public GameUIManager uiManager;
    public GameObject probTextPrefab;
    private GameObject[,] probTexts = new GameObject[BoardRows, BoardCols];

    private int currentTurn = 1;
    private bool aiScheduled = false;
    private float m_AIDifficultyTemperature = 0.1f;
    private Vector2Int? recommendedMove;

    public AudioClip wrongStone;
    public AudioSource wrongStoneObj;
    public AudioClip NewTypeClip;
    public AudioSource NewTypeSource;

    public AudioSource placeStoneSource;
    public AudioClip placeStoneClip;
    public AudioClip aiplaceStoneClip;
    public AudioSource GameBgmSource;
    public AudioClip GameBgmClip;
    public AudioSource transitionSound;
    public AudioClip transitionClip;
    public AudioSource AggressivePhase;

    public LightningEffect lightningEffect;
    public CameraCharacter cameraRotate;


    //캐릭터 프리팹 생성용 오브젝트
    public GameObject EasyMan;
    public GameObject NormalGirl;
    public GameObject HardMan;
    private GameObject currentCharacter; //현재 캐릭터 (중간 연출용)
    public Sprite aggressiveSprite;
    public Sprite aggressiveSprite_Normal;
    public Sprite aggressiveSprite_Hard;



    public Transform CharacterPoint; //캐릭터 좌표

    //다이올로그 모음
    public DialogueManager dialogueManager;
    public DialogueSequence gameOverDialogue;
    public DialogueSequence aggressiveDialogue;
    public DialogueSequence aggressiveDialogue_2;
    public DialogueSequence aggressiveDialogue_Normal_1;
    public DialogueSequence aggressiveDialogue_Normal_2;

    public DialogueSequence aggressiveDialogue_Hard_1;
    public DialogueSequence aggressiveDialogue_Hard_2;
    public DialogueSequence gameOverDialogue_Easy_Lose;
    public DialogueSequence gameOverDialogue_Normal_Win;
    public DialogueSequence gameOverDialogue_Normal_Lose;
    public DialogueSequence gameOverDialogue_Hard_Win;
    public DialogueSequence gameOverDialogue_Hard_Lose;


    [SerializeField] private float cellSpacing = 1.2f;

    private bool gameEnded = false;
    private bool isDialoguePlaying = false; //중간 연출용 대사칠 때 밑에 턴 안나오게
    public Image transitionPanel;
    public CanvasGroup gameOverPanel;

    //승패 연출용
    public RectTransform victoryImage;
    public RectTransform defeatImage;

    void Start()
    {
        //난이도 받아옴
        Difficulty difficulty = GameSettings.SelectedDifficulty;
        SpawnCharacterBasedOnDifficulty();

        if (difficulty == Difficulty.Easy)
        {
            m_AIDifficultyTemperature = 0.1f;
        }
        else if (difficulty == Difficulty.Normal)
        {
            m_AIDifficultyTemperature = 0.5f;
        }
        else
        {
            m_AIDifficultyTemperature = 1.0f;
        }

        var AIModel = Unity.InferenceEngine.ModelLoader.Load(modelAsset);
        var graph = new Unity.InferenceEngine.FunctionalGraph();
        var inputs = graph.AddInputs(AIModel);
        var outputs = Unity.InferenceEngine.Functional.Forward(AIModel, inputs);
        var select_policy = outputs[0];
        var boardState = outputs[1];
        var legal = graph.AddInput(Unity.InferenceEngine.DataType.Float, new Unity.InferenceEngine.TensorShape(BoardRows * BoardCols + 1));
        select_policy = Unity.InferenceEngine.Functional.Exp(select_policy * m_AIDifficultyTemperature);
        select_policy = (0.0001f + select_policy) * legal;
        var redSum = Unity.InferenceEngine.Functional.ReduceSum(select_policy, new int[] { 1 }, true);
    

        //공격적 모델은 변수명뒤에 Agg붙일거임
        var aggressiveModel = Unity.InferenceEngine.ModelLoader.Load(aggressiveModelAsset);
        var graphAgg = new Unity.InferenceEngine.FunctionalGraph(); //공격모델용 그래프
        var inputsAgg = graphAgg.AddInputs(aggressiveModel);
        var outputsAgg = Unity.InferenceEngine.Functional.Forward(aggressiveModel, inputsAgg);
        var select_policyAgg = outputsAgg[0];
        var boardStateAgg = outputsAgg[1];
        var legalAgg = graphAgg.AddInput(Unity.InferenceEngine.DataType.Float, new Unity.InferenceEngine.TensorShape(BoardRows * BoardCols + 1));
        select_policyAgg = Unity.InferenceEngine.Functional.Exp(select_policyAgg * m_AIDifficultyTemperature);
        select_policyAgg = (0.0001f + select_policyAgg) * legalAgg;
        var redSumAgg = Unity.InferenceEngine.Functional.ReduceSum(select_policyAgg, new int[] { 1 }, true);


        select_policy /= redSum;
        select_policyAgg /= redSumAgg;


        var bestMoveModel = graph.Compile(select_policy , boardState);
        var bestMoveModelAgg = graphAgg.Compile(select_policyAgg, boardStateAgg);

        real_Engine = new Unity.InferenceEngine.Worker(bestMoveModel, Unity.InferenceEngine.BackendType.CPU);
        real_EngineAgg = new Unity.InferenceEngine.Worker(bestMoveModelAgg, Unity.InferenceEngine.BackendType.CPU);

        m_Data = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(1, 2, BoardRows, BoardCols));
        m_legalMoves = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(BoardRows * BoardCols + 1));

        GameBgmSource.Play();

        CreateBoard();
        CreateBoardGraphics();
        UpdateBoardGraphics();
    }

    void Update()
    {
        if (currentTurn == -1 && !aiScheduled && !isDialoguePlaying && !gameEnded)
        {
            aiScheduled = true;
        }

        if (currentTurn == 1 && !aiScheduled && !isDialoguePlaying && !gameEnded)
        {
            HighlightRecommendedMove();
        }

        UpdateBoardGraphics();

        if (!isDialoguePlaying && !gameEnded)
            turnText.gameObject.SetActive(true);
        UpdateTurnText(); //UI 턴 표시용

        if (gameEnded || isDialoguePlaying)
        {
            turnText.gameObject.SetActive(false);
        }

        //강제종료
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (gameEnded)
        {
            turnText.text = "게임 종료";
            return;
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
        float offset = (BoardCols - 1) / 2f;
        return new Vector3((x - offset) * cellSpacing, 0f, -(y - offset) * cellSpacing);
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

                // 확률 텍스트 생성
                var textObj = Instantiate(probTextPrefab, GetWorldPosition(x, y) + Vector3.up * 0.1f, Quaternion.identity);
                textObj.transform.SetParent(piece.transform); // 조각과 같이 따라다니게
                textObj.SetActive(false);
                probTexts[y, x] = textObj;
            }
        }
    }

    void UpdateBoardGraphics()
    {
        float blink = Mathf.PingPong(Time.time * 2f, 1f);
        bool showHint = blink > 0.5f;

        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                int CurrentState = board[y, x];
                Renderer PieceColor = pieces[y, x].GetComponent<Renderer>();

                if (recommendedMove.HasValue &&
                    recommendedMove.Value.x == x &&
                    recommendedMove.Value.y == y)
                {
                    //힌트 깜빡이게
                    PieceColor.material = showHint ? hintMat : transparentMat;
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

        /*
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
        */
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

        float rand = UnityEngine.Random.value;
        float cumulative = 0f;

        Unity.InferenceEngine.Tensor<float> latestBoard;

        //aitype 바뀌었는지 확인후 해당 ai호출
        if (currentType == AIType.AggressiveAIType)
        {
            real_EngineAgg.Schedule(m_Data, m_legalMoves);
            m_MoveProbabilities?.Dispose();
            m_MoveProbabilities = (real_EngineAgg.PeekOutput(0) as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();
            latestBoard = (real_EngineAgg.PeekOutput(1) as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();
        }
        else
        {
            real_Engine.Schedule(m_Data, m_legalMoves);
            m_MoveProbabilities?.Dispose();
            m_MoveProbabilities = (real_Engine.PeekOutput(0) as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();
            latestBoard = (real_Engine.PeekOutput(1) as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();
        }


        float boardValue = latestBoard[0, 0];

        float bestValue = -1f;
        int bestIndex = -1;

        for (int i = 0; i < m_MoveProbabilities.count; i++)
        {
            cumulative += m_MoveProbabilities[i];
            if (rand <= cumulative)
            {
                bestIndex = i;
                break;
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
        /*
        Debug.Log($"{latestBoard.shape}");
        Debug.Log($"{m_MoveProbabilities.shape}");
        */
        Debug.Log($"{boardValue}");
        placeStoneSource.pitch = 0.7f;
        placeStoneSource.PlayOneShot(aiplaceStoneClip);
        PrintMoveProbabilities();

        NextTurn();
    }


    void HighlightRecommendedMove()
    {
         if (currentType == AIType.AggressiveAIType)
        {
            recommendedMove = GetRecommendedMoveAgg();
        }
        else
        {
            recommendedMove = GetRecommendedMove();
        }
    }

    Vector2Int? GetRecommendedMove()
    {
        UpdateBoardTensor();
        UpdateLegalMovesTensor();

        real_Engine.Schedule(m_Data, m_legalMoves);

        m_MoveProbabilities?.Dispose();
        m_MoveProbabilities = (real_Engine.PeekOutput(0) as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();
        using var latestBoard = (real_Engine.PeekOutput(1) as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();

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

        if (bestIndex == -1)
            return null;

        int y = bestIndex / BoardCols;
        int x = bestIndex % BoardCols;

        return new Vector2Int(x, y);
    }

    Vector2Int? GetRecommendedMoveAgg()
    {
        UpdateBoardTensor();
        UpdateLegalMovesTensor();

        real_EngineAgg.Schedule(m_Data, m_legalMoves);

        m_MoveProbabilities?.Dispose();
        m_MoveProbabilities = (real_EngineAgg.PeekOutput(0) as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();
        using var latestBoard = (real_EngineAgg.PeekOutput(1) as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();

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

        if (bestIndex == -1)
            return null;

        int y = bestIndex / BoardCols;
        int x = bestIndex % BoardCols;

        return new Vector2Int(x, y);
    }


    public void OnPieceClicked(int x, int y)
    {
        if (currentTurn != 1)
        {
            wrongStoneObj.PlayOneShot(wrongStone);
            return;
        }
        if (board[y, x] != 0) return;

        bool valid = FlipAllDirections(x, y, currentTurn, false);
        if (!valid)
        {
            Debug.Log("여기에 수를 둘 수 없습니다");
            return;
        }

        board[y, x] = currentTurn;
        FlipAllDirections(x, y, currentTurn, true);

        placeStoneSource.pitch = 1.0f;
        placeStoneSource.PlayOneShot(placeStoneClip);

        recommendedMove = null;  // 착수 후 추천 수 초기화
        UpdateBoardGraphics();

        if (currentTurn == 1)
        {
            patternTracker.RecordMove(new Vector2Int(x, y));
            NextTurn();
        }
    }


    //모델전환용 함수
    void UpdateAIModel(ModelType recommended)
    {
        if (recommended == ModelType.Aggressive && currentType != AIType.AggressiveAIType)
        {
            currentType = AIType.AggressiveAIType;
            Debug.Log("공격 모델로 전환");

        }
        else if (recommended == ModelType.Neutral && currentType != AIType.NeutralAIType)
        {
            currentType = AIType.NeutralAIType;
            Debug.Log("중립 모델로 전환");
        }
    }


    void NextTurn()
    {
        int nextTurn = -currentTurn;

        if (HasAnyValidMove(nextTurn))
        {
            currentTurn = nextTurn;

            float boardValue = EstimateBoardValue();
            //중간 연출 조건 확인 후 연출
            if (ShouldSwitchToAggressiveAI(boardValue) && currentType != AIType.AggressiveAIType)
            {
                GameBgmSource.Stop();
                NewTypeSource.PlayOneShot(NewTypeClip);
                StartCoroutine(HandleAggressiveAISwitch());
            }

            // 중간연출 조건 안 맞으면 AI 호출
            if (currentTurn == -1 && !aiScheduled)
            {
                Invoke("RequestAI", 2f);
                aiScheduled = true;
            }
        }
        else if (HasAnyValidMove(currentTurn))
        {
            Debug.Log("상대방은 착수할 수 없어 턴을 패스합니다.");
        }
        else if (!gameEnded)
        {
            Debug.Log("양쪽 모두 착수 불가 → 게임 종료");
            gameEnded = true;
            isDialoguePlaying = true;
            currentTurn = 99; //아예 안멈추게

            int black = 0, white = 0;
            for (int y = 0; y < BoardRows; y++)
            {
                for (int x = 0; x < BoardCols; x++)
                {
                    if (board[y, x] == 1) black++;
                    else if (board[y, x] == -1) white++;
                }
            }

            StartCoroutine(HandleGameOverSequence());
        }
    }
    bool CheckPlayerWin(out int blackCount, out int whiteCount)
    {
        blackCount = 0;
        whiteCount = 0;

        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                if (board[y, x] == 1) blackCount++;
                else if (board[y, x] == -1) whiteCount++;
            }
        }

        return blackCount > whiteCount;
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

    //턴 표시용
    void UpdateTurnText()
    {
        if (turnText == null) return;

        if (currentTurn == 1)
        {
            turnText.text = "당신의 차례";
        }
        else if (currentTurn == -1)
        {
            if (aiScheduled)
                turnText.text = "AI가 계산 중...";
        }
    }

    void PrintMoveProbabilities()
    {
        if (m_MoveProbabilities == null)
        {
            Debug.Log("착수 확률 정보가 없습니다.");
            return;
        }

        string boardOutput = "=== 착수 확률 (% 기준) ===\n";

        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                int index = y * BoardCols + x;
                float prob = m_MoveProbabilities[index];
                boardOutput += $"{(prob * 100f):F1}\t";
            }
            boardOutput += "\n";
        }

        Debug.Log(boardOutput);
    }

    //이전 씬 난이도 받아와서 캐릭터 생성
    void SpawnCharacterBasedOnDifficulty()
    {
        Difficulty difficulty = GameSettings.SelectedDifficulty;
        GameObject prefabToSpawn = null;

        switch (difficulty)
        {
            case Difficulty.Easy:
                prefabToSpawn = EasyMan;
                break;
            case Difficulty.Normal:
                prefabToSpawn = NormalGirl;
                break;
            case Difficulty.Hard:
                prefabToSpawn = HardMan;
                break;
        }

        if (prefabToSpawn != null)
        {
            currentCharacter = Instantiate(prefabToSpawn, CharacterPoint.position, Quaternion.identity, CharacterPoint);
        }
        else
        {
            Debug.LogWarning("난이도 설정안됨");
        }
    }

    //모델 변환 조건파악용
    bool ShouldSwitchToAggressiveAI(float boardValue)
    {
        int cornerCount = patternTracker.CountCornerMoves();
        int blackCount = 0, whiteCount = 0;

        for (int y = 0; y < BoardRows; y++)
        {
            for (int x = 0; x < BoardCols; x++)
            {
                if (board[y, x] == 1) blackCount++;
                else if (board[y, x] == -1) whiteCount++;
            }
        }

        return cornerCount >= 1 && blackCount > whiteCount && boardValue < 0f;
    }

    //중간연출용
    IEnumerator HandleAggressiveAISwitch()
    {   

        if (gameEnded) yield break;

        // 1. 턴 중지
        aiScheduled = true;
        isDialoguePlaying = true;

        // 2. 현재 AI 타입 저장
        currentType = AIType.AggressiveAIType;

        // 3. 이펙트 실행
        lightningEffect.PlayEffect();

        // 4. 이펙트 대기
        yield return new WaitForSeconds(2f);

        // 5. 대사 시작
        Difficulty difficulty = GameSettings.SelectedDifficulty;
        DialogueSequence firstDialogue = aggressiveDialogue_Normal_1;
        DialogueSequence secondDialogue = aggressiveDialogue_Normal_2;

        if (difficulty == Difficulty.Easy)
        {
            firstDialogue = aggressiveDialogue;
            secondDialogue = aggressiveDialogue_2;
        }
        else if (difficulty == Difficulty.Hard)
        {
            firstDialogue = aggressiveDialogue_Hard_1;
            secondDialogue = aggressiveDialogue_Hard_2;
        }

        dialogueManager.StartDialogue(firstDialogue);
        yield return new WaitUntil(() => dialogueManager.IsDialogueFinished());

        yield return StartCoroutine(PlayPhaseTransition());

        //캐릭터 이미지 변경
        ChangeCharacterImageToAggressive();

        dialogueManager.StartDialogue(secondDialogue);
        yield return new WaitUntil(() => dialogueManager.IsDialogueFinished());

        AggressivePhase.Play(); //2페이즈 음악

        isDialoguePlaying = false;
        // 8. AI 계속 진행
        aiScheduled = false;
        Invoke("RequestAI", 2f);
    }
    public void ChangeCharacterImageToAggressive()
    {

        if (currentCharacter == null)
        {
            Debug.LogWarning("현재 캐릭터 인스턴스가 없습니다");
            return;
        }

        var visual = currentCharacter.GetComponent<CharacterVisual>();
        if (visual != null)
        {
            Difficulty difficulty = GameSettings.SelectedDifficulty;

            Sprite chosenSprite = aggressiveSprite_Normal;
            if (difficulty == Difficulty.Easy) chosenSprite = aggressiveSprite;
            else if (difficulty == Difficulty.Hard) chosenSprite = aggressiveSprite_Hard;

            visual.ChangeToAggressive(chosenSprite);
        }
        else
        {
            Debug.LogWarning("CharacterVisual 컴포넌트를 찾지 못했습니다.");
        }
    }

    //보드평가값 리턴함수
    float EstimateBoardValue()
    {
        UpdateBoardTensor();
        UpdateLegalMovesTensor();

        real_Engine.Schedule(m_Data, m_legalMoves);
        using var latestBoard = (real_Engine.PeekOutput(1) as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();

        return latestBoard[0, 0];
    }

    public IEnumerator PlayPhaseTransition()
    {
        transitionPanel.color = new Color(1f, 1f, 1f, 0f);
        transitionPanel.gameObject.SetActive(true);

        transitionSound.PlayOneShot(transitionClip);

        yield return transitionPanel.DOFade(1f, 5.0f).SetEase(Ease.InOutQuad).WaitForCompletion();

        // 4. 정지 시간
        yield return new WaitForSeconds(0.5f);

        // 5. 빠르게 사라짐 (알파 1 → 0)
        yield return transitionPanel.DOFade(0f, 0.3f).SetEase(Ease.OutQuad).WaitForCompletion();

        // 6. 비활성화
        transitionPanel.gameObject.SetActive(false);
    }
    IEnumerator ExitToLobby()
    {
        gameOverPanel.gameObject.SetActive(true);
        gameOverPanel.alpha = 0f;

        yield return gameOverPanel.DOFade(1f, 1f).SetEase(Ease.InOutQuad).WaitForCompletion();

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene("the last revelation");
    }
    IEnumerator HandleGameOverSequence()
    {
        bool isPlayerWin = CheckPlayerWin(out int black, out int white);
        yield return StartCoroutine(ShowGameResultBounce(isPlayerWin));

        //게임 종료 대사 분기
        DialogueSequence selectedDialogue = GetGameOverDialogue(isPlayerWin);
        dialogueManager.StartDialogue(selectedDialogue);

        yield return new WaitUntil(() => dialogueManager.IsDialogueFinished());

        yield return StartCoroutine(ExitToLobby());
    }

    IEnumerator ShowGameResultBounce(bool isWin)
    {
        RectTransform target = isWin ? victoryImage : defeatImage;
        target.gameObject.SetActive(true);

        Vector2 originalPos = target.anchoredPosition;
        Vector2 startPos = originalPos + Vector2.up * 600f;
        target.anchoredPosition = startPos;

        // 떨어지면서 bounce
        yield return target.DOAnchorPosY(originalPos.y, 1.0f)
                            .SetEase(Ease.OutBounce)
                            .WaitForCompletion();

        // 잠깐 멈췄다가 사라짐
        yield return new WaitForSeconds(1.8f);

        yield return target.DOAnchorPosY(originalPos.y + 100f, 0.6f)
                            .SetEase(Ease.InBack)
                            .WaitForCompletion();

        target.gameObject.SetActive(false);
    }

    DialogueSequence GetGameOverDialogue(bool isWin)
    {
        Difficulty difficulty = GameSettings.SelectedDifficulty;

        return (difficulty, isWin) switch
        {
            (Difficulty.Easy, true) => gameOverDialogue,
            (Difficulty.Easy, false) => gameOverDialogue_Easy_Lose,
            (Difficulty.Normal, true) => gameOverDialogue_Normal_Win,
            (Difficulty.Normal, false) => gameOverDialogue_Normal_Lose,
            (Difficulty.Hard, true) => gameOverDialogue_Hard_Win,
            (Difficulty.Hard, false) => gameOverDialogue_Hard_Lose,
            _ => gameOverDialogue_Easy_Lose // fallback
        };
    }
}

