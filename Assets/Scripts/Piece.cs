using UnityEngine;

public class Piece : MonoBehaviour
{
    //돌 프리팹에 적용할 스크립트, 클릭이벤트 리스너 역할

    public int StoneX;
    public int StoneY;

    //메인 스크립트 오브젝트로
    public OthelloGameMain game;
    
    private void OnMouseDown()
    {
        game.OnPieceClicked(StoneX, StoneY);
    }
}
