﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    [SerializeField] private List<Level> levels = new();
    [SerializeField] private int currentLevelIndex;
    [SerializeField] private PotionController currentPotionController;
    [SerializeField] private UIManager uıManager;
    [SerializeField] private UDPReceiver receiver;

    private void Start()
    {
        currentLevelIndex = 0;
        EventManager.OnLevelStart();
        StartGame();
    }

    private void StartGame()
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

    private Potion GetCurrentPotion()
    {
        return levels[currentLevelIndex].potion;
    }
    
    public PotionController GetPotionController()
    {
        return currentPotionController; 
    }
    private void OnEnable()
    {
        EventManager.ONGestureCompleted += OnIngredientComplete;
        EventManager.ONLevelCompleted += OnLevelCompleted;
    }

    private void OnLevelCompleted()
    {
        var currentLevel = GetCurrentLevel();
        currentLevel.currentIngredient = 0;
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


    public async void GetNextLevel()
    {
        EventManager.OnLevelCompleted();
        await Task.Delay(50);
        receiver.IsActive = true;

    }
  

    private void OnIngredientComplete()
    {
        var level = GetCurrentLevel();
        if (level == null) return;
        
        if (level.potion.requiredIngredients.Count == level.currentIngredient + 1)
        {
            currentLevelIndex++;
            uıManager.SetLevelPanel(true);
            ParticleManager.Instance.PlayLevelComplete(Vector3.zero);
            receiver.IsActive = false;

        }
        else
        {
            ParticleManager.Instance.PlayCorrectIngredientEffect(Vector3.zero);
            level.currentIngredient++;
        }
    }
    public float GetFillTime()
    {
        return GetCurrentLevel()?.requiredHoldTime ?? 0f;
    }

    public Ingredient GetIngredient()
    {
        var potion = GetCurrentPotion();
        var currentIngredient = GetCurrentLevel().currentIngredient;
        if (potion == null || GetCurrentLevel() == null)
            Debug.LogError("Something went wrong");
        return potion.requiredIngredients[currentIngredient];
    }
}
