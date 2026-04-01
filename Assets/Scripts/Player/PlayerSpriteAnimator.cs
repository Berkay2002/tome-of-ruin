using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlayerController))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float walkFrameRate = 8f;
    public float attackFrameRate = 12f;
    public float dodgeFrameRate = 10f;

    [Header("Sprites - Idle (one per direction)")]
    public Sprite idleDown;
    public Sprite idleUp;
    public Sprite idleLeft;
    public Sprite idleDownLeft;
    public Sprite idleUpLeft;

    [Header("Sprites - Moving (4 frames per direction)")]
    public Sprite[] movingDown = new Sprite[4];
    public Sprite[] movingUp = new Sprite[4];
    public Sprite[] movingLeft = new Sprite[4];
    public Sprite[] movingDownLeft = new Sprite[4];
    public Sprite[] movingUpLeft = new Sprite[4];

    [Header("Sprites - Attacking (3 frames per direction)")]
    public Sprite[] attackingDown = new Sprite[3];
    public Sprite[] attackingUp = new Sprite[3];
    public Sprite[] attackingLeft = new Sprite[3];
    public Sprite[] attackingDownLeft = new Sprite[3];
    public Sprite[] attackingUpLeft = new Sprite[3];

    [Header("Sprites - Dodging (2 frames per direction)")]
    public Sprite[] dodgingDown = new Sprite[2];
    public Sprite[] dodgingUp = new Sprite[2];
    public Sprite[] dodgingLeft = new Sprite[2];
    public Sprite[] dodgingDownLeft = new Sprite[2];
    public Sprite[] dodgingUpLeft = new Sprite[2];

    [Header("Sprites - Hit (one per direction)")]
    public Sprite hitDown;
    public Sprite hitUp;
    public Sprite hitLeft;
    public Sprite hitDownLeft;
    public Sprite hitUpLeft;

    [Header("Sprites - Dead")]
    public Sprite dead;

    private SpriteRenderer _sr;
    private PlayerController _controller;
    private float _frameTimer;
    private int _currentFrame;
    private PlayerState _lastState;
    private Direction8 _lastDirection;

    private enum Direction8
    {
        Down,
        Up,
        Left,
        Right,
        DownLeft,
        DownRight,
        UpLeft,
        UpRight
    }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _controller = GetComponent<PlayerController>();
    }

    private void LateUpdate()
    {
        var state = _controller.CurrentState;
        var dir = GetDirection8(_controller.FacingDirection);

        // Reset frame on state or direction change
        if (state != _lastState || dir != _lastDirection)
        {
            _currentFrame = 0;
            _frameTimer = 0f;
            _lastState = state;
            _lastDirection = dir;
        }

        // Handle mirroring for right-facing directions
        bool mirror = dir == Direction8.Right || dir == Direction8.DownRight || dir == Direction8.UpRight;
        _sr.flipX = mirror;

        switch (state)
        {
            case PlayerState.Idle:
                _sr.sprite = GetIdleSprite(dir);
                break;
            case PlayerState.Moving:
                AnimateLoop(GetMovingSprites(dir), walkFrameRate);
                break;
            case PlayerState.Attacking:
                AnimateOnce(GetAttackingSprites(dir), attackFrameRate);
                break;
            case PlayerState.Dodging:
                AnimateOnce(GetDodgingSprites(dir), dodgeFrameRate);
                break;
            case PlayerState.Hit:
                _sr.sprite = GetHitSprite(dir);
                break;
            case PlayerState.Dead:
                _sr.sprite = dead;
                break;
        }
    }

    private void AnimateLoop(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) return;

        _frameTimer += Time.deltaTime;
        if (_frameTimer >= 1f / fps)
        {
            _frameTimer -= 1f / fps;
            _currentFrame = (_currentFrame + 1) % frames.Length;
        }

        var sprite = frames[_currentFrame];
        if (sprite != null) _sr.sprite = sprite;
    }

    private void AnimateOnce(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) return;

        _frameTimer += Time.deltaTime;
        if (_frameTimer >= 1f / fps && _currentFrame < frames.Length - 1)
        {
            _frameTimer -= 1f / fps;
            _currentFrame++;
        }

        var sprite = frames[_currentFrame];
        if (sprite != null) _sr.sprite = sprite;
    }

    private Direction8 GetDirection8(Vector2 facing)
    {
        if (facing.sqrMagnitude < 0.01f) return Direction8.Down;

        float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        // Normalize to 0-360
        if (angle < 0) angle += 360f;

        // 8 directions, 45 degree slices
        if (angle >= 337.5f || angle < 22.5f) return Direction8.Right;
        if (angle >= 22.5f && angle < 67.5f) return Direction8.UpRight;
        if (angle >= 67.5f && angle < 112.5f) return Direction8.Up;
        if (angle >= 112.5f && angle < 157.5f) return Direction8.UpLeft;
        if (angle >= 157.5f && angle < 202.5f) return Direction8.Left;
        if (angle >= 202.5f && angle < 247.5f) return Direction8.DownLeft;
        if (angle >= 247.5f && angle < 292.5f) return Direction8.Down;
        return Direction8.DownRight;
    }

    // Mirror right-side directions to their left equivalents
    private Direction8 ToLeftEquivalent(Direction8 dir)
    {
        switch (dir)
        {
            case Direction8.Right: return Direction8.Left;
            case Direction8.DownRight: return Direction8.DownLeft;
            case Direction8.UpRight: return Direction8.UpLeft;
            default: return dir;
        }
    }

    private Sprite GetIdleSprite(Direction8 dir)
    {
        switch (ToLeftEquivalent(dir))
        {
            case Direction8.Down: return idleDown;
            case Direction8.Up: return idleUp;
            case Direction8.Left: return idleLeft;
            case Direction8.DownLeft: return idleDownLeft;
            case Direction8.UpLeft: return idleUpLeft;
            default: return idleDown;
        }
    }

    private Sprite[] GetMovingSprites(Direction8 dir)
    {
        switch (ToLeftEquivalent(dir))
        {
            case Direction8.Down: return movingDown;
            case Direction8.Up: return movingUp;
            case Direction8.Left: return movingLeft;
            case Direction8.DownLeft: return movingDownLeft;
            case Direction8.UpLeft: return movingUpLeft;
            default: return movingDown;
        }
    }

    private Sprite[] GetAttackingSprites(Direction8 dir)
    {
        switch (ToLeftEquivalent(dir))
        {
            case Direction8.Down: return attackingDown;
            case Direction8.Up: return attackingUp;
            case Direction8.Left: return attackingLeft;
            case Direction8.DownLeft: return attackingDownLeft;
            case Direction8.UpLeft: return attackingUpLeft;
            default: return attackingDown;
        }
    }

    private Sprite[] GetDodgingSprites(Direction8 dir)
    {
        switch (ToLeftEquivalent(dir))
        {
            case Direction8.Down: return dodgingDown;
            case Direction8.Up: return dodgingUp;
            case Direction8.Left: return dodgingLeft;
            case Direction8.DownLeft: return dodgingDownLeft;
            case Direction8.UpLeft: return dodgingUpLeft;
            default: return dodgingDown;
        }
    }

    private Sprite GetHitSprite(Direction8 dir)
    {
        switch (ToLeftEquivalent(dir))
        {
            case Direction8.Down: return hitDown;
            case Direction8.Up: return hitUp;
            case Direction8.Left: return hitLeft;
            case Direction8.DownLeft: return hitDownLeft;
            case Direction8.UpLeft: return hitUpLeft;
            default: return hitDown;
        }
    }
}
