# AlphaBoard

## 개요
유니티 Sentis를 통해 AI와 플레이 할수 있는 환경의 오셀로 게임 프로젝트
AI는 강화학습을 적용하였으며, 클라이언트 제작은 Unity로 제작하였음.

## 실행 방법
### 프로젝트를 열 경우
1. Unity 버전 : 6000.x 이상
2. Unity 프로젝트를 옮긴 뒤 Unity Hub를 통해 실행
   
### 게임 실행
1. 폴더의 AI_Othello.exe 실행
2. 타이틀 -> 난이도 선택 -> 게임 시작
3. 패배하거나, 승리할 경우 난이도 선택 씬으로 이동
4. 게임 종료는 오셀로 게임화면에서 키보드의 ESC

## 주요 기능
- 강화학습 적용된 AI와의 오셀로 전투
- AlphaZero 스타일 강화학습 기반(PyTorch 구현)
- 공격형 / 수비형 / 기본형 전략 모델 분리 학습
- Sentis를 활용한 ONNX 모델 로컬 추론
- 쉬움, 중간, 어려움 세가지 난이도 조절

## 기술 스택
- Unity 6000.0.47f1
- C# 스크립트
- PyTorch (모델 학습)
- Unity Sentis (모델 추론 및 전환 적용)

## 학습 방식 요약
- Self-play 방식으로 데이터 생성
- MCTS 기반 수 선택, 정책/가치망 학습
- 보상 shaping(코너, 엣지, 뒤집은 수)
- 모델별 policy accuracy 및 loss 수렴 확인 

## 주요 프로젝트 디렉토리 구조
```
.
├── Assets/
│   ├── Scripts/
│   │   ├── OthelloGameMain.cs       # 게임 메인 코드, 모델 로딩 및 추론
│   │   └── Charactor*.cs            # 캐릭터 비쥬얼 및 상호작용
│   ├── Dialogue/
│   │   └── DialogueManager.cs       # 대사 관련 로직
│   ├── Models/
│   │   └── 06_08.models.onnx        # 일반형 모델
│   ├── Final_models/
│   │   ├── v26_def2.onnx            # 방어형 모델
│   │   └── v27_off2.onnx            # 공격형 모델
│   ├── Images/
│   │   └── ...                      # 게임 내 이미지 리소스
│   └── Scenes/
│       ├── MainGame.unity          # 메인 게임 (오셀로 두기)
│       ├── StartScene.unity        # 타이틀 시작 화면
│       └── thelastrevelation.unity # 로비 및 난이도 선택 화면
```
