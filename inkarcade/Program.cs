// InkArcade Engine — interactive demo
// Arrow keys: move  |  P: pause  |  Esc: quit

using InkArcade;

try
{
    var engine = new TerminalEngine();
    RunDemo(engine);
}
catch (InvalidOperationException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(ex.Message);
    Console.ResetColor();
    Environment.Exit(1);
}

static void RunDemo(TerminalEngine engine)
{
    // ── Declare sprites ───────────────────────────────────────────────────────

    string[] playerArt = { " ▲ ", "███" };
    var player = engine.CreateSprite(playerArt,
        engine.Width / 2, engine.Height / 2, tag: "player", color: ConsoleColor.Cyan);

    string[] blockArt = { "▓▓▓▓▓", "▓▓▓▓▓" };
    engine.CreateSprite(blockArt, 20,                    10, tag: "obstacle", color: ConsoleColor.Red);
    engine.CreateSprite(blockArt, engine.Width  - 25,    10, tag: "obstacle", color: ConsoleColor.Red);
    engine.CreateSprite(blockArt, engine.Width  / 2 - 2,  5, tag: "obstacle", color: ConsoleColor.Red);
    engine.CreateSprite(blockArt, engine.Width  / 2 - 2, engine.Height - 8, tag: "obstacle", color: ConsoleColor.Red);

    // ── Game loop ─────────────────────────────────────────────────────────────

    engine.Run(() =>
    {
        // Input
        var key = engine.PollInput();
        if (key == ConsoleKey.LeftArrow  && player.X > 0)                              engine.MoveSprite(player, -1,  0);
        if (key == ConsoleKey.RightArrow && player.X < engine.Width  - player.Width)   engine.MoveSprite(player,  1,  0);
        if (key == ConsoleKey.UpArrow    && player.Y > 0)                              engine.MoveSprite(player,  0, -1);
        if (key == ConsoleKey.DownArrow  && player.Y < engine.Height - player.Height)  engine.MoveSprite(player,  0,  1);
        if (key == ConsoleKey.Escape) engine.Stop();
        if (key == ConsoleKey.P)      engine.TogglePause();

        // Collision check
        bool touching = engine.GetCollisions(player, "obstacle").Count > 0;

        // HUD — DrawText overlays render on top of sprites, cleared each frame
        engine.DrawText(0, 0, "InkArcade Engine  |  Arrow keys: move   P: pause   Esc: quit");
        engine.DrawText(0, 1, $"Position: ({player.X,3}, {player.Y,3})   {(touching ? "[ COLLISION ]" : "             ")}   {(engine.IsPaused ? "[ PAUSED ]" : "          ")}");
    });
}
