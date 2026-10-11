using System.Collections;
using UnityEngine;

// Classic screen-by-screen rooms: the camera frames one room at a time and slides to the next
// when the player walks through a doorway. Only the current room's monsters are active.
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    public int rows = 3;
    public int cols = 3;
    public Vector2 roomSize = new Vector2(24, 14);
    public float transitionTime = 0.7f;
    public float walkInDistance = 2f;
    // Room index (row * cols + col) that holds the boss; it gets the boss music. -1 = none.
    public int bossRoomIndex = -1;

    public PlayerController player;
    public Camera cam;
    // Monster containers, indexed row * cols + col.
    public Transform[] roomMonsters;

    public Vector2Int Current { get; private set; } // x = column, y = row
    public bool Transitioning { get; private set; }
    public bool[] Visited { get; private set; }

    void Awake()
    {
        Instance = this;
        Visited = new bool[rows * cols];
    }

    void Start()
    {
        Current = RoomAt(player.transform.position);
        cam.transform.position = RoomCenter(Current);
        for (int i = 0; i < roomMonsters.Length; i++)
            SetRoomActive(i, i == Index(Current));
        Visited[Index(Current)] = true;
        AudioManager.SetBossMusic(Index(Current) == bossRoomIndex);
    }

    void Update()
    {
        if (Transitioning || player == null) return;
        var room = RoomAt(player.transform.position);
        if (room != Current && InWorld(room))
            StartCoroutine(Transition(room));
    }

    IEnumerator Transition(Vector2Int next)
    {
        Transitioning = true;
        player.Frozen = true;

        var rb = player.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        SetRoomActive(Index(Current), false);
        foreach (var fireball in FindObjectsByType<Fireball>()) Destroy(fireball.gameObject);
        foreach (var beam in FindObjectsByType<SwordBeam>()) Destroy(beam.gameObject);

        Vector2 step = new Vector2(next.x - Current.x, Current.y - next.y); // rows grow downward
        Vector3 camFrom = cam.transform.position;
        Vector3 camTo = RoomCenter(next);
        Vector2 playerFrom = rb.position;
        Vector2 playerTo = playerFrom + step * walkInDistance;

        for (float t = 0f; t < transitionTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / transitionTime);
            cam.transform.position = Vector3.Lerp(camFrom, camTo, k);
            rb.position = Vector2.Lerp(playerFrom, playerTo, k);
            yield return null;
        }
        cam.transform.position = camTo;
        rb.position = playerTo;

        Current = next;
        Visited[Index(next)] = true;
        SetRoomActive(Index(next), true);
        if (!Boss.Defeated) AudioManager.SetBossMusic(Index(next) == bossRoomIndex);

        rb.bodyType = RigidbodyType2D.Dynamic;
        player.Frozen = false;
        Transitioning = false;
    }

    void SetRoomActive(int index, bool active)
    {
        if (index < 0 || index >= roomMonsters.Length || roomMonsters[index] == null) return;
        foreach (Transform child in roomMonsters[index])
        {
            if (active)
            {
                var enemy = child.GetComponent<Enemy>();
                if (enemy != null) enemy.ResetToSpawn();
                var boss = child.GetComponent<Boss>();
                if (boss != null) boss.ResetToSpawn();
            }
            child.gameObject.SetActive(active);
        }
    }

    public Vector2Int RoomAt(Vector2 pos)
    {
        int col = Mathf.FloorToInt((pos.x + roomSize.x / 2f) / roomSize.x);
        int row = Mathf.FloorToInt((-pos.y + roomSize.y / 2f) / roomSize.y);
        return new Vector2Int(col, row);
    }

    public Vector3 RoomCenter(Vector2Int room)
    {
        return new Vector3(room.x * roomSize.x, -room.y * roomSize.y, cam.transform.position.z);
    }

    public int Index(Vector2Int room) => room.y * cols + room.x;

    bool InWorld(Vector2Int room) => room.x >= 0 && room.x < cols && room.y >= 0 && room.y < rows;
}
