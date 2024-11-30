using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Potion Recipe", menuName = "Potion")]
public class Potion : ScriptableObject
{
    public GameObject potionPrefab;
    public List<Ingredient> requiredIngredients;
}