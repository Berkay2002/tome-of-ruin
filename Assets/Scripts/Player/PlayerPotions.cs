using UnityEngine;

public class PlayerPotions : MonoBehaviour
{
    public int potionCount;
    public int maxPotions = 5;
    public float healAmount = 30f;

    public void AddPotion()
    {
        if (potionCount < maxPotions)
            potionCount++;
    }

    public bool UsePotion()
    {
        // Stub — will be fully implemented in Task 17
        if (potionCount <= 0) return false;
        potionCount--;
        return true;
    }
}
