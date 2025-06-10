using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    public SpriteRenderer rendererToChange;
    public Vector3 defaultScale = Vector3.one;

    public void ChangeToAggressive(Sprite aggressiveSprite)
    {
        if (rendererToChange != null)
        {
            rendererToChange.sprite = aggressiveSprite;
            transform.localScale = defaultScale; 
        }
    }
}
