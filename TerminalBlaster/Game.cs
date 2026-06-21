namespace TerminalBlaster;

using System.Threading;

public class Game
{
    // ── Public state (read by Program.cs after loop exits) ────────────────────
    public int  Score  { get; private set; }
    public bool IsOver { get; private set; }

    // ── Config ────────────────────────────────────────────────────────────────
    private const int GunWidth      = 7;
    private const int PillarCount   = 4;
    private const int EnemyCols     = 4;
    private const int EnemyRows     = 2;
    private const int EnemySpacingX = 12;
    private const int EnemySpacingY = 7;
    private const int EnemyHitsToKill = 10;

    // ── Engine ────────────────────────────────────────────────────────────────
    private readonly TerminalEngine _engine;

    // ── Sprites ───────────────────────────────────────────────────────────────
    private readonly Sprite _player;

    // ── Enemy metadata (engine owns sprites; game owns HP) ────────────────────
    private readonly Dictionary<int, int> _enemyHits = new();

    // ── Wave / lives ──────────────────────────────────────────────────────────
    private int  _wave  = 1;
    private int  _lives = 3;

    // ── Input state ───────────────────────────────────────────────────────────
    private bool _spaceCooldown;

    // ── Enemy movement ────────────────────────────────────────────────────────
    private bool _movingRight = true;
    private int  _enemyMoveTick;
    private int  _enemyShootTick;
    private int  _bulletMoveTick;
    private int  _enemyBulletMoveDelay = 3;

    private static readonly Random Rng = new();

    // ── Art assets ────────────────────────────────────────────────────────────

    private static readonly string[] PlayerArt =
    {
        "  ▄█▄  ",
        "█▀▀█▀▀█",
        "  │║│  "
    };

    private static readonly string[] LifeArt =
    {
        "  ▲  ",
        "◄███►",
        "  ▼  "
    };

    private static readonly string[][] EnemyVariants =
    {
        new[] { "  ▄▄▄  ", " █████ ", "███████", " █████ ", "  ▀▀▀  " },
        new[] { " ╔═══╗ ", " ║◄►║ ", "███████", " ║[ ]║ ", " ╚███╝ " },
        new[] { " ▀█▀█▀ ", "██▄█▄██", "███████", " ╔═══╗ ", " ║▀▀▀║ " },
        new[] { "  ♦♦♦  ", " ▄███▄ ", "███████", " ▀███▀ ", "  ▀▀▀  " },
        new[] { " ┌───┐ ", " │╳╳╳│ ", "███████", " │▣▣▣│ ", " └───┘ " },
    };

    private static readonly string[][] PillarVariants =
    {
        new[] { "  ██  ", "  ██  ", "  ██  ", "  ██  ", "██████" },
        new[] { " ████ ", " ████ ", " ████ ", " ████ ", "██████" },
        new[] { "  ▲▲  ", " ████ ", "  ██  ", " ████ ", "██████" },
        new[] { "  ♦   ", " ████ ", "  ██  ", " ████ ", "══════" },
        new[] { "  ╔╗  ", "  ║║  ", "  ║║  ", " ═║║═ ", "██████" },
    };

    // ── Constructor ───────────────────────────────────────────────────────────

    public Game(TerminalEngine engine)
    {
        _engine = engine;
        _player = engine.CreateSprite(PlayerArt,
            (engine.Width - GunWidth) / 2, engine.Height - 4, "player");
        PlacePillars();
        SpawnWave();
    }

    // ── Update — called once per frame by the engine loop ─────────────────────

    public void Update()
    {
        if (_lives <= 0)
        {
            IsOver = true;
            _engine.Stop();
            return;
        }

        HandleInput();
        MoveBullets();
        MoveEnemies();
        EnemyShoot();
        CheckNextWave();
        DrawHUD();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        var key = _engine.PollInput();

        if (key == null) { _spaceCooldown = false; return; }

        switch (key)
        {
            case ConsoleKey.LeftArrow when _player.X > 0:
                _engine.MoveSprite(_player, -1, 0);
                break;

            case ConsoleKey.RightArrow when _player.X < _engine.Width - GunWidth:
                _engine.MoveSprite(_player, 1, 0);
                break;

            case ConsoleKey.Spacebar when !_spaceCooldown:
                _engine.CreateSprite(new[] { "|" },
                    _player.X + GunWidth / 2, _player.Y - 1, "bullet");
                _spaceCooldown = true;
                break;
        }
    }

    // ── Bullets ───────────────────────────────────────────────────────────────

    private void MoveBullets()
    {
        // Player bullets move up one cell per frame
        foreach (var bullet in _engine.GetSpritesByTag("bullet"))
        {
            _engine.MoveSprite(bullet, 0, -1);

            if (bullet.Y < 0) { _engine.DestroySprite(bullet); continue; }

            var hit = _engine.GetCollisions(bullet, "enemy").FirstOrDefault();
            if (hit == null) continue;

            _engine.DestroySprite(bullet);
            _enemyHits.TryGetValue(hit.Id, out int h);
            _enemyHits[hit.Id] = ++h;
            if (h >= EnemyHitsToKill)
            {
                _engine.DestroySprite(hit);
                _enemyHits.Remove(hit.Id);
                Score += 100;
            }
        }

        // Enemy bullets move down, throttled by delay counter
        if (++_bulletMoveTick < _enemyBulletMoveDelay) return;
        _bulletMoveTick = 0;

        foreach (var bullet in _engine.GetSpritesByTag("enemyBullet"))
        {
            _engine.MoveSprite(bullet, 0, 1);

            if (bullet.Y >= _engine.Height) { _engine.DestroySprite(bullet); continue; }

            if (_engine.Overlaps(bullet, _player))
            {
                _engine.DestroySprite(bullet);
                TakeHit();
                break; // TakeHit clears all enemy bullets; stop iterating the snapshot
            }
        }
    }

    // ── Enemy movement ────────────────────────────────────────────────────────

    private void MoveEnemies()
    {
        if (++_enemyMoveTick < 30) return;
        _enemyMoveTick = 0;

        var enemies = _engine.GetSpritesByTag("enemy");
        int dx = _movingRight ? 1 : -1;

        bool hitWall = false;
        foreach (var e in enemies)
            if ((_movingRight && e.X >= _engine.Width - 15) ||
                (!_movingRight && e.X <= 10))
                { hitWall = true; break; }

        if (hitWall) { _movingRight = !_movingRight; return; }

        foreach (var e in enemies) _engine.MoveSprite(e, dx, 0);
    }

    private void EnemyShoot()
    {
        if (++_enemyShootTick < 30) return;
        _enemyShootTick = 0;

        var enemies = _engine.GetSpritesByTag("enemy");
        if (enemies.Count == 0) return;

        var shooter = enemies[Rng.Next(enemies.Count)];
        _engine.CreateSprite(new[] { "█" },
            shooter.X + 3, shooter.Y + shooter.Height, "enemyBullet");
    }

    // ── Wave progression ──────────────────────────────────────────────────────

    private void CheckNextWave()
    {
        if (_engine.GetSpritesByTag("enemy").Count > 0) return;

        _wave++;
        if (_enemyBulletMoveDelay > 1) _enemyBulletMoveDelay--;

        foreach (var b in _engine.GetSpritesByTag("bullet"))      _engine.DestroySprite(b);
        foreach (var b in _engine.GetSpritesByTag("enemyBullet")) _engine.DestroySprite(b);

        SpawnWave();
    }

    // ── Damage ────────────────────────────────────────────────────────────────

    private void TakeHit()
    {
        _lives--;
        _engine.SetPosition(_player, (_engine.Width - GunWidth) / 2, _engine.Height - 4);
        foreach (var b in _engine.GetSpritesByTag("enemyBullet")) _engine.DestroySprite(b);
        Thread.Sleep(500); // brief grace period
    }

    // ── HUD (drawn each frame as text overlays) ───────────────────────────────

    private void DrawHUD()
    {
        _engine.DrawText(0, 0, $"Score: {Score}   Wave: {_wave}");

        for (int i = 0; i < _lives; i++)
            for (int j = 0; j < LifeArt.Length; j++)
                _engine.DrawText(_engine.Width - (_lives - i) * 7, j, LifeArt[j]);
    }

    // ── Spawn helpers ─────────────────────────────────────────────────────────

    private void SpawnWave()
    {
        _enemyHits.Clear();
        for (int row = 0; row < EnemyRows; row++)
            for (int col = 0; col < EnemyCols; col++)
                _engine.CreateSprite(
                    EnemyVariants[Rng.Next(EnemyVariants.Length)],
                    15 + col * EnemySpacingX,
                    3  + row * EnemySpacingY,
                    "enemy");
    }

    private void PlacePillars()
    {
        var art = PillarVariants[Rng.Next(PillarVariants.Length)];
        for (int i = 0; i < PillarCount; i++)
        {
            int sw = _engine.Width / PillarCount;
            int px = i * sw + (sw - art[0].Length) / 2;
            int py = _engine.Height - 8 - art.Length + 1;
            _engine.CreateSprite(art, px, py, "pillar");
        }
    }
}
