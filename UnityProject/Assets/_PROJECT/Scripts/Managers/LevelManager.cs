using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
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
    [SerializeField] public int currentWand;
    [SerializeField] private PotionController currentPotionController;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private UDPReceiver udpReceiver;
    [SerializeField] private WekinatorReceiver wekinatorReceiver;
    [SerializeField] private GestureRecognizer recognizer;

    private void Start()
    {
        currentWand = 1;
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
        EventManager.ONWandPerformed += OnWandPerformed;
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
        udpReceiver.IsActive = true;
        recognizer.isActive = true;

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DebugWand(currentWand);
            currentWand++;
            uiManager.SetWandInfo(currentWand);
            if (currentWand == 4)
            {
                DOVirtual.DelayedCall(2f, (() =>
                {
                    ParticleManager.Instance.PlayLevelComplete(Vector3.zero);

                }));
                DOVirtual.DelayedCall(3f, (() =>
                {
                    currentWand = 0;
                    currentLevelIndex++;
                    uiManager.SetWandInfo(0); // deactivate info
                    uiManager.SetLevelPanel(true);
                }));
            }
        }
    }

    private void OnWandPerformed()
    {
        currentWand++;
        uiManager.SetWandInfo(currentWand);
        if (currentWand == 4)
        {
            DOVirtual.DelayedCall(2f, (() =>
            {
                ParticleManager.Instance.PlayLevelComplete(Vector3.zero);

            }));
            DOVirtual.DelayedCall(3f, (() =>
            {
                currentWand = 0;
                currentLevelIndex++;
                uiManager.SetWandInfo(0); // deactivate info
                uiManager.SetLevelPanel(true);
            }));
            
        }
    }

    private void DebugWand(int operation)
    {
        wekinatorReceiver.HandleGesture(operation);
    }
    
    private void OnIngredientComplete()
    {
        var level = GetCurrentLevel();
        if (level == null) return;
        
        if (level.potion.requiredIngredients.Count == level.currentIngredient + 1)
        {
            
            uiManager.SetRecipePanel(false);
            uiManager.SetWandInfo(1);
            udpReceiver.IsActive = false;
            recognizer.isActive = false;
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
