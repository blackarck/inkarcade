namespace TerminalBlaster;

using System.Text;

// ── Sprite ───────────────────────────────────────────────────────────────────

public class Sprite
{
    public int     Id      { get; }
    public string[] Art    { get; set; }
    public int     X       { get; internal set; }
    public int     Y       { get; internal set; }
    public int     Width   => Art.Length > 0 ? Art[0].Length : 0;
    public int     Height  => Art.Length;
    public string  Tag     { get; set; }
    public bool    Visible { get; set; } = true;

    internal Sprite(int id, string[] art, int x, int y, string tag)
        => (Id, Art, X, Y, Tag) = (id, art, x, y, tag);
}

// ── Engine ───────────────────────────────────────────────────────────────────

public class TerminalEngine
{
    public const int MinWidth  = 100;
    public const int MinHeight = 30;

    public int Width  { get; }
    public int Height { get; }

    private readonly Dictionary<int, Sprite> _sprites = new();
    private int _nextId;
    private readonly StringBuilder _buffer;
    private string _lastFrame = string.Empty;
    private readonly List<(int x, int y, string text)> _overlays = new();
    private bool _running;

    // ── Init ─────────────────────────────────────────────────────────────────

    public TerminalEngine(int? width = null, int? height = null)
    {
        int w = width  ?? Console.WindowWidth;
        int h = height ?? Console.WindowHeight;

        if (w < MinWidth || h < MinHeight)
            throw new InvalidOperationException(
                $"Terminal is {w}×{h} — minimum required is {MinWidth}×{MinHeight}.\n" +
                $"Resize your terminal window and try again.");

        Width  = w;
        Height = h;
        _buffer = new StringBuilder(Width * Height);

        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible  = false;
        if (OperatingSystem.IsWindows())
            Console.SetBufferSize(Width, Height);
    }

    // ── Sprite lifecycle ──────────────────────────────────────────────────────

    public Sprite CreateSprite(string[] art, int x, int y, string tag = "")
    {
        var s = new Sprite(_nextId++, art, x, y, tag);
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

    public void DrawText(int x, int y, string text)
        => _overlays.Add((x, y, text));

    // ── Game loop ─────────────────────────────────────────────────────────────

    public void Run(Action update, int targetFps = 60)
    {
        int msPerFrame = 1000 / targetFps;
        _running = true;
        while (_running)
        {
            update();
            Render();
            Thread.Sleep(msPerFrame);
        }
    }

    public void Stop() => _running = false;

    // ── Internal renderer ─────────────────────────────────────────────────────

    private void Render()
    {
        _buffer.Clear();
        _buffer.Append(' ', Width * Height);

        foreach (var sprite in _sprites.Values)
            if (sprite.Visible)
                BlitLines(sprite.X, sprite.Y, sprite.Art);

        foreach (var (ox, oy, text) in _overlays)
            BlitLine(ox, oy, text);
        _overlays.Clear();

        string frame = _buffer.ToString();
        if (frame == _lastFrame) return;
        Console.SetCursorPosition(0, 0);
        Console.Write(frame);
        _lastFrame = frame;
    }

    private void BlitLines(int x, int y, string[] lines)
    {
        for (int row = 0; row < lines.Length; row++)
            BlitLine(x, y + row, lines[row]);
    }

    private void BlitLine(int x, int y, string text)
    {
        if (y < 0 || y >= Height || x >= Width) return;
        int col0   = Math.Max(0, x);
        int colEnd = Math.Min(x + text.Length, Width);
        int len    = colEnd - col0;
        if (len <= 0) return;
        int pos    = y * Width + col0;
        int offset = col0 - x;             // >0 when sprite clips left edge
        for (int i = 0; i < len; i++)
            _buffer[pos + i] = text[offset + i];
    }
}
