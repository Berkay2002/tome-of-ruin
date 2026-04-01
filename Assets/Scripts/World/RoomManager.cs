using UnityEngine;
using Cinemachine;

public class RoomManager : MonoBehaviour
{
    [Header("Room Confiners")]
    public PolygonCollider2D[] roomConfiners;

    [Header("Camera")]
    public CinemachineConfiner confiner;

    public int CurrentRoomIndex { get; private set; } = -1;

    public void SetActiveRoom(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= roomConfiners.Length) return;

        CurrentRoomIndex = roomIndex;

        if (confiner != null)
        {
            confiner.m_BoundingShape2D = roomConfiners[roomIndex];
            confiner.InvalidatePathCache();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        for (int i = 0; i < roomConfiners.Length; i++)
        {
            if (roomConfiners[i] != null &&
                roomConfiners[i].transform.parent == transform.parent)
            {
                SetActiveRoom(i);
                return;
            }
        }
    }
}
