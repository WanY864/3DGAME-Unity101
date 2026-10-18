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
- **3D monster visuals:** Pixel-art enemies given 3D depth using a layered extrusion method.
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

### The problem I noticed

The most frustrating problem happened near the end, when I finally exported Depthbound as a Windows EXE. In Unity's **Game** window, the first-person camera worked normally. But in the standalone game, moving the mouse felt strangely sticky. When I moved to the right, sometimes the camera would stop for a moment and then jump again. Occasionally it looked as if the view moved slightly back to the left. It felt like the game was losing my mouse for a split second.

At first I described it as a camera "re-centering" problem, but after playing more carefully I realized it was mostly **stuttering**, not an actual automatic turn back to the center. I could stand completely still and reproduce it just by moving the mouse. The enemies and other animations still looked smooth, which was confusing because the whole game did not seem slow.

Since Depthbound is played in first person, this was a serious problem. I did not want to submit a game where the very basic action of looking around felt broken.

### Test 1 — Was my mouse or the game window losing focus?

My first guess was the mouse itself. I was using a **2.4 GHz wireless mouse**, so I considered the connection or polling rate. But the same mouse worked normally in Windows and in the Unity Editor, so hardware was not a convincing explanation.

Next, I thought the Windows game window might not have captured the cursor correctly. I tried clicking into the game again and pausing and resuming it. I also changed the player script to keep the cursor locked during gameplay while still unlocking it for pause menus.

Part of the code in [FirstPersonController.cs](Assets/Scripts/FirstPersonController.cs) checks the lock state:

~~~csharp
if (Application.isFocused)
{
    if (Cursor.lockState != CursorLockMode.Locked)
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    Cursor.visible = false;
}
~~~

This helped make the intended mouse behavior clear, but **it did not fix the stuttering**. The problem was more complicated than simply keeping the cursor inside the window.

### Test 2 — Window resizing and the camera

I noticed another annoying behavior in the EXE: I could resize the window, but later it seemed to change size again. I checked [RuntimeSettings.cs](Assets/Scripts/RuntimeSettings.cs) and found that the game explicitly called Screen.SetResolution() for its windowed mode.

I changed ApplyDisplay() so that, after applying the display settings once, it would not keep applying them again during the same session:

~~~csharp
if (displayAppliedThisSession)
{
    return;
}
~~~

This dealt with a separate window-settings issue, but **the mouse still felt stuck sometimes**.

We also looked at the camera and the player's physics. My player moves with a Rigidbody, so I wondered whether the physics update rate could be different from the camera's frame rate. However, the Rigidbody already had Interpolate enabled. More importantly, the stutter happened when I was standing still, too. That made me less confident that player movement physics was the main cause.

### Test 3 — Trying different mouse input code

After that I focused on Unity's input system. The game originally read mouse movement using the old Input Manager:

~~~csharp
float rawMouseX =
    Input.GetAxis("Mouse X") * sensitivity;

float rawMouseY =
    Input.GetAxis("Mouse Y") * sensitivity;
~~~

I checked **Project Settings > Player > Active Input Handling** and tried the **Both** option, which allows the old and new input systems. That did not solve the issue. I learned that setting it to Both does not automatically change which API the script uses.

I also experimented with GetAxisRaw() and with reading Mouse.current.delta from the newer Input System. I hoped that a different source of mouse movement data might avoid the tiny stops in the camera. But the tests were not successful.

Some of these edits created new problems. I made a mistake with the capitalization of UnityEngine in a using statement, and at another point Unity entered **Safe Mode** because of compile errors involving Input System package tests. I had to fix or back out the input changes before continuing. It was frustrating because I was trying to fix one problem and accidentally creating another one.

Eventually I went back to the familiar input method and tried smoothing the values instead of lowering the mouse reading frequency:

~~~csharp
float smoothing =
    1f - Mathf.Exp(-15f * Time.unscaledDeltaTime);

smoothMouseX = Mathf.Lerp(
    smoothMouseX, rawMouseX, smoothing);

smoothMouseY = Mathf.Lerp(
    smoothMouseY, rawMouseY, smoothing);
~~~

This can make input changes less abrupt, but it **still did not solve the Windows EXE issue**. I learned that smoothing a symptom is not the same as finding its cause.

### Test 4 — Checking what was different in the Windows build

At this point I compared the two environments again:

- **Unity Editor:** mouse movement was fine.
- **Windows EXE:** the camera stuttered, even without walking.
- **Other gameplay:** enemy movement and visual effects kept running normally.
- **Mouse-lock changes, input-mode changes, and smoothing:** none had fixed it.

Instead of repeatedly rewriting the player controller, I started looking at the standalone game's **graphics and frame-rate configuration**. The Windows Player.log also showed that the game was running with **Direct3D 11**, so I had no good reason to blame DirectX 12.

I had already learned the usefulness of Player.log from a *different* build issue: one exported version showed a black screen because the main menu tried to create a Material using a Shader that was missing from the build. That issue was related to shader inclusion, not mouse input, but it taught me to treat the standalone build as its own thing and check what it was actually doing.

### What finally worked — VSync off, target 60 FPS

The successful change was in [SceneBootstrap.cs](Assets/Scripts/SceneBootstrap.cs). I disabled **vertical synchronization (VSync)** and set the game's frame-rate target to **60 FPS**:

~~~csharp
// Final Windows build frame-rate settings
QualitySettings.vSyncCount = 0;
Application.targetFrameRate = 60;
~~~

**After this change and a new Windows build, the mouse-camera stutter stopped in my test.** That was the first test where the game finally felt comfortable to look around in again.

One more small detail came up when reviewing the final GitHub code: there was also an older Application.targetFrameRate = 120 assignment farther down the same method. That could override the intended value of 60, so I removed the duplicate from the repository. The code now has one clear frame-rate setting.

I cannot say for sure exactly why VSync and frame timing produced this behavior on my computer. What I can say from my tests is that **changing mouse hardware assumptions, cursor locking, input APIs, and smoothing did not solve it, while changing the standalone frame-rate settings did**.

### What I learned from this problem

This debugging process took much longer than I expected, but it taught me several useful things:

1. **Test the actual exported game.** Something that works in the Unity Editor can behave differently as a Windows EXE.
2. **Describe the problem precisely.** "The camera moves back" and "the camera freezes briefly" sound similar but lead to different guesses.
3. **Do not assume the first explanation is correct.** I spent time blaming the mouse and input system before finding the setting that worked.
4. **Change one thing at a time and rebuild.** Otherwise, it becomes hard to know which change made a difference, and careless edits can introduce compile errors.
5. **Check the final code, not just the playtest.** Removing the duplicate 120 FPS assignment helped make the final settings consistent.

For me, this was the most useful part of developing Depthbound. I learned more from the failed tests and the final Windows build than I would have learned by only playing inside the Unity Editor.

## 2. Creating 3D Monsters from 2D Sprites

Another challenge was turning my 2D pixel-art monsters into 3D enemies. I wanted the characters to have visible depth and still match the art style of the dungeon.

I used a **3D extrusion technique** based on layered sprites. In [EnemyController.cs](Assets/Scripts/EnemyController.cs), my AddMonsterThickness() method creates five extra copies of each sprite, places them at increasing depths behind the original, and gives them a darker color to form the sides.

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

The result gives the monsters **3D depth** while keeping their original pixel-art appearance. I liked this approach because I could improve the 3D visuals without rebuilding every monster model from scratch.

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

The biggest lesson for me was that **finishing the code is not the same as finishing the game**. I needed to test the exported version, read errors from Player.log, question my first assumptions, and compare my changes carefully. I am glad that I eventually fixed the Windows mouse-camera problem, used extrusion to give my pixel-art monsters 3D depth, and improved the combat through testing.

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
