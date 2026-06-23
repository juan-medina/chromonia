# CHROMONIA
## Game Design Document

**Version 1.0 · June 2026**
Engine: Godot 4 · Language: C# · Platform: Windows / macOS / Linux

---

## Table of Contents

1. [Overview](#1-overview)
2. [Gameplay](#2-gameplay)
3. [Controls](#3-controls)
4. [Screens & Application Flow](#4-screens--application-flow)
5. [Assets](#5-assets)
6. [Technical Design](#6-technical-design)
7. [Suggested Godot Scene Structure](#7-suggested-godot-scene-structure)
8. [Out of Scope (v1.0)](#8-out-of-scope-v10)
9. [Open Questions](#9-open-questions)

---

## 1. Overview

### 1.1 Concept

Chromonia is a relaxing, single-player territory-claiming game inspired by the 1981 Taito arcade classic Qix. The player draws lines to claim sections of a playfield, gradually revealing a desaturated public domain painting in full colour. Two coloured enemies — one red, one blue — patrol the unclaimed space, and the player must manage their own drawing colour to interact with each correctly.

There is no score, no timer, and no punishment for failure. The experience is designed to feel meditative: beautiful classical paintings reveal themselves slowly while a curated public domain classical music soundtrack plays in the background.

### 1.2 Pillars

- **Relaxing** — no pressure, no punishment, no rush
- **Beautiful** — public domain fine art as the game canvas
- **Simple** — a handful of rules that create emergent complexity
- **Endless** — paintings cycle randomly, the loop never stops

### 1.3 Target Platform

| | |
|---|---|
| Engine | Godot 4 |
| Language | C# (.NET) |
| Primary OS | Windows |
| Also ships | macOS, Linux |
| Mobile | Not planned |
| Web | Not planned |
| Design resolution | 1920 × 1080 |
| Auto-update | Velopack |

### 1.4 Name & Identity

**Title: Chromonia**

A compound of *Chroma* (Greek: colour) and *Harmonia* (Greek goddess of harmony and concord). The name reflects both the colour-reveal mechanic and the peaceful, musical tone of the experience.

---

## 2. Gameplay

### 2.1 Core Loop

1. A random painting is loaded from the active collection and displayed in greyscale.
2. Enemies are spawned into the unclaimed area based on the chosen difficulty.
3. The player draws lines to claim territory, revealing the painting in colour section by section.
4. The round ends when the fill threshold is reached **and** all enemies have been eliminated.
5. A new random painting is immediately presented. The loop continues indefinitely.

### 2.2 The Playfield

The playfield is a rectangle matching the painting dimensions, letterboxed with dark bars where the painting aspect ratio does not match 1920 × 1080. The entire playfield begins as unclaimed (greyscale). Claimed area is revealed in full colour via a desaturation shader lerp.

The player's marker always starts on the border of the playfield. The border and all previously claimed polygon edges are considered **safe ground**.

### 2.3 Drawing

**Movement is continuous and vector-based, but angularly constrained.** The player can only travel in one of 8 directions — horizontal, vertical, or 45° diagonal — and can only turn in 45° increments. There is no grid; the player moves freely at any speed within those allowed angles.

When the player moves off safe ground into unclaimed space, a line begins drawing behind them (a *Stix*). The line is rendered in the player's current colour (red or blue). The player may change direction while drawing, producing a multi-segment line, as long as each new direction is one of the 8 allowed angles.

If the player returns to safe ground, the enclosed area is claimed. The player may press Space at any time — even while drawing mid-line — to instantly swap their active colour.

> **Note:** Swapping colour mid-line changes the entire active line to the new colour. This is a core defensive mechanic, allowing the player to dynamically swap "teams" to allow an approaching enemy to pass through their line harmlessly.

### 2.4 Claiming Territory

When a line is completed, the **smaller** of the two resulting areas is always claimed — regardless of enemy positions. This is the fundamental rule inherited from Qix and is what makes enemy enclosure a deliberate act of skill.

At the moment of claiming, enemies inside the newly claimed polygon are checked:

| Condition | Result |
|---|---|
| No enemies inside the polygon | Area is claimed. Painting revealed in colour. |
| Only same-colour enemies inside | Those enemies are eliminated. Area is claimed. |
| Any opposite-colour enemy inside | Line is cancelled. Nothing is claimed. All enemies inside survive. |
| Both colours of enemy inside | Line is cancelled. Nothing is claimed. Both enemies survive. |

> **Note:** A wrong-colour enemy acts as an absolute veto. Even if a right-colour enemy is also present inside, the claim is cancelled entirely.

### 2.5 Colour Mechanics

The player has two colours: **red** and **blue**. The active colour determines which enemies are friendly (ignore the line) and which are hostile (cancel the line on contact).

| Condition | Result |
|---|---|
| Drawing blue — red enemy touches line | Line cancelled. Player snaps to border. |
| Drawing blue — blue enemy touches line | Nothing happens. Blue is friendly to a blue line. |
| Drawing red — blue enemy touches line | Line cancelled. Player snaps to border. |
| Drawing red — red enemy touches line | Nothing happens. Red is friendly to a red line. |

**Summary: your colour = your team. Your team ignores your line. The opposing team cancels it.**

> **Note:** There is no death mechanic. A cancelled line is the only consequence of an enemy interaction. The player simply tries again.

### 2.6 Enemy Behaviour

Enemies drift slowly and smoothly around the unclaimed space. They do not actively chase the player — their movement is ambient and meditative, not aggressive. Speed and quantity increase with difficulty.

Two enemies of opposite colours in close proximity are effectively **immune to capture** until the player separates them with careful cuts. This is the primary source of strategic depth.

When a claim is cancelled (wrong-colour enemy inside), all enemies inside continue drifting freely. There is no teleportation or special behaviour.

### 2.7 Win Condition

A round is won when **both** of the following are true simultaneously:

- The claimed area equals or exceeds the fill threshold.
- All enemies have been eliminated.

Either condition can be met first. The player may clear all enemies early and then claim territory freely, or hit the fill threshold and then hunt remaining enemies. Both create a satisfying cooldown phase after the tension of the main game.

### 2.8 Difficulty

Chosen before each session on the play screen. Enemy count is always evenly split between red and blue.

| Difficulty | Enemies | Fill Threshold | Enemy Speed |
|---|---|---|---|
| Easy | 2 (1R + 1B) | 65% | Slow |
| Normal | 4 (2R + 2B) | 75% | Medium |
| Hard | 6 (3R + 3B) | 80% | Fast |

---

## 3. Controls

| Input | Action |
|---|---|
| Arrow Keys / WASD | Move the player marker |
| Space | Switch colour (red ↔ blue). Can be used mid-line to dynamically change the entire line's colour and dodge enemies. |
| Escape | Open in-game pause menu |

> Controller support is a stretch goal. The game is designed primarily around keyboard.

---

## 4. Screens & Application Flow

### 4.1 Screen List

- EULA Screen *(first launch only)*
- Main Menu
- Play Setup Screen
- Game Screen
- Round Complete Screen
- Credits Screen

### 4.2 EULA Screen

Shown once on first launch. Displays the End User Licence Agreement. Player must click Accept to proceed. Acceptance is stored in user settings and the screen is never shown again.

### 4.3 Main Menu

- **Play** → Play Setup Screen
- **Settings** → Settings panel (fullscreen/window toggle, custom image folder path)
- **Credits** → Credits Screen
- **Quit**

No score display, no leaderboard, no save state. Intentionally minimal.

### 4.4 Play Setup Screen

Before each session the player chooses:

- **Difficulty:** Easy / Normal / Hard
- **Collection:** Bundled paintings or Custom folder

Pressing Play loads a random painting from the selected collection and begins the game.

### 4.5 Game Screen

Full-screen playfield. HUD is minimal:

- Current colour indicator (small red or blue dot, corner of screen)
- Fill percentage progress bar (subtle)
- Enemy count remaining

No score. No timer.

### 4.6 Round Complete Screen

When a round is won, the painting is shown in full colour with its **title and artist name** for a few seconds. This is the reward moment — the player sees the completed artwork. After a short pause (or a keypress) a new random painting loads automatically.

### 4.7 Pause / Escape Menu

A simple overlay triggered by Escape during play:

- Resume
- New Painting *(loads a new random painting, same difficulty and collection)*
- Quit to Main Menu

### 4.8 Credits Screen

- Game concept and development
- Paintings sourced from Artvee and other public domain archives
- Music sourced from Musopen (public domain classical recordings)
- Godot Engine (MIT licence)

---

## 5. Assets

### 5.1 Paintings

All paintings are public domain works sourced manually from:

- **Artvee** (artvee.com) — curated high-resolution public domain paintings
- **The Metropolitan Museum of Art Open Access** (metmuseum.org)
- **National Gallery of Art** (nga.gov)
- Other CC0 museum archives as needed

**Curation criteria:**
- Landscape or near-square orientation preferred (avoids extreme letterboxing)
- Rich in colour — the greyscale-to-colour reveal should feel dramatic
- Strong readable composition — recognisable even at partial completion
- Variety of subjects: landscapes, still life, mythology, portraiture

**Processing pipeline:**
1. Download original high-resolution files
2. Resize and letterbox to 1920 × 1080 (preserve aspect ratio, dark bars where needed)
3. Save as JPEG at quality balancing file size and fidelity
4. Place in `res://paintings/bundled/`

Custom paintings are loaded at runtime from a user-configured system folder. Supported formats: PNG, JPG.

> Greyscale conversion is **not** pre-processed. A desaturation shader is applied at runtime to unclaimed areas. Only one version of each image is stored.

### 5.2 Music

All music sourced from **Musopen** (musopen.org) — public domain recordings of classical works. An intentional pairing: public domain art with public domain music.

- **Style:** Calm instrumental classical — chamber music, piano, string quartets
- **Format:** OGG
- **Playback:** Tracks shuffle randomly, loop or cycle continuously
- **Volume:** Soft background level, never competing with gameplay

### 5.3 Audio Identity

The entire sound design follows a single creative rule: **every sound is either a painting sound or a music sound.** There are no generic UI bleeps or electronic effects. The player is an artist at a canvas, accompanied by a small chamber ensemble.

#### Painting Sounds (actions and feedback)

| Event | Sound |
|---|---|
| Player starts drawing a line | Soft brush-on-canvas stroke |
| Player moving along safe border | Faint dry brush texture (subtle, nearly silent) |
| Area successfully claimed | Wet paint brush sweep — full and satisfying |
| Enemy eliminated | Paint drop hitting palette — a soft, clean plop |
| Line cancelled (enemy contact) | Brush lifted from canvas — a gentle, non-alarming drag |

#### Music Sounds (UI and milestones)

| Event | Sound |
|---|---|
| Menu item selected / button press | Single soft piano key or string pluck |
| Menu item hovered / navigation | Muted pizzicato string note |
| Round complete | Warm resolving chord — strings or piano, major key |
| New painting loaded | Gentle harp glissando or soft piano arpeggio |
| Colour switched (Space) | Two-note interval — low to high for blue, high to low for red |

> All sounds should reinforce relaxation. Nothing harsh, sudden, or alarming. The audio goal is that the game feels like sitting in a quiet gallery with a string quartet playing softly in the next room.

---

## 6. Technical Design

### 6.1 Engine & Language

| | |
|---|---|
| Engine | Godot 4 |
| Scripting | C# (.NET) |
| Auto-update | Velopack |
| Design resolution | 1920 × 1080 |
| Rendering | 2D |

### 6.2 Geometry & Rendering

The game uses a **vector-based approach**, consistent with the original Qix rather than a grid.

- The player's active line is stored as a `PackedVector2Array` of **waypoints**. A new point is recorded only when the player changes direction or closes the shape — not continuously during movement. Each segment between consecutive waypoints is guaranteed to travel in one of 8 directions (horizontal, vertical, or 45° diagonal).
- On completion the line is closed into a polygon. Area is calculated using the **Shoelace formula**. The smaller area is always claimed.
- The painting `Sprite2D` carries a `ShaderMaterial` using `reveal.gdshader`. The shader reads the painting texture and a `mask_texture` uniform — a greyscale-encoded state map produced by a `SubViewport` (same resolution as the painting).
- The mask encodes zone state by RGB channel: black `(0,0,0)` = free (rendered greyscale), white `(1,1,1)` = claimed (full colour), red `(1,0,0)` = claimed with red tint, blue `(0,0,1)` = claimed with blue tint.
- When an area is claimed, a `Polygon2D` of the appropriate mask colour is added to the SubViewport's `MaskRoot` node. The viewport texture is passed to the shader via `SetShaderParameter("mask_texture", ...)`. Because SubViewport renders one frame later, the parameter update is deferred with `await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame)`.
- A border is drawn into the mask at load time as four thin white rectangles along the painting edges. This border serves as the player's starting safe ground and as a positional reference for game elements.
- The active drawing line is also a node inside the SubViewport (`Line2D` added to `MaskRoot`), coloured red `(1,0,0)` or blue `(0,0,1)` to match the player's current draw colour. The shader blends it with the painting in real time. On cancellation the node is removed; on successful completion it is removed and replaced with a `Polygon2D` of the same colour.
- Enemy-inside check at claim time uses a standard **point-in-polygon test** for each enemy.
- Polygon subtraction (removing the newly claimed polygon from the remaining free space) is the most complex geometric operation. Evaluate **Clipper2** (C# port) vs. a bespoke solution.

### 6.3 Image Loading

- **Bundled:** Loaded from `res://paintings/bundled/` via standard Godot resource loading.
- **Custom:** Loaded at runtime from a user-configured absolute path using `DirAccess` and `FileAccess`, then converted to `ImageTexture`.
- On session start, available images from the selected collection are shuffled. Each round picks the next in the shuffled list, cycling when exhausted.

### 6.4 Settings Persistence

Stored via Godot's `ConfigFile` to `user://settings.cfg`:

| Key | Type | Description |
|---|---|---|
| `fullscreen` | bool | Fullscreen or windowed |
| `custom_folder_path` | string | Absolute path to custom images folder |
| `eula_accepted` | bool | Whether EULA has been accepted |
| `last_difficulty` | int | 0=Easy, 1=Normal, 2=Hard |
| `last_collection` | enum | `bundled` or `custom` |

### 6.5 Auto-Update

Velopack handles automatic updates on Windows. On launch, the app checks for a new release on GitHub. If available, it downloads in the background and prompts the player to restart to apply. macOS and Linux updates are manual via the GitHub releases page.

---

## 7. Suggested Godot Scene Structure

> This is a starting point, not a strict requirement. Adjust as implementation evolves.

### Scenes

| Path | Purpose |
|---|---|
| `res://scenes/main_menu.tscn` | Main menu |
| `res://scenes/play_setup.tscn` | Difficulty and collection selection |
| `res://scenes/game.tscn` | Main playfield — painting, polygons, player, enemies, HUD |
| `res://scenes/round_complete.tscn` | Full-colour painting reveal with title and artist |
| `res://scenes/credits.tscn` | Credits screen |
| `res://scenes/eula.tscn` | EULA on first launch |
| `res://scenes/pause_menu.tscn` | Overlay pause menu |

### Scripts

| Path | Responsibility |
|---|---|
| `res://scripts/GameManager.cs` | Session state: current painting, enemy list, fill %, win condition check |
| `res://scripts/Player.cs` | Movement, line drawing, colour switching, line cancellation |
| `res://scripts/Enemy.cs` | Drift movement, colour property, collision with active line |
| `res://scripts/PolygonManager.cs` | Polygon creation, Shoelace area, point-in-polygon, Polygon2D spawning |
| `res://scripts/ImageLoader.cs` | Load bundled and custom paintings, shuffle list, provide next image |
| `res://scripts/SettingsManager.cs` | Read/write ConfigFile, expose settings to UI |
| `res://scripts/MusicPlayer.cs` | Shuffle and cycle music tracks, manage playback |
| `res://scripts/AudioManager.cs` | Play painting and music sound effects by event name |

### Resources

| Path | Contents |
|---|---|
| `res://paintings/bundled/` | Pre-processed 1920×1080 JPEG paintings |
| `res://music/` | OGG classical tracks from Musopen |
| `res://sfx/paint/` | Brush, drip, sweep sound effects |
| `res://sfx/music/` | Piano keys, string plucks, chords, glissandos |
| `res://shaders/reveal.gdshader` | Desaturation shader — blends full-colour painting with greyscale based on mask_texture state |

---

## 8. Out of Scope (v1.0)

- Mobile (iOS / Android)
- Web export
- Score or leaderboard of any kind
- Online multiplayer
- Controller support *(stretch goal only)*
- Level editor or custom enemy placement
- Achievements
- Localisation *(English only for v1.0)*

---

## 9. Open Questions

- **Polygon subtraction library:** Evaluate Clipper2 C# port vs. a bespoke solution for the Godot Polygon2D use case.
- **Fill percentage display:** Progress bar, numeric %, or fully hidden (trust the player to feel progress visually)?
- **Round complete timing:** How many seconds to display the completed painting before auto-advancing?
- **Music bundle size:** Minimum viable track count vs. download size — how many tracks ship with v1.0?
- **Custom folder validation:** Validate images on load (check format, minimum dimensions) or fail silently on bad files?
- **Enemy spawn positions:** Fully random within free space, or enforce a minimum distance from the player start position?
- **Colour switch audio:** Should the two-note interval for switching colour play even when on the border (idle), or only when actively drawing?

---

*Chromonia — public domain art, public domain music, endless calm.*