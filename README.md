# 2D Claude Test — Top-Down Adventure Prototype

A small top-down action prototype in the style of classic Zelda, built in Unity with the help of Claude. Explore a single board, swing your sword, and clear out every monster.

![The board](Docs/screenshot.png)

All of the pixel art, sound effects and music are generated from code — there are no imported art or audio assets.

## Playing

1. Open the project in **Unity 6000.6.5f1** (or a compatible Unity 6 version).
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press **Play**.

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Move | WASD / Arrow keys | Left stick |
| Sword attack | Space / J | South button (A / Cross) |

Defeat every monster to win. You have 5 hearts; if they run out, the board restarts.

## Features

- **Player** with four-direction movement, walk animations for each direction, and a sweeping sword arc.
- **Three monster types**
  - **Slime** (red, 2 HP) — wanders around.
  - **Bat** (purple, 1 HP) — flutters erratically.
  - **Moblin** (orange, 3 HP) — chases you when you get close.
- **Combat**: knockback, hit flashes and invulnerability frames.
- **Heart pickups** that monsters sometimes drop.
- **HUD** showing hearts, monsters remaining, and win / game-over messages.
- **Audio**: chiptune sound effects and an original looping background track.

## Project layout

| Path | Contents |
| --- | --- |
| `Assets/Scripts/` | Runtime gameplay: `PlayerController`, `PlayerAnimator`, `Enemy`, `SpriteAnimator`, `SwordHitbox`, `Health`, `HeartPickup`, `GameHUD`, `AudioManager` |
| `Assets/Editor/ZeldaBoardBuilder.cs` | Draws the pixel art and builds the level in the open scene |
| `Assets/Editor/ChiptuneSynth.cs` | Synthesizes the sound effects and music as WAV files |
| `Assets/Art/` | Generated sprites and tiles |
| `Assets/Audio/` | Generated sound effects and music |
| `Assets/Prefabs/` | Heart pickup prefab |
| `Assets/Scenes/SampleScene.unity` | The playable scene |

## Editing the level

The board layout is a text grid in `Assets/Editor/ZeldaBoardBuilder.cs` (`Map`):

| Character | Meaning |
| --- | --- |
| `#` | Wall |
| `b` | Bush |
| `.` | Grass |
| `P` | Player start |
| `s` | Slime |
| `v` | Bat |
| `m` | Moblin |

After editing, run **Tools > Build Zelda Board** in the Unity menu. This regenerates the art, audio and level in the open scene and saves it.

The sprites are also defined as text in the same file, one character per pixel, so you can redraw characters there. To change the music, edit the `Melody` and `Bass` note lists in `Assets/Editor/ChiptuneSynth.cs` and rebuild.

## Tech

- Unity 6 with the Universal Render Pipeline (2D Renderer)
- Input System package
- Tilemaps with composite colliders for walls and bushes
