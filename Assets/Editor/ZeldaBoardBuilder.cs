using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Tools > Build Zelda Board: generates pixel-art sprites and builds the top-down level in the open scene.
public static class ZeldaBoardBuilder
{
    const string ArtDir = "Assets/Art";
    const string PrefabDir = "Assets/Prefabs";
    const int PPU = 16;

    // '#' wall, 'b' bush, '.' grass, 'P' player, 's' slime (wanders), 'v' bat (erratic), 'm' moblin (chases)
    static readonly string[] Map =
    {
        "########################",
        "#.........b............#",
        "#..b...........s....b..#",
        "#......##.........##...#",
        "#..v...##....b....##.m.#",
        "#..........bbb.........#",
        "#...b......bbb.....s...#",
        "#......................#",
        "#.....s.........v......#",
        "#...##.............b...#",
        "#...##....P............#",
        "#..........#####...m...#",
        "#..b..v..........s...b.#",
        "########################",
    };

    static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
    {
        { 'G', new Color32(60, 160, 60, 255) },   // green
        { 'g', new Color32(30, 100, 30, 255) },   // dark green
        { 'd', new Color32(20, 80, 30, 255) },    // bush outline
        { 'l', new Color32(110, 200, 90, 255) },  // leaf highlight
        { 'H', new Color32(140, 90, 40, 255) },   // hair
        { 'S', new Color32(240, 200, 150, 255) }, // skin
        { 'K', new Color32(20, 20, 20, 255) },    // black
        { 'M', new Color32(200, 100, 90, 255) },  // mouth
        { 'B', new Color32(100, 60, 30, 255) },   // brown
        { 'Y', new Color32(230, 200, 40, 255) },  // gold
        { 'W', new Color32(240, 240, 240, 255) }, // white
        { 'w', new Color32(120, 130, 140, 255) }, // steel outline
        { 'L', new Color32(210, 225, 240, 255) }, // steel
        { 'R', new Color32(210, 50, 50, 255) },   // red
        { 'r', new Color32(255, 150, 150, 255) }, // red highlight
        { 'D', new Color32(130, 20, 20, 255) },   // dark red
        { 'P', new Color32(130, 60, 170, 255) },  // purple
        { 'p', new Color32(80, 30, 110, 255) },   // dark purple
        { 'O', new Color32(220, 130, 50, 255) },  // orange
        { 'o', new Color32(150, 80, 30, 255) },   // dark orange
        { 'E', new Color32(90, 90, 100, 255) },   // empty heart grey
    };

    static readonly string[] PlayerArt =
    {
        "......GGGG......",
        "....GGGGGGGG....",
        "...GGGGGGGGGGG..",
        "...HHGGGGGGHH...",
        "...HSSSSSSSSH...",
        "...SSKSSSSKSS...",
        "...SSSSSSSSSS...",
        "....SSSMMSSS....",
        "..gGGGGGGGGGGg..",
        ".SgGGGGGGGGGGgS.",
        ".SgGGBBBBBBGGgS.",
        "..gGGGGYYGGGGg..",
        "...GGGGGGGGGG...",
        "...GGGG..GGGG...",
        "...BBB....BBB...",
        "..BBBB....BBBB..",
    };

    static readonly string[] SwordArt =
    {
        ".......ww.......",
        "......wLLw......",
        "......wLLw......",
        "......wLLw......",
        "......wLLw......",
        "......wLLw......",
        "......wLLw......",
        "......wLLw......",
        "......wLLw......",
        "......wLLw......",
        "...YYYYYYYYYY...",
        "...YoooooooooY..",
        ".......BB.......",
        ".......BB.......",
        ".......BB.......",
        ".......YY.......",
    };

    static readonly string[] SlimeArt =
    {
        "................",
        "................",
        "................",
        "......RRRR......",
        "....RRRRRRRR....",
        "...RRrRRRRRRR...",
        "..RRrrRRRRRRRR..",
        "..RRRRRRRRRRRR..",
        ".RRWWKRRRRWWKRR.",
        ".RRWWKRRRRWWKRR.",
        ".RRRRRRRRRRRRRR.",
        ".RRRRRRRRRRRRRR.",
        "DRRRRRRRRRRRRRRD",
        "DDRRRRRRRRRRRRDD",
        ".DDDDDDDDDDDDDD.",
        "................",
    };

    static readonly string[] BatArt =
    {
        "................",
        "................",
        "................",
        "P..............P",
        "PP...P....P...PP",
        "PPP..PPPPPP..PPP",
        "PPPPPPPPPPPPPPPP",
        "pPPPPPYPPYPPPPPp",
        ".pPPPPPPPPPPPPp.",
        "..pp.PPWWPP.pp..",
        "......PPPP......",
        ".......pp.......",
        "................",
        "................",
        "................",
        "................",
    };

    static readonly string[] MoblinArt =
    {
        "....oo....oo....",
        "....oOOOOOOo....",
        "...OOOOOOOOOO...",
        "..OOOKKOOKKOOO..",
        "..OOOYKOOKYOOO..",
        "..OOOOOOOOOOOO..",
        "...OOooooooOO...",
        "...OOWoOOoWOO...",
        "....OOOOOOOO....",
        "..BBBBBBBBBBBB..",
        ".OBBBBBBBBBBBBO.",
        ".OBBBYYYYYBBBBO.",
        "..BBBBBBBBBBBB..",
        "...BBBB..BBBB...",
        "...ooo....ooo...",
        "..oooo....oooo..",
    };

    static readonly string[] BushArt =
    {
        "................",
        ".....dddddd.....",
        "...ddGGGGGGdd...",
        "..dGGGlGGGGGGd..",
        ".dGGlllGGGlGGGd.",
        ".dGGGlGGGllGGGd.",
        "dGGGGGGGGGlGGGGd",
        "dGGlGGGGGGGGGGGd",
        "dGlllGGGGGlGGGGd",
        "dGGlGGGGGlllGGGd",
        ".dGGGGGlGGlGGGd.",
        ".dGGGGlllGGGGGd.",
        "..dGGGGlGGGGGd..",
        "...ddGGGGGGdd...",
        ".....dddddd.....",
        "................",
    };

    static readonly string[] HeartArt =
    {
        "................",
        "..RRRR....RRRR..",
        ".RRrrRR..RRRRRR.",
        ".RrrRRRRRRRRRRR.",
        ".RrRRRRRRRRRRRR.",
        ".RRRRRRRRRRRRRR.",
        ".RRRRRRRRRRRRRR.",
        "..RRRRRRRRRRRR..",
        "...RRRRRRRRRR...",
        "....RRRRRRRR....",
        ".....RRRRRR.....",
        "......RRRR......",
        ".......RR.......",
        "................",
        "................",
        "................",
    };

    // ---------- Animation frames ----------
    // Bodies use rows 0-12; legs (rows 13-15) are swapped in per frame by WithLegs().

    static readonly string[] PlayerUpArt =
    {
        "......GGGG......",
        "....GGGGGGGG....",
        "...GGGGGGGGGG...",
        "...GGGGGGGGGG...",
        "...HHHHHHHHHH...",
        "...HHHHHHHHHH...",
        "...HHHHHHHHHH...",
        "....HHHHHHHH....",
        "..gGGGGGGGGGGg..",
        ".SgGGGGGGGGGGgS.",
        ".SgGGGGGGGGGGgS.",
        "..gGGBBBBBBGGg..",
        "...GGGGGGGGGG...",
    };

    static readonly string[] PlayerSideArt =
    {
        ".....GGGG.......",
        "...GGGGGGGG.....",
        "..GGGGGGGGGG....",
        ".GGHHHGGGGG.....",
        "..GHHSSSSSS.....",
        "..HHSSSSKSS.....",
        "..HHSSSSSSSS....",
        "...HSSSSSSM.....",
        "...gGGGGGGGg....",
        "...gGGGGGGGgS...",
        "...gGBBBBBBgS...",
        "...gGGGYYGGg....",
        "....GGGGGGGG....",
    };

    static readonly string[][] FrontLegs =
    {
        new[] { "...GGGG..GGGG...", "...BBB....BBB...", "..BBBB....BBBB.." }, // stand
        new[] { "...GGGG..GGGG...", "..BBBB....BBB...", "..........BBBB.." }, // walk A
        new[] { "...GGGG..GGGG...", "...BBB....BBBB..", "..BBBB.........." }, // walk B
    };

    static readonly string[][] SideLegs =
    {
        new[] { "....GGG.GGG.....", "....BBB.BBB.....", "....BBBBBBBB...." },
        new[] { "...GGG...GGG....", "..BBB.....BBB...", "..BB.......BBB.." },
        new[] { "....GGGGGG......", ".....BBBB.......", ".....BBBBB......" },
    };

    static readonly string[][] MoblinLegs =
    {
        new[] { "...BBBB..BBBB...", "...ooo....ooo...", "..oooo....oooo.." },
        new[] { "...BBBB..BBBB...", "..oooo....ooo...", "..........oooo.." },
        new[] { "...BBBB..BBBB...", "...ooo....oooo..", "..oooo.........." },
    };

    static readonly string[] BatWingsDownArt =
    {
        "................",
        "................",
        "................",
        "................",
        "................",
        ".....P....P.....",
        "....PPPPPPPP....",
        "..PPPPYPPYPPPP..",
        ".PPPPPPPPPPPPPP.",
        "PPPpPPPWWPPPpPPP",
        "PP...PPPPPP...PP",
        "P......pp......P",
        "................",
        "................",
        "................",
        "................",
    };

    static string[] WithLegs(string[] body, string[] legs)
    {
        var rows = new string[16];
        for (int i = 0; i < 13; i++) rows[i] = i < body.Length ? body[i] : "";
        for (int i = 0; i < 3; i++) rows[13 + i] = legs[i];
        return rows;
    }

    // Stand, walk A, walk B, attack (stand nudged 1px toward the facing direction).
    static Sprite[] PlayerFrames(string name, string[] body, string[][] legs, int attackDx, int attackDy)
    {
        var stand = FromArt(WithLegs(body, legs[0]));
        return new[]
        {
            SaveSprite($"Player_{name}_Stand", stand),
            SaveSprite($"Player_{name}_WalkA", FromArt(WithLegs(body, legs[1]))),
            SaveSprite($"Player_{name}_WalkB", FromArt(WithLegs(body, legs[2]))),
            SaveSprite($"Player_{name}_Attack", Shift(stand, attackDx, attackDy)),
        };
    }

    [MenuItem("Tools/Build Zelda Board")]
    public static void Build()
    {
        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory(PrefabDir);

        // --- Sprites ---
        var playerDown = PlayerFrames("Down", PlayerArt, FrontLegs, 0, -1);
        var playerUp = PlayerFrames("Up", PlayerUpArt, FrontLegs, 0, 1);
        var playerSide = PlayerFrames("Side", PlayerSideArt, SideLegs, 1, 0);
        var swordSprite = SaveSprite("Sword", FromArt(SwordArt));
        var slimeTex = FromArt(SlimeArt);
        var slimeFrames = new[] { SaveSprite("Slime", slimeTex), SaveSprite("Slime_Squash", Squash(slimeTex, 1.12f, 0.82f)) };
        var batFrames = new[] { SaveSprite("Bat", FromArt(BatArt)), SaveSprite("Bat_Flap", FromArt(BatWingsDownArt)) };
        var moblinStand = SaveSprite("Moblin", FromArt(WithLegs(MoblinArt, MoblinLegs[0])));
        var moblinFrames = new[]
        {
            SaveSprite("Moblin_WalkA", FromArt(WithLegs(MoblinArt, MoblinLegs[1]))),
            moblinStand,
            SaveSprite("Moblin_WalkB", FromArt(WithLegs(MoblinArt, MoblinLegs[2]))),
            moblinStand,
        };
        var bushSprite = SaveSprite("Bush", FromArt(BushArt));
        var heartSprite = SaveSprite("Heart", FromArt(HeartArt));
        var heartEmptySprite = SaveSprite("HeartEmpty", FromArt(HeartArt, 'E'));
        var grassSprite = SaveSprite("Grass", GrassTexture());
        var wallSprite = SaveSprite("Wall", WallTexture());

        // --- Tiles ---
        var grassTile = SaveTile("GrassTile", grassSprite, Tile.ColliderType.None);
        var wallTile = SaveTile("WallTile", wallSprite, Tile.ColliderType.Grid);
        var bushTile = SaveTile("BushTile", bushSprite, Tile.ColliderType.Sprite);

        // --- Clean up anything from a previous build ---
        foreach (var name in new[] { "Board", "Player", "Monsters", "HUD", "Audio" })
        {
            var old = GameObject.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }

        // --- Board (tilemaps) ---
        var board = new GameObject("Board", typeof(Grid));
        Undo.RegisterCreatedObjectUndo(board, "Build Zelda Board");

        var ground = CreateTilemap(board.transform, "Ground", 0);
        var walls = CreateTilemap(board.transform, "Walls", 1);
        var wallBody = walls.gameObject.AddComponent<Rigidbody2D>();
        wallBody.bodyType = RigidbodyType2D.Static;
        var tilemapCollider = walls.gameObject.AddComponent<TilemapCollider2D>();
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        walls.gameObject.AddComponent<CompositeCollider2D>();

        int rows = Map.Length;
        int cols = Map[0].Length;
        var spawns = new List<(char c, Vector3 pos)>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                char ch = c < Map[r].Length ? Map[r][c] : '#';
                var cell = new Vector3Int(c - cols / 2, rows / 2 - 1 - r, 0);
                ground.SetTile(cell, grassTile);
                if (ch == '#') walls.SetTile(cell, wallTile);
                else if (ch == 'b') walls.SetTile(cell, bushTile);
                else if (ch != '.') spawns.Add((ch, new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0)));
            }
        }

        // --- Heart pickup prefab (dropped by monsters) ---
        var heartGo = new GameObject("HeartPickup");
        heartGo.transform.localScale = Vector3.one * 0.6f;
        var heartSr = heartGo.AddComponent<SpriteRenderer>();
        heartSr.sprite = heartSprite;
        heartSr.sortingOrder = 4;
        heartGo.AddComponent<CircleCollider2D>().isTrigger = true;
        heartGo.AddComponent<HeartPickup>();
        var heartPrefab = PrefabUtility.SaveAsPrefabAsset(heartGo, PrefabDir + "/HeartPickup.prefab");
        Object.DestroyImmediate(heartGo);

        // --- Player & monsters ---
        GameObject player = null;
        var monsters = new GameObject("Monsters");
        Undo.RegisterCreatedObjectUndo(monsters, "Build Zelda Board");

        foreach (var (ch, pos) in spawns)
        {
            switch (ch)
            {
                case 'P':
                    player = CreatePlayer(pos, playerDown, playerUp, playerSide, swordSprite);
                    break;
                case 's':
                    CreateMonster("Slime", pos, slimeFrames, 3f, monsters.transform, Enemy.Behaviour.Wander, 1.2f, 2, heartPrefab);
                    break;
                case 'v':
                    CreateMonster("Bat", pos, batFrames, 10f, monsters.transform, Enemy.Behaviour.Erratic, 2.5f, 1, heartPrefab);
                    break;
                case 'm':
                    CreateMonster("Moblin", pos, moblinFrames, 6f, monsters.transform, Enemy.Behaviour.Chase, 2f, 3, heartPrefab);
                    break;
            }
        }

        // --- Camera: frame the whole board ---
        var cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Build Zelda Board");
            Undo.RecordObject(cam.transform, "Build Zelda Board");
            cam.orthographic = true;
            cam.orthographicSize = rows / 2f + 0.2f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
        }

        // --- HUD ---
        var hud = new GameObject("HUD");
        Undo.RegisterCreatedObjectUndo(hud, "Build Zelda Board");
        var hudComp = hud.AddComponent<GameHUD>();
        hudComp.playerHealth = player != null ? player.GetComponent<Health>() : null;
        hudComp.heartFull = heartSprite.texture;
        hudComp.heartEmpty = heartEmptySprite.texture;

        // --- Audio ---
        var clips = ChiptuneSynth.GenerateAll();
        var audio = new GameObject("Audio");
        Undo.RegisterCreatedObjectUndo(audio, "Build Zelda Board");
        var am = audio.AddComponent<AudioManager>();
        am.music = clips.music;
        am.swing = clips.swing;
        am.hit = clips.hit;
        am.enemyDie = clips.enemyDie;
        am.hurt = clips.hurt;
        am.pickup = clips.pickup;
        am.victory = clips.victory;
        am.gameOver = clips.gameOver;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"Zelda board built: {spawns.Count} spawns. Press Play! Move with WASD/arrows, attack with Space/J.");
    }

    static GameObject CreatePlayer(Vector3 pos, Sprite[] down, Sprite[] up, Sprite[] side, Sprite swordSprite)
    {
        var go = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(go, "Build Zelda Board");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = down[0];
        sr.sortingOrder = 5;

        var anim = go.AddComponent<PlayerAnimator>();
        anim.down = down;
        anim.up = up;
        anim.side = side;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.7f, 0.8f);
        col.offset = new Vector2(0, -0.05f);

        var health = go.AddComponent<Health>();
        health.maxHealth = 5;
        health.invulnerableTime = 1f;
        health.stunTime = 0.2f;

        var swordGo = new GameObject("Sword");
        swordGo.transform.SetParent(go.transform, false);
        var swordSr = swordGo.AddComponent<SpriteRenderer>();
        swordSr.sprite = swordSprite;
        swordSr.sortingOrder = 6;
        var swordCol = swordGo.AddComponent<BoxCollider2D>();
        swordCol.isTrigger = true;
        swordCol.size = new Vector2(0.5f, 1f);
        var hitbox = swordGo.AddComponent<SwordHitbox>();
        swordGo.SetActive(false);

        var controller = go.AddComponent<PlayerController>();
        controller.sword = hitbox;
        return go;
    }

    static void CreateMonster(string name, Vector3 pos, Sprite[] frames, float fps, Transform parent,
        Enemy.Behaviour behaviour, float speed, int hp, GameObject drop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = 5;

        var animator = go.AddComponent<SpriteAnimator>();
        animator.frames = frames;
        animator.fps = fps;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.linearDamping = 0;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        var health = go.AddComponent<Health>();
        health.maxHealth = hp;
        health.invulnerableTime = 0.3f;
        health.stunTime = 0.25f;

        var enemy = go.AddComponent<Enemy>();
        enemy.behaviour = behaviour;
        enemy.moveSpeed = speed;
        enemy.dropOnDeath = drop;
    }

    static Tilemap CreateTilemap(Transform parent, string name, int order)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = order;
        return go.GetComponent<Tilemap>();
    }

    // --- Pixel art helpers ---

    static Texture2D FromArt(string[] art, char recolorAllTo = '\0')
    {
        var tex = NewTexture();
        for (int y = 0; y < 16; y++)
        {
            string row = y < art.Length ? art[y] : "";
            for (int x = 0; x < 16; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                Color32 c = new Color32(0, 0, 0, 0);
                if (ch != '.' && Palette.TryGetValue(ch, out var pc))
                    c = recolorAllTo != '\0' ? Palette[recolorAllTo] : pc;
                tex.SetPixel(x, 15 - y, c); // art rows are top-down
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D Shift(Texture2D src, int dx, int dy)
    {
        var tex = NewTexture();
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int sx = x - dx, sy = y - dy;
                bool inside = sx >= 0 && sx < 16 && sy >= 0 && sy < 16;
                tex.SetPixel(x, y, inside ? src.GetPixel(sx, sy) : Color.clear);
            }
        tex.Apply();
        return tex;
    }

    // Squash-and-stretch: scale around the bottom centre (nearest-neighbour).
    static Texture2D Squash(Texture2D src, float scaleX, float scaleY)
    {
        var tex = NewTexture();
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int sx = Mathf.FloorToInt(8f + (x + 0.5f - 8f) / scaleX);
                int sy = Mathf.FloorToInt((y + 0.5f) / scaleY);
                bool inside = sx >= 0 && sx < 16 && sy >= 0 && sy < 16;
                tex.SetPixel(x, y, inside ? src.GetPixel(sx, sy) : Color.clear);
            }
        tex.Apply();
        return tex;
    }

    static Texture2D GrassTexture()
    {
        var tex = NewTexture();
        var rng = new System.Random(7);
        var baseCol = new Color32(88, 176, 80, 255);
        var dark = new Color32(66, 146, 64, 255);
        var light = new Color32(120, 200, 100, 255);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int n = rng.Next(100);
                tex.SetPixel(x, y, n < 8 ? dark : n < 12 ? light : baseCol);
            }
        // a couple of grass tufts
        foreach (var (x, y) in new[] { (3, 4), (11, 10) })
        {
            tex.SetPixel(x, y, dark); tex.SetPixel(x + 1, y + 1, dark); tex.SetPixel(x + 2, y, dark);
        }
        tex.Apply();
        return tex;
    }

    static Texture2D WallTexture()
    {
        var tex = NewTexture();
        var stone = new Color32(130, 120, 110, 255);
        var hi = new Color32(165, 155, 140, 255);
        var mortar = new Color32(70, 62, 58, 255);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int row = y / 4;
                int offset = (row % 2) * 4;
                bool isMortar = y % 4 == 0 || (x + offset) % 8 == 0;
                bool isHighlight = !isMortar && (y % 4 == 3);
                tex.SetPixel(x, y, isMortar ? mortar : isHighlight ? hi : stone);
            }
        tex.Apply();
        return tex;
    }

    static Texture2D NewTexture()
    {
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        return tex;
    }

    static Sprite SaveSprite(string name, Texture2D tex)
    {
        string path = $"{ArtDir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true; // HUD draws the heart textures directly
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Tile SaveTile(string name, Sprite sprite, Tile.ColliderType colliderType)
    {
        string path = $"{ArtDir}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        AssetDatabase.SaveAssets();
        return tile;
    }
}
