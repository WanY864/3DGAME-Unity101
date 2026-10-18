# Depthbound

**A first-person 3D dungeon crawler made with Unity 6 for my Unity 101 final project.**

Depthbound is a small dungeon combat game where I explore rooms, fight different enemies, choose whether to spare or defeat weakened monsters, and try to survive the bosses. I wanted the game to feel like a 3D dungeon while keeping a simple pixel-art look.

- **Engine:** Unity 6 (6000.6.0f1)
- **Language:** C#
- **Platform:** Windows x64
- **Project type:** Unity 101 Final Project

## How to Play

1. Run the game and click **DESCEND** on the main menu.
2. Use **WASD** and the mouse to move through the dungeon and look around.
3. Fight enemies with your dagger or switch to the sword and shield. You can hold the left mouse button for repeated attacks.
4. Use the **Space** boost to dash. It is useful for moving quickly and avoiding danger.
5. When an enemy enters the mercy state, decide whether to **spare** it or **kill** it. These decisions affect the run.
6. Clear rooms, take upgrades when offered, and try to beat the bosses through the three acts.

### Controls

| Key / Mouse | Action |
| --- | --- |
| **W / A / S / D** | Move |
| **Mouse** | Look around |
| **Left Mouse Button (hold)** | Attack / continue a combo |
| **Q** | Switch between dagger and sword + shield |
| **Right Mouse Button (hold)** | Block / attempt a parry (sword + shield mode) |
| **Space** | Boost / dash |
| **Y** | Spare a weakened enemy when the mercy prompt appears |
| **X** | Kill a weakened enemy when the mercy prompt appears |
| **1 / 2 / 3** | Select an upgrade when the upgrade screen appears |
| **B** | Open / close the bestiary |
| **Esc** | Pause / resume or close the bestiary |
| **QUIT button** | Exit the application |

The HUD shows important information such as health, combat status, and boost cooldown. The pause menu also has settings for mouse sensitivity, sound volume, camera shake, and display mode.

## Main Features

- **3D dungeon and physics:** First-person movement, collisions, and Rigidbody-based movement.
- **Prefab spawning:** Enemy instances are created from an enemy Prefab during play.
- **Boost:** Pressing Space applies an impulse with Rigidbody.AddForce().
- **Combat:** Two weapon styles, attack combos, blocking, and parrying.
- **Enemy variety:** Different monster types with different health, speed, attack damage, and behaviors.
- **Mercy / Sin choices:** Decide what happens to some defeated enemies.
- **Visual style:** Runtime-generated pixel sprites and a layered extrusion-like effect for monster thickness.
- **UI, audio, and scenes:** Live HUD, sound effects, a main menu, a game scene, and buttons to quit.

### A small code example: Rigidbody boost

This is part of the boost implementation in [FirstPersonController.cs](Assets/Scripts/FirstPersonController.cs). I used AddForce with Impulse mode because the assignment requires a key-triggered burst of force.

~~~csharp
public float boostImpulse = 24f;

// Inside the movement / physics update:
if (boostRequested)
{
    boostRequested = false;
    Vector3 boostDirection =
        desiredDirection.sqrMagnitude > 0.01f
            ? desiredDirection
            : transform.forward;

    body.AddForce(
        boostDirection * boostImpulse,
        ForceMode.Impulse);
}
~~~

## Running the Project

### From Unity

1. Clone or download this repository.
2. Open the folder as a project in **Unity 6 (6000.6.0f1)**.
3. Open **Assets/Scenes/MainMenu.unity**.
4. Press Play.

### Windows build

The Windows build is distributed as a **complete build folder**, not just Depthbound.exe. Keep Depthbound.exe, Depthbound_Data, and the other generated Unity runtime files together.

The build folder is excluded from the Git repository using the Unity .gitignore. The Windows submission is provided separately.

To create a build from Unity, use **Tools > Depthbound > Build Windows x64** after the project compiles successfully.

---

# Post-Mortem — Unity 101 Final Project

*Windows build testing, sprite extrusion, and gameplay balancing*

This is my development reflection for the Unity 101 final project. My biggest issue was the mouse camera in the exported Windows game. After that, I also worked on giving 2D monsters more depth and balancing the combat through playtesting.

## 1. Main Problem: Mouse Camera Stuttering in the Windows Build

### What went wrong

When I tested Depthbound inside the Unity Editor, looking around with the mouse felt normal. After I exported it to a Windows EXE, though, the camera movement became very uncomfortable. Sometimes I would move the mouse to the right and the camera seemed to stop for a split second, or move slightly back. It almost felt like the mouse was not completely captured by the game window.

This mattered a lot because looking around is such a basic part of a first-person game. Even though the game could run, it did not feel good to play.

### My investigation (including the guesses that were wrong)

My first guess was that the wireless mouse or Windows window focus was causing the problem. I tried clicking back into the game, changing between windowed and full-screen mode, and making the game lock the cursor more aggressively. These changes did not fix the stutter.

Then I thought it might be Unity's mouse input system. I checked the Active Input Handling setting, tried different ways to read the mouse movement, and experimented with input smoothing. Some of these attempts even caused compilation errors, so I had to go back and fix the scripts before I could build again. That was a good reminder to change one thing at a time.

The clue that helped me most was a simple playtest: **the monsters and animations stayed smooth, and only the camera movement felt wrong**. It also happened in the standalone EXE but not in the Editor. This made me realize I should check the differences between running in the Editor and running a Windows build instead of only looking at the mouse code.

I also encountered a separate Windows-build problem: the game launched to a black screen. I checked Player.log and found that the main menu was trying to create a material with a missing shader. Including the needed shader in the build helped get the menu working. This was another reason I learned to look at build logs rather than guess.

### What finally worked

The change that actually made the camera feel smooth was **disabling VSync and setting a 60 FPS target** for the standalone game. The final frame-rate settings in [SceneBootstrap.cs](Assets/Scripts/SceneBootstrap.cs) are:

~~~csharp
// Windows build: use a 60 FPS target without VSync.
QualitySettings.vSyncCount = 0;
Application.targetFrameRate = 60;
~~~

I also discovered a second targetFrameRate assignment later in the same method. That could override my 60 FPS setting, so I removed the duplicate. I rebuilt the Windows version and tested the camera again.

I still do not fully understand every reason this particular setup behaved differently in the EXE, but I learned that frame timing and input responsiveness are connected. **A game that feels fine in the Unity Editor is not automatically ready to submit as a Windows build.**

## 2. Making the 2D Monsters Look More 3D

Another challenge was the monster art. The enemies were made from pixel sprites. I liked how they fit the style of the dungeon, but they looked too flat in a 3D world.

Instead of remaking all the monsters as detailed 3D models, I tried a simple **extrusion-like layered sprite method**. In [EnemyController.cs](Assets/Scripts/EnemyController.cs), my AddMonsterThickness() method creates five extra copies of the sprite, places them at different depths behind the original, and makes them darker.

Here is the main idea from my code:

~~~csharp
public float monsterThickness = 0.5f;

private void AddMonsterThickness(GameObject spriteObject)
{
    int layers = 5;

    for (int i = 1; i <= layers; i++)
    {
        GameObject layer = new GameObject("DepthLayer_" + i);
        layer.transform.SetParent(spriteObject.transform, false);

        float depth = monsterThickness * i / layers;
        layer.transform.localPosition =
            new Vector3(0f, 0f, -depth);

        SpriteRenderer render = layer.AddComponent<SpriteRenderer>();
        render.sprite = pixelSpriteRenderer.sprite;
        render.color = new Color(0.25f, 0.22f, 0.20f, 1f);
        render.sortingOrder =
            pixelSpriteRenderer.sortingOrder - 1;
    }
}
~~~

This is **not a real closed 3D mesh**. It is a 2.5D depth effect made with layered SpriteRenderers. For this project, I thought it was a useful compromise because it kept the original pixel-art appearance and gave the monsters a thicker look without requiring a new modeling workflow.

## 3. Playtesting Enemy Health and Player Stats

I also spent time playing through the dungeon and adjusting combat numbers. I did not want every enemy to feel exactly the same. In [EnemyController.cs](Assets/Scripts/EnemyController.cs), different enemy types have their own starting health, damage, and movement speed, and enemy health increases as the player goes through later rooms.

For example, this part calculates the room-based health increase:

~~~csharp
int roomAct = roomIndex / 3;
int localRoom = roomIndex % 3;

float roomHealth =
    roomAct * 18f + localRoom * 18f;

// Different enemy types add this to different base HP values.
~~~

I tested the player side too. The player starts with **100 health**, and I adjusted the Space boost impulse to **24** so it felt more useful. I also checked weapon damage and how upgrades changed combat. Most of this was trial and error: play a room, notice something that felt too slow or too punishing, adjust a value, and try again.

## What I Learned

The biggest lesson for me was that **finishing the code is not the same as finishing the game**. I needed to test the exported version, read errors from Player.log, question my first assumptions, and compare my changes carefully. I am glad that I eventually fixed the Windows mouse-camera problem, found a practical way to make the pixel monsters feel less flat, and improved the combat through testing.

---

## Repository Structure

~~~text
Assets/
  Editor/          # Project setup, checks, and Windows build tools
  Resources/       # Enemy prefab
  Scenes/          # MainMenu and Game
  Scripts/         # Gameplay, enemies, UI, audio, and effects
Packages/          # Unity package dependencies
ProjectSettings/   # Unity project configuration
README.md          # Gameplay guide and post-mortem
.gitignore         # Unity-generated files excluded from version control
~~~

**Course:** Unity 101 — Final Project  
**Game:** Depthbound  
**GitHub:** https://github.com/WanY864/3DGAME-Unity101
