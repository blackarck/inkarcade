namespace InkArcade;

using System.Text;

// ── Sprite ───────────────────────────────────────────────────────────────────

public class Sprite
{
    public int            Id      { get; }
    public string[]       Art     { get; set; }
    public int            X       { get; internal set; }
    public int            Y       { get; internal set; }
    public int            Width   => Art.Length > 0 ? Art[0].Length : 0;
    public int            Height  => Art.Length;
    public string         Tag     { get; set; }
    public bool           Visible { get; set; } = true;
    public ConsoleColor?  Color   { get; set; }

    internal Sprite(int id, string[] art, int x, int y, string tag)
        => (Id, Art, X, Y, Tag) = (id, art, x, y, tag);
}

// ── Engine ───────────────────────────────────────────────────────────────────

public class TerminalEngine
{
    public const int MinWidth  = 100;
    public const int MinHeight = 30;

    public int Width     { get; }
    public int Height    { get; }
    public int TargetFps { get; }

    private readonly Dictionary<int, Sprite> _sprites = new();
    private int _nextId;

    private readonly (char Ch, ConsoleColor? Fg)[] _cells;
    private readonly (char Ch, ConsoleColor? Fg)[] _prevCells;

    private readonly List<(int x, int y, string text, ConsoleColor? color)> _overlays = new();
    private bool _running;
    private bool _paused;

    public bool IsPaused => _paused;

    // ── Init ─────────────────────────────────────────────────────────────────

    public TerminalEngine(int? width = null, int? height = null, int targetFps = 60)
    {
        int w = width  ?? Console.WindowWidth;
        int h = height ?? Console.WindowHeight;

        if (w < MinWidth || h < MinHeight)
            throw new InvalidOperationException(
                $"Terminal is {w}×{h} — minimum required is {MinWidth}×{MinHeight}.\n" +
                $"Resize your terminal window and try again.");

        Width     = w;
        Height    = h;
        TargetFps = targetFps;
        _cells     = new (char, ConsoleColor?)[Width * Height];
        _prevCells = new (char, ConsoleColor?)[Width * Height];

        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible  = false;
        if (OperatingSystem.IsWindows())
            Console.SetBufferSize(Width, Height);
    }

    // ── Sprite lifecycle ──────────────────────────────────────────────────────

    public Sprite CreateSprite(string[] art, int x, int y, string tag = "", ConsoleColor? color = null)
    {
        var s = new Sprite(_nextId++, art, x, y, tag) { Color = color };
        _sprites[s.Id] = s;
        return s;
    }

    public void DestroySprite(Sprite sprite) => _sprites.Remove(sprite.Id);

    // ── Movement ──────────────────────────────────────────────────────────────

    public void MoveSprite(Sprite sprite, int dx, int dy)
        => (sprite.X, sprite.Y) = (sprite.X + dx, sprite.Y + dy);

    public void SetPosition(Sprite sprite, int x, int y)
        => (sprite.X, sprite.Y) = (x, y);

    // ── Query ─────────────────────────────────────────────────────────────────

    /// Returns a snapshot — safe to destroy sprites while iterating the result.
    public List<Sprite> GetSpritesByTag(string tag)
    {
        var list = new List<Sprite>();
        foreach (var s in _sprites.Values)
            if (s.Tag == tag) list.Add(s);
        return list;
    }

    // ── Collision ─────────────────────────────────────────────────────────────

    public bool Overlaps(Sprite a, Sprite b)
        => a.X < b.X + b.Width  && a.X + a.Width  > b.X &&
           a.Y < b.Y + b.Height && a.Y + a.Height > b.Y;

    public List<Sprite> GetCollisions(Sprite sprite, string tag)
    {
        var hits = new List<Sprite>();
        foreach (var other in GetSpritesByTag(tag))
            if (Overlaps(sprite, other))
                hits.Add(other);
        return hits;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public ConsoleKey? PollInput()
        => Console.KeyAvailable ? Console.ReadKey(true).Key : null;

    // ── Text overlays ─────────────────────────────────────────────────────────
    // Call DrawText each frame from your update function.
    // Overlays render on top of sprites and are cleared after each frame.

    public void DrawText(int x, int y, string text, ConsoleColor? color = null)
        => _overlays.Add((x, y, text, color));

    // ── Game loop ─────────────────────────────────────────────────────────────

    public void Run(Action update, int? targetFps = null)
    {
        int msPerFrame = 1000 / (targetFps ?? TargetFps);
        _running = true;
        while (_running)
        {
            if (!_paused)
            {
                update();
                Render();
            }
            Thread.Sleep(msPerFrame);
        }
    }

    public void Stop()        => _running = false;
    public void Pause()       => _paused  = true;
    public void Resume()      => _paused  = false;
    public void TogglePause() => _paused  = !_paused;

    // ── Internal renderer ─────────────────────────────────────────────────────

    private void Render()
    {
        Array.Fill(_cells, (' ', (ConsoleColor?)null));

        foreach (var sprite in _sprites.Values)
            if (sprite.Visible)
                BlitLines(sprite.X, sprite.Y, sprite.Art, sprite.Color);

        foreach (var (ox, oy, text, color) in _overlays)
            BlitLine(ox, oy, text, color);
        _overlays.Clear();

        if (_cells.AsSpan().SequenceEqual(_prevCells.AsSpan())) return;

        Console.SetCursorPosition(0, 0);
        Console.Write(BuildFrame());
        _cells.CopyTo(_prevCells, 0);
    }

    private void BlitLines(int x, int y, string[] lines, ConsoleColor? color)
    {
        for (int row = 0; row < lines.Length; row++)
            BlitLine(x, y + row, lines[row], color);
    }

    private void BlitLine(int x, int y, string text, ConsoleColor? color)
    {
        if (y < 0 || y >= Height || x >= Width) return;
        int col0   = Math.Max(0, x);
        int colEnd = Math.Min(x + text.Length, Width);
        int len    = colEnd - col0;
        if (len <= 0) return;
        int pos    = y * Width + col0;
        int offset = col0 - x;             // >0 when sprite clips left edge
        for (int i = 0; i < len; i++)
            _cells[pos + i] = (text[offset + i], color);
    }

    private string BuildFrame()
    {
        var sb = new StringBuilder(_cells.Length + 64);
        ConsoleColor? current = null;

        foreach (var (ch, fg) in _cells)
        {
            if (fg != current)
            {
                sb.Append(fg.HasValue ? AnsiCode(fg.Value) : "\x1b[0m");
                current = fg;
            }
            sb.Append(ch);
        }

        if (current.HasValue) sb.Append("\x1b[0m");
        return sb.ToString();
    }

    private static string AnsiCode(ConsoleColor c) => c switch
    {
        ConsoleColor.Black       => "\x1b[30m",
        ConsoleColor.DarkRed     => "\x1b[31m",
        ConsoleColor.DarkGreen   => "\x1b[32m",
        ConsoleColor.DarkYellow  => "\x1b[33m",
        ConsoleColor.DarkBlue    => "\x1b[34m",
        ConsoleColor.DarkMagenta => "\x1b[35m",
        ConsoleColor.DarkCyan    => "\x1b[36m",
        ConsoleColor.Gray        => "\x1b[37m",
        ConsoleColor.DarkGray    => "\x1b[90m",
        ConsoleColor.Red         => "\x1b[91m",
        ConsoleColor.Green       => "\x1b[92m",
        ConsoleColor.Yellow      => "\x1b[93m",
        ConsoleColor.Blue        => "\x1b[94m",
        ConsoleColor.Magenta     => "\x1b[95m",
        ConsoleColor.Cyan        => "\x1b[96m",
        ConsoleColor.White       => "\x1b[97m",
        _                        => "\x1b[0m",
    };
}
