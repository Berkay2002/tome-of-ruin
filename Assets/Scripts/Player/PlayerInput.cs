using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerInput : MonoBehaviour
{
    private PlayerController _controller;
    private PlayerCombat _combat;
    private PlayerPotions _potions;
    private ComboBookUI _comboBookUI;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _combat = GetComponent<PlayerCombat>();
        _potions = GetComponent<PlayerPotions>();
    }

    private void Start()
    {
        _comboBookUI = FindObjectOfType<ComboBookUI>();
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _controller.SetMoveInput(new Vector2(h, v));

        if (Input.GetButtonDown("Fire1"))
        {
            _combat.TryAttack();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _combat.TryDodgeCancel();
            _controller.StartDodge();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (_potions != null) _potions.UsePotion();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (_comboBookUI != null) _comboBookUI.Toggle();
        }
    }
}
