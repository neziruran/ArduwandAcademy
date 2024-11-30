using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Potion Recipe", menuName = "Ingredient")]
public class Ingredient : ScriptableObject
{
    [SerializeField] private string ingredientName;
    [SerializeField] private Sprite ingredientImage;

    public Sprite İngredientImage => ingredientImage;
    public string IngredientName => ingredientName;
}