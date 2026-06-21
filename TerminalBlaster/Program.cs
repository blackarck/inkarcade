// =========================
//  Terminal Blaster
//  A console-based shooting game in C#
//  author - blackarck  |  nov 2025
// =========================

using System.Text;
using TerminalBlaster;

try
{
    var engine = new TerminalEngine();
    ShowTitle(engine.Width, engine.Height);

    var game = new Game(engine);
    engine.Run(game.Update);

    // Game over screen (shown after engine loop exits)
    Console.Clear();
    Console.SetCursorPosition(engine.Width / 2 - 5, engine.Height / 2);
    Console.Write("GAME OVER");
    Console.SetCursorPosition(engine.Width / 2 - 7, engine.Height / 2 + 1);
    Console.Write($"Final Score: {game.Score}");
    Console.ReadKey(true);
}
catch (InvalidOperationException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(ex.Message);
    Console.ResetColor();
    Environment.Exit(1);
}

// ── Title screen ──────────────────────────────────────────────────────────────

static void ShowTitle(int width, int height)
{
    string[] titleArt =
    {
        @" ██████╗ ██████╗ ███╗   ██╗███████╗ ██████╗ ██╗     ███████╗     ██████╗ ██╗   ██╗███╗   ██╗",
        @"██╔════╝██╔═══██╗████╗  ██║██╔════╝██╔═══██╗██║     ██╔════╝    ██╔════╝ ██║   ██║████╗  ██║",
        @"██║     ██║   ██║██╔██╗ ██║███████╗██║   ██║██║     █████╗      ██║  ███╗██║   ██║██╔██╗ ██║",
        @"██║     ██║   ██║██║╚██╗██║╚════██║██║   ██║██║     ██╔══╝      ██║   ██║██║   ██║██║╚██╗██║",
        @"╚██████╗╚██████╔╝██║ ╚████║███████║╚██████╔╝███████╗███████╗    ╚██████╔╝╚██████╔╝██║ ╚████║",
        @" ╚═════╝ ╚═════╝ ╚═╝  ╚═══╝╚══════╝ ╚═════╝ ╚══════╝╚══════╝     ╚═════╝  ╚═════╝ ╚═╝  ╚═══╝"
    };

    Console.OutputEncoding = System.Text.Encoding.UTF8;
    Console.CursorVisible  = false;

    var buf  = new StringBuilder(width * height);
    int artW = titleArt[0].Length;
    int finalX = (width - artW) / 2;
    int startX = width;                    // slide in from right

    while (startX > finalX)
    {
        buf.Clear();
        buf.Append(' ', width * height);

        for (int row = 0; row < titleArt.Length; row++)
        {
            int y   = (height - titleArt.Length) / 2 + row;
            int col = Math.Max(0, startX);
            int len = Math.Min(artW, width - col);
            int off = col - startX;
            if (y >= 0 && y < height && len > 0)
                for (int i = 0; i < len; i++)
                    buf[y * width + col + i] = titleArt[row][off + i];
        }

        Console.SetCursorPosition(0, 0);
        Console.Write(buf.ToString());
        startX -= 2;
        Thread.Sleep(30);
    }

    // "Press any key" prompt
    string prompt = "[ Press any key to start ]";
    int py = (height + titleArt.Length) / 2 + 2;
    int px = (width - prompt.Length) / 2;
    if (py >= 0 && py < height && px >= 0 && px + prompt.Length <= width)
        for (int i = 0; i < prompt.Length; i++)
            buf[py * width + px + i] = prompt[i];

    Console.SetCursorPosition(0, 0);
    Console.Write(buf.ToString());
    Console.ReadKey(true);
    Console.Clear();
}
