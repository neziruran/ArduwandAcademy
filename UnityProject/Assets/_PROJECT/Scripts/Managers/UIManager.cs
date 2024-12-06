using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Image gestureImage;
    [SerializeField] private TextMeshProUGUI txtPotionName;
    [SerializeField] private LevelManager levelManager; 
    [SerializeField] private GameObject levelUI;

    private void Start()
    {
        levelUI.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        EventManager.ONLevelCompleted += OnLevelComplete;
        EventManager.ONGestureStarted += UpdatePanel;
        EventManager.ONLevelStart += OnLevelStart;
        EventManager.ONGestureCompleted += UpdatePanel;

    }
    private void OnDisable()
    {
        EventManager.ONGestureStarted -= UpdatePanel;
        EventManager.ONLevelStart -= OnLevelStart;
        EventManager.ONGestureCompleted -= UpdatePanel;
        EventManager.ONLevelCompleted -= OnLevelComplete;

    }
    
    private void OnLevelStart()
    {
        UpdatePanel();
        SetLevelPanel(false);
    }

    public void SetLevelPanel(bool isActive)
    {
        levelUI.gameObject.SetActive(isActive);
    }
    
    private void OnLevelComplete()
    {
        UpdatePanel();
        SetLevelPanel(false);
    }
    
    
    private async void UpdatePanel()
    {
        await Task.Delay(50);
        var ingredient = levelManager.GetIngredient();
        txtPotionName.SetText(ingredient.IngredientName);
        gestureImage.sprite = ingredient.İngredientImage;

    }
}
