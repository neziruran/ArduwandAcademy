using System;
using UnityEngine;
using UnityEngine.UI;

public class GestureRecognizer : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private UDPReceiver receiver;
    [SerializeField] private Image recognitionBar;

    [Header("Settings")]
    [SerializeField] public bool isActive;
    [SerializeField] private bool catchingGesture;
    
    private float currentFillTime = 0f;

    private void OnEnable()
    {
        EventManager.ONLevelCompleted += ResetBar;
    }

    private void OnDisable()
    {
        EventManager.ONLevelCompleted -= ResetBar;
    }

    private void Start()
    {
        if (levelManager == null)
        {
            Debug.LogError("Level Manager is null");
        }
        if (recognitionBar == null)
        {
            Debug.LogError("Recognition Bar is not set");
        }

        ResetBar();
    }

    private void Update()
    {
        if (!isActive) return;
        
        var currentGesture = receiver.GetGesture();
        var targetGesture = levelManager.GetIngredient();

        // If the gesture matches, start filling the bar
        if (string.Equals(currentGesture, targetGesture.IngredientName, StringComparison.CurrentCultureIgnoreCase))
        {
            catchingGesture = true;
            FillBar(levelManager.GetFillTime());
        }
        else
        {
            if (catchingGesture)
            {
                // Gesture failed, reset the bar
                ResetBar();
                catchingGesture = false;
                EventManager.OnGestureFail();
                Debug.LogWarning("Gesture catching failed");
            }
        }
    }

    private void FillBar(float fillDuration)
    {
        if (catchingGesture)
        {
            // Time passed while holding the gesture
            currentFillTime += Time.deltaTime;
            
            // Update the fill bar based on the time passed
            recognitionBar.fillAmount = currentFillTime / fillDuration;

            // Determine the potion ingredient fraction to fill
            var level = levelManager.GetCurrentLevel();
            float ingredientFraction = 1f / level.potion.requiredIngredients.Count; // Divide potion into equal ingredient fractions
            
            // Fill the potion liquid based on the current ingredient fraction
            levelManager.GetPotionController().FillPotion(ingredientFraction * Time.deltaTime / fillDuration); // Incrementally fill based on time

            // If the fill time exceeds the fill duration, stop the gesture process and update the potion and bar
            if (currentFillTime >= fillDuration)
            {
                recognitionBar.fillAmount = 1f; // Make sure the bar is completely filled
                catchingGesture = false; // Stop catching the gesture
                EventManager.OnGestureCompleted();
                ResetBar();
            }
        }
    }

    private void ResetBar()
    {
        recognitionBar.fillAmount = 0f; // Reset the bar to empty
        currentFillTime = 0f; // Reset the timer
    }
}
