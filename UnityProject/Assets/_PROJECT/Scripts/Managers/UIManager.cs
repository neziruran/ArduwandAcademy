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
    [SerializeField] private GameObject recipePanel;
    [SerializeField] private Image gestureImage;
    [SerializeField] private TextMeshProUGUI txtPotionName;
    [SerializeField] private LevelManager levelManager; 
    [SerializeField] private GameObject levelCompleteUI;
    [SerializeField] private TextMeshProUGUI txtWandInfo;

    private void Start()
    {
        levelCompleteUI.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        EventManager.ONLevelCompleted += OnLevelComplete;
        EventManager.ONLevelStart += OnLevelStart;
        EventManager.ONGestureCompleted += UpdatePanel;

    }
    private void OnDisable()
    {
        EventManager.ONLevelStart -= OnLevelStart;
        EventManager.ONGestureCompleted -= UpdatePanel;
        EventManager.ONLevelCompleted -= OnLevelComplete;

    }
    
    private void OnLevelStart()
    {
        UpdatePanel();
        SetLevelPanel(false);
        SetWandInfo(0);
    }

    public void SetWandInfo(int operation)
    {
        switch (operation)
        {
            case 0:
                txtWandInfo.SetText("");
                break;
            case 1:
                StartCoroutine(SetFirstText());
                break;
            case 2:
                txtWandInfo.SetText("Wave your wand up / down");
                break;
            case 3:
                txtWandInfo.SetText("Wave your wand in a circle");
                break;
            default:
                txtWandInfo.SetText("");
                break;
                
        }
    }

    private IEnumerator SetFirstText()
    {
        txtWandInfo.SetText("Wave your wand to finalize the spell");
        yield return new WaitForSeconds(2f);
        txtWandInfo.SetText("Wave your wand left right");
    }

    public void SetLevelPanel(bool isActive)
    {
        levelCompleteUI.gameObject.SetActive(isActive);
    }
    public void SetRecipePanel(bool isActive)
    {
        recipePanel.gameObject.SetActive(isActive);
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
