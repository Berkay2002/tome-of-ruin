using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AttackVisualFeedbackTests
{
    private GameObject _playerObj;
    private AttackVisualFeedback _vfx;
    private SpriteRenderer _swingArcSr;

    [SetUp]
    public void SetUp()
    {
        _playerObj = new GameObject("TestPlayer");

        var swingArcObj = new GameObject("SwingArc");
        swingArcObj.transform.SetParent(_playerObj.transform);
        swingArcObj.transform.localPosition = Vector3.zero;
        _swingArcSr = swingArcObj.AddComponent<SpriteRenderer>();
        _swingArcSr.enabled = false;

        _vfx = _playerObj.AddComponent<AttackVisualFeedback>();
        _vfx.swingArcRenderer = _swingArcSr;
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerObj != null)
            Object.DestroyImmediate(_playerObj);
    }

    [Test]
    public void SwingArc_StartsDisabled()
    {
        Assert.IsFalse(_swingArcSr.enabled);
    }

    [UnityTest]
    public IEnumerator ShowSwing_EnablesArc_ThenDisablesAfterDuration()
    {
        _vfx.ShowSwing(Vector2.right, 0.2f, 1.2f);

        // Arc should be enabled
        yield return null;
        Assert.IsTrue(_swingArcSr.enabled);

        // Wait for swing to end
        yield return new WaitForSeconds(0.25f);

        Assert.IsFalse(_swingArcSr.enabled);
    }

    [UnityTest]
    public IEnumerator ShowSwing_RotatesToFaceDirection()
    {
        _vfx.ShowSwing(Vector2.up, 0.3f, 1.2f);
        yield return null;

        // Up direction should be ~90 degrees
        float expectedAngle = 90f;
        float actualAngle = _swingArcSr.transform.localRotation.eulerAngles.z;
        Assert.AreEqual(expectedAngle, actualAngle, 1f);
    }

    [UnityTest]
    public IEnumerator ShowSwing_ScalesWithRange()
    {
        float range = 2.4f;
        float expectedScale = range / 1.2f; // 2.0
        _vfx.ShowSwing(Vector2.right, 0.3f, range);
        yield return null;

        Assert.AreEqual(expectedScale, _swingArcSr.transform.localScale.x, 0.01f);
    }

    [UnityTest]
    public IEnumerator ShowSwing_NewSwingCancelsPrevious()
    {
        _vfx.ShowSwing(Vector2.right, 0.5f, 1.2f);
        yield return new WaitForSeconds(0.1f);

        // Start new swing in different direction
        _vfx.ShowSwing(Vector2.left, 0.2f, 1.2f);
        yield return null;

        // Should be rotated for left direction (~180 degrees)
        float actualAngle = _swingArcSr.transform.localRotation.eulerAngles.z;
        Assert.AreEqual(180f, actualAngle, 1f);

        // Wait for second swing to end
        yield return new WaitForSeconds(0.25f);
        Assert.IsFalse(_swingArcSr.enabled);
    }
}
