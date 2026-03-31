using UnityEngine;

public enum PickupType
{
    Attack,
    ComboBook,
    HealthPotion,
    Key
}

public class ItemPickup : MonoBehaviour
{
    public PickupType pickupType;
    public AttackData attackData;
    public ComboBookData comboBookData;
    public string keyId;
    public float healAmount = 25f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        switch (pickupType)
        {
            case PickupType.Attack:
                var inv = other.GetComponent<PlayerInventory>();
                if (inv != null && attackData != null) inv.AddAttack(attackData);
                break;

            case PickupType.ComboBook:
                var inv2 = other.GetComponent<PlayerInventory>();
                if (inv2 != null && comboBookData != null) inv2.AddComboBook(comboBookData);
                break;

            case PickupType.HealthPotion:
                var potions = other.GetComponent<PlayerPotions>();
                if (potions != null) potions.AddPotion();
                break;

            case PickupType.Key:
                if (GameManager.Instance != null)
                    GameManager.Instance.State.CollectKey(keyId);
                break;
        }

        Destroy(gameObject);
    }
}
