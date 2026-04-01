using NUnit.Framework;
using UnityEngine;

public class RoomManagerTests
{
    [Test]
    public void SetActiveRoom_UpdatesCurrentRoomIndex()
    {
        var go = new GameObject("RoomManager");
        var roomManager = go.AddComponent<RoomManager>();

        var room0 = new GameObject("Room0");
        var collider0 = room0.AddComponent<PolygonCollider2D>();
        var room1 = new GameObject("Room1");
        var collider1 = room1.AddComponent<PolygonCollider2D>();

        roomManager.roomConfiners = new PolygonCollider2D[] { collider0, collider1 };
        roomManager.SetActiveRoom(1);

        Assert.AreEqual(1, roomManager.CurrentRoomIndex);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(room0);
        Object.DestroyImmediate(room1);
    }

    [Test]
    public void SetActiveRoom_InvalidIndex_DoesNotChange()
    {
        var go = new GameObject("RoomManager");
        var roomManager = go.AddComponent<RoomManager>();

        var room0 = new GameObject("Room0");
        var collider0 = room0.AddComponent<PolygonCollider2D>();
        roomManager.roomConfiners = new PolygonCollider2D[] { collider0 };
        roomManager.SetActiveRoom(0);

        roomManager.SetActiveRoom(5);
        Assert.AreEqual(0, roomManager.CurrentRoomIndex);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(room0);
    }
}
