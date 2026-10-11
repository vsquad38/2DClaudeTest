using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    // The world is a grid of rooms, each exactly one screen (RoomW x RoomH tiles).
    // Each layout below is a room's 22x12 interior; the outer wall is added automatically, with a
    // 2-tile doorway cut in the middle of every side that touches another room.
    // '#' wall, 'b' bush, 'w' water, '.' grass, 'P' player start,
    // 's' slime (wanders), 'v' bat (erratic), 'm' moblin (chases), 'B' boss, 'S' hidden upgraded sword
    const int RoomW = 24;
    const int RoomH = 14;
    const int WorldCols = 3;
    const int WorldRows = 3;

    static readonly string[][] Rooms =
    {
        // Row 0, col 0 — the woods (the Crystal Sword is hidden in the top-right nook, behind the bushes)
        new[]
        {
            "bb.bb.bb..........bbbS",
            "b.................b.b.",
            "..bb....s.....bb....b.",
            "..bb..........bb..s.b.",
            "........bb............",
            "..s.....bb.......bb...",
            "................bbb...",
            "..bb.......s..........",
            "..bb..........bb......",
            ".........v....bb...b..",
            "b...bb...............b",
            "bb..bb.....b...bb...bb",
        },
        // Row 0, col 1 — the ruins
        new[]
        {
            "......................",
            "..##..............##..",
            "..##......m.......##..",
            "......................",
            "......##......##......",
            "......##......##......",
            "..........s...........",
            "......##......##......",
            "......##......##......",
            "......................",
            "..##........v.....##..",
            "..##..............##..",
        },
        // Row 0, col 2 — the lake (sword nook in the top-right corner behind the bushes)
        new[]
        {
            "....................bS",
            "...wwwwww...........b.",
            "..wwwwwwwww.....v...b.",
            "..wwwwwwwww...........",
            "...wwwwwww......bb....",
            "....wwww........bb....",
            "......................",
            ".........v............",
            "..............wwww....",
            ".............wwwwww...",
            "....s.........wwww....",
            "......................",
        },
        // Row 1, col 0 — the hedge maze (sword nook in the hedge's top-left corner)
        new[]
        {
            "......................",
            ".bbbbbbb....bbbbbbbb..",
            ".bS.................b.",
            ".b..s.....bb.....v..b.",
            ".b........bb........b.",
            "......................",
            "......................",
            ".b........bb........b.",
            ".b..v.....bb.....s..b.",
            ".b..................b.",
            ".bbbbbbb....bbbbbbbb..",
            "......................",
        },
        // Row 1, col 1 — the start
        new[]
        {
            "......................",
            "..b................b..",
            "......................",
            "......##......##......",
            "......##......##......",
            "......................",
            "..........P...........",
            "......................",
            "......##......##......",
            "......##......##......",
            "..b.....s..........b..",
            "......................",
        },
        // Row 1, col 2 — the moblin camp
        new[]
        {
            "......................",
            "..########..########..",
            "..#................#..",
            "..#..m.........m...#..",
            "..#................#..",
            "..........bb..........",
            "..........bb..........",
            "..#................#..",
            "..#.......s........#..",
            "..#................#..",
            "..########..########..",
            "......................",
        },
        // Row 2, col 0 — the graveyard (sword nook in the bottom-left corner behind a tombstone).
        // Tombstone rows are two tiles apart so there's comfortable room to walk between them.
        new[]
        {
            "......................",
            "..#...#.......#...#...",
            "......................",
            "......................",
            "..#...#...#...#...#...",
            "........v.............",
            "..........s...........",
            "..#...#.......#...#...",
            "..............v.......",
            "......................",
            ".#....#...#...#...#...",
            "S#....................",
        },
        // Row 2, col 1 — the river crossing (sword nook in the bottom-right corner behind the bushes)
        new[]
        {
            "......................",
            "..b...........s....b..",
            "......................",
            "wwwwwwwwww..wwwwwwwwww",
            "wwwwwwwwww..wwwwwwwwww",
            "......................",
            "......m...............",
            "......................",
            "..bb..............bb..",
            "..bb......v.......bb..",
            "....................b.",
            "....................bS",
        },
        // Row 2, col 2 — the arena. 'B' is only the boss's starting point: each level the LevelDirector moves
        // the boss to a random corner room and clears that room's regular monsters.
        new[]
        {
            "......................",
            "......................",
            "...##............##...",
            "...##....m.......##...",
            "......................",
            "..........B...........",
            "...............v......",
            "......................",
            "...##............##...",
            "...##.....s......##...",
            "......................",
            "......................",
        },
    };

    // Character at (x, y) of room (row, col), including the generated outer wall and doorways.
    static char RoomChar(int row, int col, int x, int y)
    {
        bool doorUp = row > 0, doorDown = row < WorldRows - 1;
        bool doorLeft = col > 0, doorRight = col < WorldCols - 1;
        bool doorCol = x == RoomW / 2 - 1 || x == RoomW / 2;
        bool doorRow = y == RoomH / 2 - 1 || y == RoomH / 2;

        // Outer wall, with doorways. The first few tiles inside each doorway are kept clear too, so the
        // room transition (which walks the player in) never drops them inside a wall or bush.
        const int clearDepth = 3;
        if (y <= clearDepth && doorUp && doorCol) return '.';
        if (y >= RoomH - 1 - clearDepth && doorDown && doorCol) return '.';
        if (x <= clearDepth && doorLeft && doorRow) return '.';
        if (x >= RoomW - 1 - clearDepth && doorRight && doorRow) return '.';
        if (x == 0 || y == 0 || x == RoomW - 1 || y == RoomH - 1) return '#';

        string[] layout = Rooms[row * WorldCols + col];
        int ix = x - 1, iy = y - 1;
        string line = iy < layout.Length ? layout[iy] : "";
        return ix < line.Length ? line[ix] : '.';
    }

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
        { 'N', new Color32(225, 215, 190, 255) }, // bone
        { 'n', new Color32(150, 140, 120, 255) }, // bone shadow
        { 'k', new Color32(45, 30, 25, 255) },    // crack / scorch
        { 'C', new Color32(140, 235, 255, 255) }, // crystal
        { 'c', new Color32(40, 120, 200, 255) },  // crystal outline
    };

    static readonly string[] CrystalSwordArt =
    {
        ".......cc.......",
        "......cCWc......",
        "......cCWc......",
        "......cCWc......",
        "......cCWc......",
        "......cCWc......",
        "......cCWc......",
        "......cCWc......",
        "......cCWc......",
        "......cCCc......",
        "..PPPPPPPPPPPP..",
        "..pPPPPYYPPPPp..",
        ".......BB.......",
        ".......BB.......",
        ".......BB.......",
        "......YYYY......",
    };

    static readonly string[] BossArt =
    {
        "..K..........K..",
        ".KK..........KK.",
        ".KRK........KRK.",
        "..KRKKKKKKKKRK..",
        "...KRRRRRRRRK...",
        "..KRRRRRRRRRRK..",
        ".KRRYYRRRRYYRRK.",
        ".KRRYKRRRRKYRRK.",
        ".KRRRRRRRRRRRRK.",
        ".KRRRDDDDDDRRRK.",
        "..KRDWDWWDWDRK..",
        "..KRDDDDDDDDRK..",
        "...KRRRRRRRRK...",
        "..KKRRKKKKRRKK..",
        ".KRRK......KRRK.",
        ".KKK........KKK.",
    };

    // Mouth wide open (wind-up / breathing fire).
    static readonly string[] BossAttackArt =
    {
        "..K..........K..",
        ".KK..........KK.",
        ".KRK........KRK.",
        "..KRKKKKKKKKRK..",
        "...KRRRRRRRRK...",
        "..KRRRRRRRRRRK..",
        ".KRRYYRRRRYYRRK.",
        ".KRRYYRRRRYYRRK.",
        ".KRRDDDDDDDDRRK.",
        "..KDWDWWDWWDWDK.",
        "..KDDDDOODDDDDK.",
        "..KDWDWDDWDWDK..",
        "...KRRRRRRRRK...",
        "..KKRRKKKKRRKK..",
        ".KRRK......KRRK.",
        ".KKK........KKK.",
    };

    // Second walk frame: feet swapped.
    static readonly string[] BossStepArt =
    {
        "..K..........K..",
        ".KK..........KK.",
        ".KRK........KRK.",
        "..KRKKKKKKKKRK..",
        "...KRRRRRRRRK...",
        "..KRRRRRRRRRRK..",
        ".KRRYYRRRRYYRRK.",
        ".KRRYKRRRRKYRRK.",
        ".KRRRRRRRRRRRRK.",
        ".KRRRDDDDDDRRRK.",
        "..KRDWDWWDWDRK..",
        "..KRDDDDDDDDRK..",
        "...KRRRRRRRRK...",
        "..KKRRKKKKRRKK..",
        "..KRRK....KRRK..",
        "..KKK......KKK..",
    };

    // Lightning bolt fired from the sword at full health (points up; rotated to the swing direction).
    static readonly string[] BoltArt =
    {
        "..........YW....",
        ".........YW.....",
        "........YW......",
        ".......YWW......",
        "......YWWYYYY...",
        "..........YW....",
        ".........YW.....",
        "........YWW.....",
        ".......YWWYYYY..",
        "...........YW...",
        "..........YW....",
        ".........YW.....",
        "........YW......",
        ".......Y........",
        "................",
        "................",
    };

    // The Crystal Sword's bolt: thicker, crystal-blue with a white-hot core and a dark outline.
    static readonly string[] CrystalBoltArt =
    {
        "..........cWc...",
        ".........cWWc...",
        "........cWWc....",
        ".......cWWCc....",
        "......cWWCCCCc..",
        ".....ccccCWWc...",
        "........cWWc....",
        ".......cWWCc....",
        "......cWWCCCCc..",
        ".....ccccCWWc...",
        "........cWWc....",
        ".......cWWc.....",
        "......cWc.......",
        ".....cc.........",
        "................",
        "................",
    };

    static readonly string[] FlameArt =
    {
        "................",
        "................",
        ".......O........",
        "......OYO.......",
        ".....OYYO.......",
        "....OYYWYO......",
        "....OYWWYO..O...",
        "...OYYWWYYO.OO..",
        "...OYWWWWYO.OYO.",
        "..OYYWWWWYYOOYO.",
        "..OYWWWWWWYYYO..",
        "..OYYWWWWYYYO...",
        "...OOYYYYYOO....",
        ".....OOOOOO.....",
        "................",
        "................",
    };

    // ---------- Dangerous-landscape art (used from level 2 up) ----------

    static readonly string[] ThornBushArt =
    {
        "................",
        "..p....p....p...",
        "...p..pp...p....",
        "..ppPpBBpPpp....",
        ".pBBBBBBBBBBp...",
        "..pBBpBBpBBBBp..",
        ".pBBBBBBBBBBBBp.",
        "ppBpBBBBBpBBBBpp",
        ".pBBBBpBBBBBpBp.",
        "..pBBBBBBBBBBBp.",
        ".pBpBBBBBpBBBp..",
        "..pBBBBpBBBBBp..",
        "...ppBBBBBBpp...",
        "..p..pppppp..p..",
        ".p............p.",
        "................",
    };

    static readonly string[] CrackArt =
    {
        "................",
        "................",
        "......k.........",
        ".......k........",
        ".......k....k...",
        "........k..k....",
        "........kkk.....",
        ".......k..k.....",
        "......k....k....",
        ".....k......k...",
        "....k.......kk..",
        "...k............",
        "................",
        "................",
        "................",
        "................",
    };

    static readonly string[] BonesArt =
    {
        "................",
        "................",
        "................",
        "................",
        "..NN........NN..",
        ".NNNn......NNNn.",
        "...nNN....NNn...",
        ".....NNNNNN.....",
        ".....nNNNNn.....",
        "...NNn....nNN...",
        ".NNNn......NNNn.",
        "..nn........nn..",
        "................",
        "................",
        "................",
        "................",
    };

    static readonly string[] SkullArt =
    {
        "................",
        "................",
        "................",
        ".....NNNNNN.....",
        "....NNNNNNNN....",
        "...NNNNNNNNNN...",
        "...NNkkNNkkNN...",
        "...NNkkNNkkNN...",
        "...nNNNkkNNNn...",
        "....nNNNNNNn....",
        ".....NkNkNkN....",
        ".....nNNNNNn....",
        "................",
        "................",
        "................",
        "................",
    };

    static readonly string[] EmberArt =
    {
        "................",
        "................",
        "................",
        "......R.........",
        ".....ROR........",
        "....ROYOR.......",
        ".....ROYOR...R..",
        "......ROYOR.ROR.",
        ".......ROYOROR..",
        "........ROOR....",
        ".........RR.....",
        "................",
        "................",
        "................",
        "................",
        "................",
    };

    static readonly string[] FireballArt =
    {
        "................",
        "................",
        "................",
        "......OOOO......",
        ".....OYYYYO.....",
        "....OYWWWYYO....",
        "....OYWWYYYO....",
        "...OYYYYYYYYO...",
        "...OYYYYYYYRO...",
        "....OYYYYYRO....",
        "....OOYYYROO....",
        ".....OOOOOO.....",
        "................",
        "................",
        "................",
        "................",
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
        var crystalSwordSprite = SaveSprite("CrystalSword", FromArt(CrystalSwordArt));
        var bossFrames = new[] { SaveSprite("Boss", FromArt(BossArt)), SaveSprite("Boss_Step", FromArt(BossStepArt)) };
        var bossAttackSprite = SaveSprite("Boss_Attack", FromArt(BossAttackArt));
        var fireballSprite = SaveSprite("Fireball", FromArt(FireballArt));
        var boltSprite = SaveSprite("SwordBolt", FromArt(BoltArt));
        var crystalBoltSprite = SaveSprite("CrystalBolt", FromArt(CrystalBoltArt));
        var glowSprite = SaveSprite("Glow", GlowTexture());
        var flameTex = FromArt(FlameArt);
        var flameFrames = new[] { SaveSprite("Flame", flameTex), SaveSprite("Flame_Flicker", Shift(flameTex, 0, 1)) };
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
        var waterSprite = SaveSprite("Water", WaterTexture());
        var scorchedSprite = SaveSprite("ScorchedGrass", ScorchedGrassTexture());
        var thornSprite = SaveSprite("ThornBush", FromArt(ThornBushArt));
        var crackSprite = SaveSprite("Crack", FromArt(CrackArt));
        var bonesSprite = SaveSprite("Bones", FromArt(BonesArt));
        var skullSprite = SaveSprite("Skull", FromArt(SkullArt));
        // Embers flicker: the second frame burns hotter (orange -> yellow, yellow -> white).
        var emberHot = EmberArt.Select(row => row.Replace('Y', 'W').Replace('O', 'Y')).ToArray();
        var emberFrames = new[] { SaveSprite("Ember", FromArt(EmberArt)), SaveSprite("Ember_Hot", FromArt(emberHot)) };

        // --- Tiles ---
        var waterTile = SaveTile("WaterTile", waterSprite, Tile.ColliderType.Grid);
        var scorchedTile = SaveTile("ScorchedGrassTile", scorchedSprite, Tile.ColliderType.None);
        // Thorns block like a full tile, so their spiky outline never changes how passages fit the player.
        var thornTile = SaveTile("ThornBushTile", thornSprite, Tile.ColliderType.Grid);
        var grassTile = SaveTile("GrassTile", grassSprite, Tile.ColliderType.None);
        var wallTile = SaveTile("WallTile", wallSprite, Tile.ColliderType.Grid);
        var bushTile = SaveTile("BushTile", bushSprite, Tile.ColliderType.Sprite);

        // --- Clean up anything from a previous build ---
        foreach (var name in new[] { "Board", "Player", "Monsters", "HUD", "Audio", "Rooms" })
        {
            var old = GameObject.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }

        // --- Board (tilemaps) ---
        var board = new GameObject("Board", typeof(Grid));
        Undo.RegisterCreatedObjectUndo(board, "Build Zelda Board");

        var ground = CreateTilemap(board.transform, "Ground", 0);
        var walls = CreateTilemap(board.transform, "Walls", 1);

        // Room (row, col) is centred at (col * RoomW, -row * RoomH) in world units.
        var spawns = new List<(char c, Vector3 pos, int room)>();
        for (int row = 0; row < WorldRows; row++)
        for (int col = 0; col < WorldCols; col++)
        {
            for (int y = 0; y < RoomH; y++)
            {
                for (int x = 0; x < RoomW; x++)
                {
                    char ch = RoomChar(row, col, x, y);
                    var cell = new Vector3Int(col * RoomW + x - RoomW / 2, -row * RoomH + RoomH / 2 - 1 - y, 0);
                    ground.SetTile(cell, grassTile);
                    if (ch == '#') walls.SetTile(cell, wallTile);
                    else if (ch == 'b') walls.SetTile(cell, bushTile);
                    else if (ch == 'w') walls.SetTile(cell, waterTile);
                    else if (ch != '.') spawns.Add((ch, new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0), row * WorldCols + col));
                }
            }
        }

        // Colliders go on after the tiles are placed, then the merged geometry is generated explicitly.
        // (If the composite is generated while the tilemap is still empty, the scene saves with no wall collision.)
        var wallBody = walls.gameObject.AddComponent<Rigidbody2D>();
        wallBody.bodyType = RigidbodyType2D.Static;
        var tilemapCollider = walls.gameObject.AddComponent<TilemapCollider2D>();
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        var composite = walls.gameObject.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons; // solid, so anything inside gets pushed out
        tilemapCollider.ProcessTilemapChanges();
        composite.GenerateGeometry();
        if (composite.pathCount == 0)
            Debug.LogError("Zelda board: wall collider geometry is empty - walls will not block movement.");

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

        // --- Fireball prefab (shot by the boss) ---
        var fireballGo = new GameObject("Fireball");
        fireballGo.transform.localScale = Vector3.one * 0.7f;
        var fireballSr = fireballGo.AddComponent<SpriteRenderer>();
        fireballSr.sprite = fireballSprite;
        fireballSr.sortingOrder = 7;
        var fireballRb = fireballGo.AddComponent<Rigidbody2D>();
        fireballRb.gravityScale = 0;
        fireballRb.freezeRotation = true;
        fireballRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var fireballCol = fireballGo.AddComponent<CircleCollider2D>();
        fireballCol.isTrigger = true;
        fireballCol.radius = 0.3f;
        fireballGo.AddComponent<Fireball>();
        var fireballPrefab = PrefabUtility.SaveAsPrefabAsset(fireballGo, PrefabDir + "/Fireball.prefab");
        Object.DestroyImmediate(fireballGo);

        // --- Burning-bush flames (left behind when a Crystal Sword bolt burns a bush) ---
        var flameGo = new GameObject("BushFire");
        var flameSr = flameGo.AddComponent<SpriteRenderer>();
        flameSr.sprite = flameFrames[0];
        flameSr.sortingOrder = 6;
        var flameAnim = flameGo.AddComponent<SpriteAnimator>();
        flameAnim.frames = flameFrames;
        flameAnim.fps = 14f;
        var flameFade = flameGo.AddComponent<FadeOut>();
        flameFade.delay = 0.3f;
        flameFade.duration = 0.35f;
        flameFade.grow = 0.4f;
        var flamePrefab = PrefabUtility.SaveAsPrefabAsset(flameGo, PrefabDir + "/BushFire.prefab");
        Object.DestroyImmediate(flameGo);

        // --- Sword beam prefab (lightning bolt fired while health is high enough) ---
        var beamGo = new GameObject("SwordBeam");
        beamGo.transform.localScale = Vector3.one * 1.3f;
        var beamSr = beamGo.AddComponent<SpriteRenderer>();
        beamSr.sprite = boltSprite;
        beamSr.sortingOrder = 7;
        var beamRb = beamGo.AddComponent<Rigidbody2D>();
        beamRb.gravityScale = 0;
        beamRb.freezeRotation = true;
        beamRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var beamCol = beamGo.AddComponent<BoxCollider2D>();
        beamCol.isTrigger = true;
        beamCol.size = new Vector2(0.4f, 0.8f);
        var beam = beamGo.AddComponent<SwordBeam>();
        beam.crystalSprite = crystalBoltSprite;
        beam.burnEffectPrefab = flamePrefab;
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(beamGo.transform, false);
        glowGo.transform.localScale = Vector3.one * 2.2f;
        var glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = glowSprite;
        glowSr.color = new Color(0.55f, 0.95f, 1f, 0.45f);
        glowSr.sortingOrder = 6;
        beam.glow = glowSr;
        var beamPrefab = PrefabUtility.SaveAsPrefabAsset(beamGo, PrefabDir + "/SwordBeam.prefab").GetComponent<SwordBeam>();
        Object.DestroyImmediate(beamGo);

        // --- Player & monsters ---
        GameObject player = null;
        var monsters = new GameObject("Monsters");
        Undo.RegisterCreatedObjectUndo(monsters, "Build Zelda Board");

        // One container per room so the RoomManager can switch a whole room's monsters on and off.
        int bossRoom = -1;
        var roomContainers = new Transform[WorldRows * WorldCols];
        for (int i = 0; i < roomContainers.Length; i++)
        {
            var container = new GameObject($"Room {i / WorldCols},{i % WorldCols}");
            container.transform.SetParent(monsters.transform, false);
            roomContainers[i] = container.transform;
        }

        foreach (var (ch, pos, room) in spawns)
        {
            var parent = roomContainers[room];
            // Keep monsters on their room's floor (inside the outer wall).
            var center = new Vector2((room % WorldCols) * RoomW, -(room / WorldCols) * RoomH);
            var bounds = new Rect(center.x - RoomW / 2f + 1.5f, center.y - RoomH / 2f + 1.5f, RoomW - 3f, RoomH - 3f);
            switch (ch)
            {
                case 'P':
                    player = CreatePlayer(pos, playerDown, playerUp, playerSide, swordSprite, beamPrefab);
                    break;
                case 's':
                    CreateMonster("Slime", pos, slimeFrames, 3f, parent, bounds, Enemy.Behaviour.Wander, 1.2f, 2, 100, heartPrefab);
                    break;
                case 'v':
                    CreateMonster("Bat", pos, batFrames, 10f, parent, bounds, Enemy.Behaviour.Erratic, 2.5f, 1, 150, heartPrefab);
                    break;
                case 'm':
                    CreateMonster("Moblin", pos, moblinFrames, 6f, parent, bounds, Enemy.Behaviour.Chase, 2f, 3, 250, heartPrefab);
                    break;
                case 'B':
                    CreateBoss(pos, bossFrames, bossAttackSprite, parent, bounds, fireballPrefab);
                    bossRoom = room;
                    break;
                case 'S':
                    CreateSwordPickup(pos, crystalSwordSprite, parent);
                    break;
            }
        }

        // --- Camera: frames one room; starts on the player's room ---
        var cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Build Zelda Board");
            Undo.RecordObject(cam.transform, "Build Zelda Board");
            cam.orthographic = true;
            cam.orthographicSize = RoomH / 2f + 0.2f;
            Vector3 start = Vector3.zero;
            foreach (var (ch, _, room) in spawns)
                if (ch == 'P') start = new Vector3((room % WorldCols) * RoomW, -(room / WorldCols) * RoomH, 0);
            cam.transform.position = start + new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
        }

        // --- Rooms ---
        var roomsGo = new GameObject("Rooms");
        Undo.RegisterCreatedObjectUndo(roomsGo, "Build Zelda Board");
        var roomManager = roomsGo.AddComponent<RoomManager>();
        roomManager.rows = WorldRows;
        roomManager.cols = WorldCols;
        roomManager.roomSize = new Vector2(RoomW, RoomH);
        roomManager.player = player != null ? player.GetComponent<PlayerController>() : null;
        roomManager.cam = cam;
        roomManager.roomMonsters = roomContainers;
        roomManager.bossRoomIndex = bossRoom;

        // Level setup (tint, extra monsters, which sword nook is used) happens at runtime.
        var director = roomsGo.AddComponent<LevelDirector>();
        director.rooms = roomManager;
        director.ground = ground;
        director.walls = walls;
        director.grassTile = grassTile;
        director.scorchedGrassTile = scorchedTile;
        director.bushTile = bushTile;
        director.thornBushTile = thornTile;
        director.crackSprite = crackSprite;
        director.bonesSprite = bonesSprite;
        director.skullSprite = skullSprite;
        director.emberFrames = emberFrames;

        // --- HUD ---
        var hud = new GameObject("HUD");
        Undo.RegisterCreatedObjectUndo(hud, "Build Zelda Board");
        var hudComp = hud.AddComponent<GameHUD>();
        hudComp.playerHealth = player != null ? player.GetComponent<Health>() : null;
        hudComp.heartFull = heartSprite.texture;
        hudComp.heartEmpty = heartEmptySprite.texture;
        hudComp.upgradedSwordIcon = crystalSwordSprite.texture;

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
        am.bossMusic = clips.bossMusic;
        am.dangerMusic = clips.dangerMusic;
        am.fireball = clips.fireball;
        am.itemGet = clips.itemGet;
        am.zap = clips.zap;
        am.burn = clips.burn;

        // Crystal Sword bolts burn bushes on the walls tilemap.
        if (player != null)
        {
            var playerController = player.GetComponent<PlayerController>();
            playerController.walls = walls;
            playerController.bushTile = bushTile;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"Zelda board built: {spawns.Count} spawns. Press Play! Move with WASD/arrows, attack with Space/J.");
    }

    static GameObject CreatePlayer(Vector3 pos, Sprite[] down, Sprite[] up, Sprite[] side, Sprite swordSprite, SwordBeam beamPrefab)
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

        // Slim capsule with rounded ends: slides along wall corners instead of snagging on them.
        var col = go.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Vertical;
        col.size = new Vector2(0.6f, 0.75f);
        col.offset = new Vector2(0, -0.08f);

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
        controller.beamPrefab = beamPrefab;
        controller.moveSpeed = 6f;
        return go;
    }

    static void CreateMonster(string name, Vector3 pos, Sprite[] frames, float fps, Transform parent, Rect bounds,
        Enemy.Behaviour behaviour, float speed, int hp, int score, GameObject drop)
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
        enemy.bounds = bounds;
        enemy.scoreValue = score;
    }

    static void CreateBoss(Vector3 pos, Sprite[] walkFrames, Sprite attackFrame, Transform parent, Rect roomBounds, GameObject fireballPrefab)
    {
        var go = new GameObject("Boss");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 2.5f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = walkFrames[0];
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.mass = 5f;
        rb.linearDamping = 4f; // knockback fades quickly
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.38f;

        // Boss HP is set at runtime from its hit counts and the level (see Boss.ApplyLevel).
        var health = go.AddComponent<Health>();
        health.maxHealth = 30;
        // Long enough that one swing counts once, shorter than the 0.3 s between swings so none are wasted.
        health.invulnerableTime = 0.25f;
        health.stunTime = 0.1f;

        var boss = go.AddComponent<Boss>();
        boss.walkFrames = walkFrames;
        boss.attackFrame = attackFrame;
        boss.fireballPrefab = fireballPrefab;
        // The boss is ~2 tiles wide, so keep its centre a bit further from the walls.
        boss.bounds = new Rect(roomBounds.x + 0.75f, roomBounds.y + 0.75f, roomBounds.width - 1.5f, roomBounds.height - 1.5f);
    }

    static void CreateSwordPickup(Vector3 pos, Sprite sprite, Transform parent)
    {
        var go = new GameObject("CrystalSword");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, 0, -45f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;
        var pickup = go.AddComponent<SwordPickup>();
        pickup.upgradedSwordSprite = sprite;
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

    // Burnt, ashy ground for the later levels, with the odd glowing ember.
    static Texture2D ScorchedGrassTexture()
    {
        var tex = NewTexture();
        var rng = new System.Random(11);
        var baseCol = new Color32(92, 96, 58, 255);
        var ash = new Color32(70, 66, 58, 255);
        var dry = new Color32(118, 112, 70, 255);
        var ember = new Color32(205, 95, 40, 255);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int n = rng.Next(100);
                tex.SetPixel(x, y, n < 14 ? ash : n < 20 ? dry : n < 21 ? ember : baseCol);
            }
        tex.Apply();
        return tex;
    }

    // Soft round halo: white with alpha fading out from the centre (tinted at runtime).
    static Texture2D GlowTexture()
    {
        var tex = NewTexture();
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8f, 8f)) / 8f;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        return tex;
    }

    static Texture2D WaterTexture()
    {
        var tex = NewTexture();
        var deep = new Color32(40, 90, 200, 255);
        var mid = new Color32(60, 120, 220, 255);
        var foam = new Color32(170, 210, 250, 255);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                // Two staggered rows of little wave crests.
                int wy = y % 8;
                int wx = (x + (y / 8) * 4) % 8;
                bool crest = (wy == 5 && (wx == 1 || wx == 4)) || (wy == 6 && (wx == 2 || wx == 3));
                tex.SetPixel(x, y, crest ? foam : (y / 4) % 2 == 0 ? deep : mid);
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
