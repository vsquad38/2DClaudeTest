using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

// Sets up the world for the current level when the game scene loads:
// makes the landscape look more dangerous the higher the level, moves the boss to a random corner room (its lair),
// hides the Crystal Sword in one random spot outside the lair, and adds extra monsters to each room.
// Runs before RoomManager.Start, while every room is still active.
[DefaultExecutionOrder(-100)]
public class LevelDirector : MonoBehaviour
{
    public RoomManager rooms;
    public Tilemap ground;
    public Tilemap walls;
    public int maxExtraMonstersPerRoom = 3;
    public float minSpawnDistanceFromPlayer = 4f;

    [Header("Dangerous landscape")]
    public TileBase grassTile;
    public TileBase scorchedGrassTile;   // level 4+
    public TileBase bushTile;
    public TileBase thornBushTile;       // level 3+
    public Sprite crackSprite;           // level 2+
    public Sprite bonesSprite;           // level 2+
    public Sprite skullSprite;           // level 3+
    public Sprite[] emberFrames;         // level 4+

    // The landscape grows darker and redder each level: day, parched, dusk, ashen, blood moon, deep blood moon.
    static readonly Color[] LevelTints =
    {
        Color.white,
        new Color(0.97f, 0.9f, 0.78f),
        new Color(0.88f, 0.78f, 0.78f),
        new Color(0.8f, 0.66f, 0.62f),
        new Color(0.78f, 0.55f, 0.55f),
        new Color(0.72f, 0.48f, 0.5f),
    };

    void Start()
    {
        int level = GameSession.Level;
        ApplyLandscape(level);
        PlaceBoss();
        HideSword();
        AddExtraMonsters(level);
        ScatterDecor(level);
        GameHUD.ShowBanner($"Level {level}", level == 1 ? "Find the boss and defeat it!" : "The land grows darker. The monsters grow stronger...");
    }

    void ApplyLandscape(int level)
    {
        var tint = LevelTints[Mathf.Min(level, LevelTints.Length) - 1];
        if (ground != null) ground.color = tint;
        if (walls != null) walls.color = Color.Lerp(Color.white, tint, 0.7f);

        // Bushes wither into thorns, then the grass is scorched.
        if (level >= 3 && walls != null && bushTile != null && thornBushTile != null)
        {
            walls.SwapTile(bushTile, thornBushTile);
            var player = rooms != null ? rooms.player : null;
            if (player != null) player.bushTile = thornBushTile; // Crystal Sword bolts burn thorns too
        }
        if (level >= 4 && ground != null && grassTile != null && scorchedGrassTile != null)
            ground.SwapTile(grassTile, scorchedGrassTile);

        // The light dims from level 4.
        var light = FindAnyObjectByType<Light2D>();
        if (light != null && light.lightType == Light2D.LightType.Global)
            light.intensity *= level >= 6 ? 0.75f : level == 5 ? 0.82f : level == 4 ? 0.9f : 1f;
    }

    // Cracks and bones from level 2, skulls from level 3, glowing ember cracks from level 4; more each level.
    // Purely decorative (no collision), on open ground away from walls, bushes and water.
    void ScatterDecor(int level)
    {
        if (level < 2 || rooms == null || walls == null || ground == null) return;

        int cracks = Mathf.Min(8, level);
        int bones = Mathf.Min(4, level / 2);
        int skulls = level >= 3 ? Mathf.Min(3, level - 2) : 0;
        int embers = level >= 4 ? Mathf.Min(6, (level - 3) * 2) : 0;

        var parent = new GameObject("Decor").transform;
        var used = new HashSet<Vector3Int>();
        int roomW = Mathf.RoundToInt(rooms.roomSize.x), roomH = Mathf.RoundToInt(rooms.roomSize.y);

        for (int room = 0; room < rooms.rows * rooms.cols; room++)
        {
            // Interior cells of this room (inside the outer wall).
            int originX = (room % rooms.cols) * roomW - roomW / 2;
            int originY = -(room / rooms.cols) * roomH + roomH / 2 - 1;
            Vector3Int RandomCell() => new Vector3Int(originX + Random.Range(1, roomW - 1), originY - Random.Range(1, roomH - 1), 0);

            Place(cracks, crackSprite, null);
            Place(bones, bonesSprite, null);
            Place(skulls, skullSprite, null);
            Place(embers, null, emberFrames);

            void Place(int count, Sprite sprite, Sprite[] frames)
            {
                if (sprite == null && (frames == null || frames.Length == 0)) return;
                for (int i = 0; i < count; i++)
                {
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        var cell = RandomCell();
                        if (used.Contains(cell) || walls.HasTile(cell) || !ground.HasTile(cell)) continue;
                        used.Add(cell);
                        var go = new GameObject("Decor");
                        go.transform.SetParent(parent, false);
                        go.transform.position = ground.GetCellCenterWorld(cell);
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = sprite != null ? sprite : frames[0];
                        sr.sortingOrder = 1; // above the ground, below characters
                        sr.flipX = Random.value < 0.5f;
                        if (frames != null && frames.Length > 1)
                        {
                            var anim = go.AddComponent<SpriteAnimator>();
                            anim.frames = frames;
                            anim.fps = 5f;
                        }
                        break;
                    }
                }
            }
        }
    }

    // The boss's lair is a random corner room (never next to the central starting room), different from last level.
    // Regular monsters in the lair are cleared out.
    void PlaceBoss()
    {
        var boss = FindAnyObjectByType<Boss>(FindObjectsInactive.Include);
        if (boss == null || rooms == null) return;

        int[] corners =
        {
            0,
            rooms.cols - 1,
            (rooms.rows - 1) * rooms.cols,
            rooms.rows * rooms.cols - 1,
        };
        var choices = corners.Distinct().Where(r => r != GameSession.LastBossRoom).ToList();
        if (choices.Count == 0) choices = corners.Distinct().ToList();
        int lair = choices[Random.Range(0, choices.Count)];
        GameSession.LastBossRoom = lair;

        var container = rooms.roomMonsters[lair];
        foreach (Transform child in container)
            if (child.GetComponent<Enemy>() != null) Destroy(child.gameObject);

        // Room bounds, kept a bit further from the walls because the boss is about two tiles wide.
        var center = new Vector2((lair % rooms.cols) * rooms.roomSize.x, -(lair / rooms.cols) * rooms.roomSize.y);
        var floor = new Rect(center.x - rooms.roomSize.x / 2f + 2.25f, center.y - rooms.roomSize.y / 2f + 2.25f,
            rooms.roomSize.x - 4.5f, rooms.roomSize.y - 4.5f);

        boss.transform.SetParent(container, true);
        boss.Relocate(FindOpenSpot(center, 1.2f), floor);
        rooms.bossRoomIndex = lair;
    }

    // Nearest point to `near` (spiralling outward) where a circle of `radius` touches no wall, bush or water.
    static Vector2 FindOpenSpot(Vector2 near, float radius)
    {
        Physics2D.SyncTransforms();
        for (float r = 0f; r <= 6f; r += 0.5f)
        {
            int steps = r == 0f ? 1 : Mathf.CeilToInt(2f * Mathf.PI * r / 0.5f);
            for (int i = 0; i < steps; i++)
            {
                float a = 2f * Mathf.PI * i / steps;
                var p = near + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                bool blocked = false;
                foreach (var hit in Physics2D.OverlapCircleAll(p, radius))
                    if (hit.attachedRigidbody != null && hit.attachedRigidbody.bodyType == RigidbodyType2D.Static) { blocked = true; break; }
                if (!blocked) return p;
            }
        }
        return near;
    }

    // Every hiding spot has a SwordPickup; keep one at random (not in the boss's lair, not the same spot as last level).
    void HideSword()
    {
        var lair = rooms != null && rooms.bossRoomIndex >= 0 ? rooms.roomMonsters[rooms.bossRoomIndex] : null;
        var spots = FindObjectsByType<SwordPickup>(FindObjectsInactive.Include)
            .OrderBy(s => s.transform.position.x).ThenBy(s => s.transform.position.y)
            .ToList();
        if (spots.Count == 0) return;

        var choices = Enumerable.Range(0, spots.Count)
            .Where(i => spots[i].transform.parent != lair)
            .Where(i => i != GameSession.LastSwordSpot || spots.Count == 1)
            .ToList();
        if (choices.Count == 0) choices = Enumerable.Range(0, spots.Count).ToList();
        int keep = choices[Random.Range(0, choices.Count)];
        GameSession.LastSwordSpot = keep;

        for (int i = 0; i < spots.Count; i++)
            if (i != keep) Destroy(spots[i].gameObject);
    }

    // Level 2 adds one extra monster per room, level 3 two, level 4+ three (copies of monsters already in the room).
    void AddExtraMonsters(int level)
    {
        int extra = Mathf.Min(maxExtraMonstersPerRoom, level - 1);
        if (extra <= 0 || rooms == null) return;

        Physics2D.SyncTransforms();
        Vector2 playerPos = rooms.player != null ? (Vector2)rooms.player.transform.position : Vector2.zero;

        for (int r = 0; r < rooms.roomMonsters.Length; r++)
        {
            var container = rooms.roomMonsters[r];
            if (container == null || r == rooms.bossRoomIndex) continue;

            var residents = new List<Enemy>();
            foreach (Transform child in container)
            {
                var enemy = child.GetComponent<Enemy>();
                if (enemy != null) residents.Add(enemy);
            }
            if (residents.Count == 0) continue;

            for (int i = 0; i < extra; i++)
            {
                var source = residents[Random.Range(0, residents.Count)];
                if (TryFindSpawnPoint(source.bounds, playerPos, out var pos))
                    Instantiate(source.gameObject, pos, Quaternion.identity, container).name = source.name;
            }
        }
    }

    bool TryFindSpawnPoint(Rect area, Vector2 playerPos, out Vector2 pos)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            pos = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
            if (Vector2.Distance(pos, playerPos) < minSpawnDistanceFromPlayer) continue;
            if (Physics2D.OverlapCircle(pos, 0.5f) != null) continue; // wall, bush, water or another monster
            return true;
        }
        pos = default;
        return false;
    }
}
