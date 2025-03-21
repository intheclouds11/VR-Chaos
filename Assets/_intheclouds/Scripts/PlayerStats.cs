using System;
using TMPro;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField]
    private int startingHealth = 3;
    
    [SerializeField]
    private int baseDamage = 1;
    
    [SerializeField]
    private TextMeshProUGUI healthIndicator;

    public int CurrentHealth {get; private set; }
    private bool highFiveBuff;

    private void Awake()
    {
        CurrentHealth = startingHealth;
    }

    private void Reset()
    {
        CurrentHealth = startingHealth;
    }

    private void TakeDamage(int damage)
    {
        var newHealth = CurrentHealth - damage;

        if (newHealth > 0)
        {
            CurrentHealth = newHealth;
            // play hurtSFX
        }
        else
        {
            CurrentHealth = 0; // GorillaMovement stops movement
            // play diedSFX
            // delay for 3 seconds, then respawn
        }
    }
}
