namespace InkArcade;

// ─────────────────────────────────────────────────────────────────────────────
//  RainDrops — game assets, title screen, and playfield setup
//  Call RainDropGame.RunGame(engine) from Program.cs to start.
// ─────────────────────────────────────────────────────────────────────────────

public static class RainDropGame
{
    // ── 1. Title art ─────────────────────────────────────────────────────────
    // "RAINDROPS" in 5-row block pixel letters.
    // Each letter is 5 chars wide; all 9 letters = 45 chars total.
    // Center in a 100-wide terminal at x = (100 - 45) / 2 = 27.

    public static readonly string[] TitleArt =
    {
        "▓▓▓   ▓▓  ▓▓▓  ▓  ▓ ▓▓▓  ▓▓▓   ▓▓  ▓▓▓   ▓▓▓ ",
        "▓  ▓ ▓  ▓  ▓   ▓▓ ▓ ▓  ▓ ▓  ▓ ▓  ▓ ▓  ▓ ▓    ",
        "▓▓▓  ▓▓▓▓  ▓   ▓ ▓▓ ▓  ▓ ▓▓▓  ▓  ▓ ▓▓▓   ▓▓  ",
        "▓ ▓  ▓  ▓  ▓   ▓  ▓ ▓  ▓ ▓ ▓  ▓  ▓ ▓       ▓ ",
        "▓  ▓ ▓  ▓ ▓▓▓  ▓  ▓ ▓▓▓  ▓  ▓  ▓▓  ▓    ▓▓▓  ",
    };

    // ── 2. Screen dividers ────────────────────────────────────────────────────
    // Two rows / two columns of alternating ▪ dots give a thick dotted texture.

    public static string[] MakeHorizDivider(int width)
    {
        var row0 = new char[width];
        var row1 = new char[width];
        for (int i = 0; i < width; i++)
        {
            row0[i] = i % 2 == 0 ? '▪' : ' ';
            row1[i] = i % 2 == 0 ? ' ' : '▪';
        }
        return [new string(row0), new string(row1)];
    }

    public static string[] MakeVertDivider(int height)
    {
        var lines = new string[height];
        for (int i = 0; i < height; i++)
            lines[i] = i % 2 == 0 ? "▪ " : " ▪";
        return lines;
    }

    // Creates all four divider sprites and positions them to quarter the screen.
    public static void SetupPlayfield(TerminalEngine engine)
    {
        int midX = engine.Width  / 2;
        int midY = engine.Height / 2;

        engine.CreateSprite(MakeHorizDivider(engine.Width), 0,    midY, "divider", ConsoleColor.DarkGray);
        engine.CreateSprite(MakeVertDivider(engine.Height), midX, 0,    "divider", ConsoleColor.DarkGray);
    }

    // ── 3. Raindrop ───────────────────────────────────────────────────────────
    // Classic teardrop shape — pointed tip at top, rounded base.
    // 5 wide × 6 tall. Color: ConsoleColor.Cyan

    public static readonly string[] RainDropArt =
    {
        "  ╷  ",
        " ╱▓╲ ",
        "▓▓▓▓▓",
        "▓▓▓▓▓",
        " ▓▓▓ ",
        "  ▓  ",
    };

    public static Sprite CreateRainDrop(TerminalEngine engine, int x, int y)
        => engine.CreateSprite(RainDropArt, x, y, "raindrop", ConsoleColor.Cyan);

    // ── 4. Shirt ──────────────────────────────────────────────────────────────
    // V-collar shirt with box-drawing outline.
    // 7 wide × 6 tall. Color: ConsoleColor.Green

    public static readonly string[] ShirtArt =
    {
        "╲     ╱",
        " ╲   ╱ ",
        " ╔═══╗ ",
        " ║   ║ ",
        " ║   ║ ",
        " ╚═══╝ ",
    };

    public static Sprite CreateShirt(TerminalEngine engine, int x, int y)
        => engine.CreateSprite(ShirtArt, x, y, "shirt", ConsoleColor.Green);

    // ── 5. Boy ────────────────────────────────────────────────────────────────
    // Three sprites that stack vertically to form the boy character.
    // All parts are 7 chars wide so they align at the same x position.
    //
    //  BoyHatArt   — 2 rows — ConsoleColor.Yellow
    //  BoyFaceArt  — 7 rows — ConsoleColor.White  (head + torso)
    //  BoyPantsArt — 3 rows — ConsoleColor.Blue

    public static readonly string[] BoyHatArt =
    {
        "  ▄▄▄  ",
        "▐█████▌",
    };

    public static readonly string[] BoyFaceArt =
    {
        " ┌───┐ ",
        " │o o│ ",
        " │ ω │ ",
        " └─┬─┘ ",
        "┌──┴──┐",
        "│     │",
        "└─────┘",
    };

    public static readonly string[] BoyPantsArt =
    {
        "┌──┬──┐",
        "│  │  │",
        "└──┘──┘",
    };

    // Creates all three boy parts at (x, y) and returns them as a tuple.
    public static (Sprite hat, Sprite face, Sprite pants) CreateBoy(TerminalEngine engine, int x, int y)
    {
        var hat   = engine.CreateSprite(BoyHatArt,   x, y,                                          "boy", ConsoleColor.Yellow);
        var face  = engine.CreateSprite(BoyFaceArt,  x, y + BoyHatArt.Length,                       "boy", ConsoleColor.White);
        var pants = engine.CreateSprite(BoyPantsArt, x, y + BoyHatArt.Length + BoyFaceArt.Length,   "boy", ConsoleColor.Blue);
        return (hat, face, pants);
    }

    // ── Title screen ──────────────────────────────────────────────────────────
    // Phase 1 — title reveals one column per frame (left → right, ~0.75 s at 60 fps).
    // Phase 2 — title blinks 5 times.
    // Phase 3 — "Press any key" prompt; waits for input.
    // Returns true when the player presses a key, false if they pressed Escape.

    public static bool ShowTitleScreen(TerminalEngine engine)
    {
        int fps   = engine.TargetFps;
        int frame = 0;
        bool revealDone  = false;
        int  blinkFrame  = 0;
        bool waitForKey  = false;
        bool gameStarted = false;

        int titleW = TitleArt[0].Length;                              // 45
        int titleX = (engine.Width  - titleW)          / 2;
        int titleY = Math.Max(2, (engine.Height - TitleArt.Length) / 2 - 3);

        const string Prompt  = "·  Press any key to start  ·";
        int promptX = (engine.Width - Prompt.Length) / 2;
        int promptY = titleY + TitleArt.Length + 2;

        engine.Run(() =>
        {
            var key = engine.PollInput();
            if (key == ConsoleKey.Escape) { engine.Stop(); return; }
            if (waitForKey && key.HasValue) { gameStarted = true; engine.Stop(); return; }

            frame++;

            // ── Phase 1: reveal ───────────────────────────────────────────────
            int revealCols = revealDone ? titleW : Math.Min(frame, titleW);
            if (!revealDone && revealCols >= titleW) { revealDone = true; blinkFrame = 0; }

            // ── Phase 2: blink ────────────────────────────────────────────────
            bool showTitle = true;
            if (revealDone && !waitForKey)
            {
                blinkFrame++;
                int half = Math.Max(1, fps / 3);             // ~0.33 s per half-blink
                showTitle = (blinkFrame / half) % 2 == 0;
                if (blinkFrame >= half * 10)                  // 5 full blinks done
                {
                    waitForKey = true;
                    showTitle  = true;
                }
            }

            // ── Draw title ────────────────────────────────────────────────────
            if (showTitle)
            {
                for (int r = 0; r < TitleArt.Length; r++)
                    engine.DrawText(titleX, titleY + r, TitleArt[r][..revealCols], ConsoleColor.Cyan);
            }

            // ── Draw prompt ───────────────────────────────────────────────────
            if (waitForKey)
                engine.DrawText(promptX, promptY, Prompt, ConsoleColor.Yellow);
        });

        return gameStarted;
    }

    // ── RunGame ───────────────────────────────────────────────────────────────
    // Entry point for the full game. Call this from Program.cs.

    public static void RunGame(TerminalEngine engine)
    {
        if (!ShowTitleScreen(engine)) return;

        SetupPlayfield(engine);

        // Preview all assets in their quadrants until Esc
        var (_, _, _) = CreateBoy(engine, 4, 4);
        CreateShirt(engine,    engine.Width / 2 + 4, 4);
        CreateRainDrop(engine, engine.Width / 2 + 4, engine.Height / 2 + 4);

        engine.Run(() =>
        {
            var key = engine.PollInput();
            if (key == ConsoleKey.Escape) engine.Stop();
            engine.DrawText(0, 0, "RainDrops  |  Esc: quit", ConsoleColor.DarkGray);
        });
    }
}
