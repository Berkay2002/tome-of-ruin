using UnityEngine;

public enum PlayerState
{
    Idle,
    Moving,
    Attacking,
    Dodging,
    Hit,
    Dead
}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float dodgeSpeed = 10f;
    public float dodgeDuration = 0.3f;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    private Vector2 _moveInput;
    private Vector2 _lastFacingDirection = Vector2.down;
    private Rigidbody2D _rb;
    private float _dodgeTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void Update()
    {
        UpdateState();
    }

    private void FixedUpdate()
    {
        switch (CurrentState)
        {
            case PlayerState.Moving:
                _rb.velocity = _moveInput.normalized * moveSpeed;
                break;
            case PlayerState.Dodging:
                _rb.velocity = _lastFacingDirection * dodgeSpeed;
                _dodgeTimer -= Time.fixedDeltaTime;
                if (_dodgeTimer <= 0f)
                    CurrentState = PlayerState.Idle;
                break;
            case PlayerState.Idle:
            case PlayerState.Dead:
                _rb.velocity = Vector2.zero;
                break;
        }
    }

    public void SetMoveInput(Vector2 input)
    {
        _moveInput = input;
        if (input.sqrMagnitude > 0.01f)
            _lastFacingDirection = input.normalized;
    }

    public void UpdateState()
    {
        if (CurrentState == PlayerState.Dead) return;
        if (CurrentState == PlayerState.Dodging) return;
        if (CurrentState == PlayerState.Attacking) return;
        if (CurrentState == PlayerState.Hit) return;

        CurrentState = _moveInput.sqrMagnitude > 0.01f
            ? PlayerState.Moving
            : PlayerState.Idle;
    }

    public bool StartDodge()
    {
        if (CurrentState == PlayerState.Dodging) return false;
        if (CurrentState == PlayerState.Dead) return false;

        CurrentState = PlayerState.Dodging;
        _dodgeTimer = dodgeDuration;
        return true;
    }

    public void SetState(PlayerState state)
    {
        CurrentState = state;
    }

    public Vector2 FacingDirection => _lastFacingDirection;
}
