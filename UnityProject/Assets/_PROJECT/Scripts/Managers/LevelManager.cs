using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [System.Serializable]
    public class Level
    {
        public Potion potion;
        public float requiredHoldTime;
        public int currentIngredient;
    }

    [SerializeField] private List<Level> levels = new List<Level>();
    [SerializeField] private int currentLevelIndex = 0;
    [SerializeField] private PotionController currentPotionController;

    [Header("Level Display")]
    [SerializeField] private TextMeshProUGUI txtTargetGesture;
    [SerializeField] private TextMeshProUGUI txtPotionName;


    private void Start()
    {
        UpdateRecipePanel();
        currentLevelIndex = 0;
        EventManager.OnLevelStart();
        SpawnPotion(); // Spawn potion when level starts
    }

    private void SpawnPotion()
    {
        var currentLevel = GetCurrentLevel();
        if (currentLevel != null && currentLevel.potion != null)
        {
            GameObject potionInstance = Instantiate(currentLevel.potion.potionPrefab, Vector3.zero, Quaternion.identity);
            currentPotionController = potionInstance.GetComponent<PotionController>();
        }
        else
        {
            Debug.LogError("No potion prefab found for the current level.");
        }
    }

    public PotionController GetCurrentPotion()
    {
        return currentPotionController; // Return the potion controller of the current level's potion
    }

    private void UpdateRecipePanel()
    {
        var currentLevel = GetCurrentLevel();

        if (currentLevel.currentIngredient < currentLevel.potion.requiredIngredients.Count)
        {
            txtTargetGesture.SetText(currentLevel.potion.requiredIngredients[currentLevel.currentIngredient]);
        }
        else
        {
            Debug.LogError("Current ingredient index is out of bounds.");
        }
        
        txtPotionName.SetText(currentLevel.potion.name);
    }

    private void OnEnable()
    {
        EventManager.ONGestureCompleted += OnIngredientComplete;
        EventManager.ONLevelCompleted += OnLevelCompleted;
    }

    private void OnLevelCompleted()
    {
        Debug.Log("level completed");
        var currentlevel = GetCurrentLevel();
        currentlevel.currentIngredient = 0;
    }

    private void OnDisable()
    {
        EventManager.ONGestureCompleted -= OnIngredientComplete;
    }

    public Level GetCurrentLevel()
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Count)
        {
            Debug.LogError("Invalid level index.");
            return null;
        }
        return levels[currentLevelIndex];
    }

  

    private void OnIngredientComplete()
    {
        var level = GetCurrentLevel();
        if (level != null)
        {
            Debug.Log($"potion count is {level.potion.requiredIngredients.Count} ing count is {level.currentIngredient}");

            if (level.potion.requiredIngredients.Count == level.currentIngredient + 1)
            {
                currentLevelIndex++;
                Debug.Log("Congrats Potion Completed");
                EventManager.OnLevelCompleted();
                txtTargetGesture.SetText(level.potion.requiredIngredients[currentLevelIndex]);

                Debug.LogError("level completed");
            }
            else
            {
                level.currentIngredient++;
            }
            UpdateRecipePanel();
        }
    }
    

    public float GetFillTime()
    {
        return GetCurrentLevel()?.requiredHoldTime ?? 0f;
    }

    public string GetCurrentRecipeItem()
    {
        var level = GetCurrentLevel();
        if (level != null && level.potion.requiredIngredients.Count > level.currentIngredient)
        {
            return level.potion.requiredIngredients[level.currentIngredient];
        }
        return string.Empty;
    }
}
