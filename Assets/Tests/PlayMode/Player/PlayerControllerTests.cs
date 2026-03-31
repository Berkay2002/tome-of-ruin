using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerControllerTests
{
    private GameObject _playerObj;
    private PlayerController _player;

    [SetUp]
    public void SetUp()
    {
        _playerObj = new GameObject("Player");
        _playerObj.AddComponent<Rigidbody2D>();
        _playerObj.AddComponent<BoxCollider2D>();
        _player = _playerObj.AddComponent<PlayerController>();
        _player.moveSpeed = 5f;
        _player.dodgeSpeed = 10f;
        _player.dodgeDuration = 0.3f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_playerObj);
    }

    [Test]
    public void Player_StartsInIdleState()
    {
        Assert.AreEqual(PlayerState.Idle, _player.CurrentState);
    }

    [Test]
    public void Player_SetMoveInput_ChangesToMovingState()
    {
        _player.SetMoveInput(new Vector2(1, 0));
        _player.UpdateState();
        Assert.AreEqual(PlayerState.Moving, _player.CurrentState);
    }

    [Test]
    public void Player_ZeroMoveInput_ReturnsToIdle()
    {
        _player.SetMoveInput(new Vector2(1, 0));
        _player.UpdateState();
        _player.SetMoveInput(Vector2.zero);
        _player.UpdateState();
        Assert.AreEqual(PlayerState.Idle, _player.CurrentState);
    }

    [Test]
    public void Player_Dodge_EntersDodgingState()
    {
        _player.SetMoveInput(new Vector2(1, 0));
        _player.StartDodge();
        Assert.AreEqual(PlayerState.Dodging, _player.CurrentState);
    }

    [Test]
    public void Player_CannotDodge_WhenAlreadyDodging()
    {
        _player.SetMoveInput(new Vector2(1, 0));
        _player.StartDodge();
        bool result = _player.StartDodge();
        Assert.IsFalse(result);
    }

    [Test]
    public void Player_CannotMove_WhenDead()
    {
        _player.SetState(PlayerState.Dead);
        _player.SetMoveInput(new Vector2(1, 0));
        _player.UpdateState();
        Assert.AreEqual(PlayerState.Dead, _player.CurrentState);
    }

    [Test]
    public void PlayerHealth_TakeDamage_ReducesHealth()
    {
        var health = _playerObj.AddComponent<PlayerHealth>();
        health.maxHealth = 100f;
        health.Init();

        health.TakeDamage(25f);

        Assert.AreEqual(75f, health.CurrentHealth, 0.001f);
    }

    [Test]
    public void PlayerHealth_TakeLethalDamage_SetsDeadState()
    {
        var health = _playerObj.AddComponent<PlayerHealth>();
        health.maxHealth = 100f;
        health.Init();

        health.TakeDamage(150f);

        Assert.AreEqual(0f, health.CurrentHealth, 0.001f);
        Assert.AreEqual(PlayerState.Dead, _player.CurrentState);
    }

    [Test]
    public void PlayerHealth_CannotTakeDamage_WhenDodging()
    {
        var health = _playerObj.AddComponent<PlayerHealth>();
        health.maxHealth = 100f;
        health.Init();

        _player.SetMoveInput(new Vector2(1, 0));
        _player.StartDodge();

        health.TakeDamage(50f);

        Assert.AreEqual(100f, health.CurrentHealth, 0.001f);
    }

    [Test]
    public void PlayerHealth_Heal_ClampsToMax()
    {
        var health = _playerObj.AddComponent<PlayerHealth>();
        health.maxHealth = 100f;
        health.Init();

        health.TakeDamage(20f);
        health.Heal(50f);

        Assert.AreEqual(100f, health.CurrentHealth, 0.001f);
    }
}
