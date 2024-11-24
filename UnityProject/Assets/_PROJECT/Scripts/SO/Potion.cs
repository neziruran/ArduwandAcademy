using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PotionRecipe", menuName = "Potion/Recipe")]
public class Potion : ScriptableObject
{
    public GameObject potionPrefab;
    public List<string> requiredIngredients;
}