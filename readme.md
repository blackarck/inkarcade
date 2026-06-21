# InkArcade — Terminal Blaster

A fast-paced **terminal-based arcade shooter** powered by a lightweight C# game engine that runs entirely inside your console. No dependencies, no frameworks — just .NET 10 and a terminal.

![Title Screen](images/Title.jpg)
![Gameplay](images/terminalBlaster.gif)
![In Action](images/gamePlay.jpg)

---

## Quick Start

```bash
git clone https://github.com/blackarck/inkarcade.git
cd inkarcade/TerminalBlaster
dotnet run
```

**Requirements:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Terminal window at least **100 columns × 30 rows** (120×35 recommended)
- A monospace font — Menlo, JetBrains Mono, or Consolas

**Controls:**

| Key | Action |
|-----|--------|
| `←` / `→` | Move |
| `Space` | Fire |

---

## Architecture

The project is split into three files with a clear separation of concerns:

```
TerminalBlaster/
├── Engine.cs   → TerminalEngine + Sprite  (no game knowledge)
├── Game.cs     → game logic using the engine API
└── Program.cs  → entry point, title screen, game-over screen
```

The engine knows nothing about aliens or guns. `Game.cs` knows nothing about render buffers or console APIs. `Program.cs` is ~40 lines.

---

## Engine Reference

### `TerminalEngine`

```csharp
var engine = new TerminalEngine();           // reads Console.WindowWidth/Height
var engine = new TerminalEngine(120, 35);    // explicit size
```

Throws `InvalidOperationException` if the terminal is smaller than the minimum (100×30).

#### Sprite lifecycle

```csharp
// Declare an asset — art is a string[] of equal-width lines
Sprite player = engine.CreateSprite(art, x, y, tag: "player");

// Remove it from the world
engine.DestroySprite(sprite);
```

#### Movement

```csharp
engine.MoveSprite(sprite, dx, dy);        // relative — moves by dx, dy
engine.SetPosition(sprite, x, y);         // absolute — teleport
```

#### Querying

```csharp
// Returns a snapshot list — safe to destroy sprites while iterating
List<Sprite> enemies = engine.GetSpritesByTag("enemy");
```

#### Collision

```csharp
// AABB bounding-box overlap between two sprites
bool hit = engine.Overlaps(a, b);

// All sprites of a given tag that overlap the given sprite
List<Sprite> hits = engine.GetCollisions(sprite, "enemy");
```

#### Input

```csharp
// Non-blocking — returns null if no key is waiting
ConsoleKey? key = engine.PollInput();
```

#### Text overlays

```csharp
// Drawn on top of sprites, cleared after each frame.
// Call this every frame from your update function.
engine.DrawText(x, y, "Score: 1200");
```

#### Game loop

```csharp
engine.Run(game.Update, targetFps: 60);   // blocks until Stop() is called
engine.Stop();                            // exits the loop
```

---

### `Sprite`

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Unique identifier |
| `Art` | `string[]` | ASCII art lines (equal-width) |
| `X`, `Y` | `int` | Top-left position in terminal cells |
| `Width` | `int` | `Art[0].Length` |
| `Height` | `int` | `Art.Length` |
| `Tag` | `string` | Grouping label used for queries and collision |
| `Visible` | `bool` | Excluded from rendering when `false` |

---

## How the Renderer Works

Each frame the engine:

1. Fills a `StringBuilder` of `width × height` characters with spaces
2. Blits each visible sprite into the buffer at `y * width + x`
3. Blits text overlays on top
4. Compares the result to the previous frame — skips `Console.Write` if nothing changed

This diff check is why there's no flicker even at 60 fps.

---

## Writing a Game with the Engine

```csharp
using TerminalBlaster;

var engine = new TerminalEngine();

// Declare assets
string[] shipArt = { " ▲ ", "███" };
var ship = engine.CreateSprite(shipArt, engine.Width / 2, engine.Height - 3, "player");

engine.Run(() =>
{
    // Input
    var key = engine.PollInput();
    if (key == ConsoleKey.LeftArrow)  engine.MoveSprite(ship, -1, 0);
    if (key == ConsoleKey.RightArrow) engine.MoveSprite(ship,  1, 0);

    // HUD (re-draw every frame)
    engine.DrawText(0, 0, "InkArcade Engine");
});
```

The engine owns the render loop and the sprite dictionary. Your game owns everything else — positions, HP, state machines, whatever you need.

---

## Terminal Size

The engine enforces a minimum at startup:

```
Terminal is 80×24 — minimum required is 100×30.
Resize your terminal window and try again.
```

| Size | Notes |
|------|-------|
| 100×30 | Minimum supported |
| 120×35 | Recommended — comfortable margins |
| 160×45 | Wide mode — more room for larger waves |

**macOS:** Terminal → Preferences → Profiles → Window → set Columns and Rows before launching.

---

## Features

- Wave-based enemy progression with increasing bullet speed
- 5 enemy variants and 5 pillar variants, randomised each game
- Bounding-box collision for bullets and player
- Slide-in title screen animation
- Diff-based renderer — no flicker, minimal redraws
- macOS / Linux / Windows compatible (no platform-specific APIs in game logic)

---

## Ideas for Extension

- Power-ups and weapon upgrades (new sprite tags + collision logic)
- Color support via `Console.ForegroundColor` in the renderer (see `BlitLine`)
- Sound via `Console.Beep()` or a lightweight audio library
- High-score persistence to a local file or API
- A second scene / game built on the same engine

---

## Project Notes

Things learned building this:

- Colors caused severe flickering — dropped in favour of pure ASCII
- `System.Timers.Timer` introduced timing jitter — replaced with a plain `while` loop and `Thread.Sleep`
- Unicode box-drawing characters (`█ ▄ ╔ ║`) are single `char` values in C#, so buffer position math stays correct
- `Console.SetBufferSize` is Windows-only — wrapped in `OperatingSystem.IsWindows()`

---

## Contributing

Fork → branch → commit → pull request. Screenshots welcome.

---

## License

MIT — see [LICENSE](LICENSE).
