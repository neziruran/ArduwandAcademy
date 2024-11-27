using System;
using LiquidVolumeFX;
using UnityEngine;

public class PotionController : MonoBehaviour
{
    public LiquidVolume liquid;


    private void OnEnable()
    {
        EventManager.ONLevelCompleted += OnSetPotionLevel;
    }

    private void OnDisable()
    {
        EventManager.ONLevelCompleted -= OnSetPotionLevel;
    }

    private void OnSetPotionLevel()
    {
        SetLevel(0);
    }

    private void Start()
    {
        liquid = GetComponent<LiquidVolume>();
        SetLevel(0);
    }

    // Total volume of potion (from 0 to 1)
    private void SetLevel(float level)
    {
        liquid.level = Mathf.Clamp(level, 0f, 0.75f); // Ensure level is between 0 and 1
    }

    // This method will fill the potion by a specific fraction (based on ingredient size)
    public void FillPotion(float fraction)
    {
        liquid.level = Mathf.Clamp(liquid.level + fraction, 0f, 0.75f);
    }
}