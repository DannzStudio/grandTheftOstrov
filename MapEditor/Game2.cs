using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace OstrovMapEditor;

public class Game2 : Game
{
    private enum Tool
    {
        Road = 1,
        Roundabout = 2,
        Square = 3,
        Building = 4
    }

    private sealed class UiButton
    {
        public string Label;
        public Rectangle Bounds;
        public Action OnClick;
        public Func<bool> IsSelected;
        public Color AccentColor;
    }

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private Texture2D _pixel;
    private Texture2D _circle;

    private MapData _map = new();

    // Kamera
    private Vector2 _cameraPos = new(0, 0);
    private float _zoom = 1f;

    // Vstup
    private KeyboardState _prevKey;
    private MouseState _prevMouse;

    // UI tlačítka
    private readonly List<UiButton> _buttons = new();
    private const int TopBarHeight = 40;

    // Aktuální nástroj a rozdělané tvary
    private Tool _tool = Tool.Road;
    private readonly List<Vector2> _roadPoints = new();
    private const float SnapRadiusWorld = 24f;

    private bool _dragging = false;
    private Vector2 _dragStart;
    private float _buildingRotation = 0f;

    private readonly Color[] _colors =
    {
        new(150, 70, 70),
        new(95, 115, 135),
        new(170, 140, 85),
        new(85, 125, 95),
        new(120, 120, 125)
    };
    private int _colorIdx = 0;

    public Game2()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "Ostrov Map Editor | Klikaci UI + Snap silnic";
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();
        BuildUiButtons();
        base.Initialize();
    }

    private void BuildUiButtons()
    {
        _buttons.Clear();

        var defs = new (string label, Color col, Func<bool> selected, Action action)[]
        {
            ("Silnice [1]", Color.CornflowerBlue, () => _tool == Tool.Road, () => { CommitRoadIfReady(); _tool = Tool.Road; }),
            ("Kruhak [2]", Color.MediumPurple, () => _tool == Tool.Roundabout, () => { CommitRoadIfReady(); _tool = Tool.Roundabout; }),
            ("Namesti [3]", Color.Khaki, () => _tool == Tool.Square, () => { CommitRoadIfReady(); _tool = Tool.Square; }),
            ("Budova [4]", Color.IndianRed, () => _tool == Tool.Building, () => { CommitRoadIfReady(); _tool = Tool.Building; }),
            ("Barva [C]", Color.SandyBrown, () => false, () => _colorIdx = (_colorIdx + 1) % _colors.Length),
            ("Zpet [Z]", Color.LightGray, () => false, UndoLast),
            ("Ulozit [F5]", Color.SeaGreen, () => false, SaveMap),
            ("Nacist [F9]", Color.SteelBlue, () => false, LoadMap)
        };

        int x = 10;
        int y = 6;
        int h = TopBarHeight - 12;

        foreach (var d in defs)
        {
            int w = Math.Max(92, d.label.Length * 8 + 20);
            _buttons.Add(new UiButton
            {
                Label = d.label,
                Bounds = new Rectangle(x, y, w, h),
                AccentColor = d.col,
                IsSelected = d.selected,
                OnClick = d.action
            });
            x += w + 8;
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _circle = CreateCircleTexture(128);
    }

    protected override void UnloadContent()
    {
        _pixel?.Dispose();
        _circle?.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var key = Keyboard.GetState();
        var mouse = Mouse.GetState();
        Vector2 screenMouse = mouse.Position.ToVector2();
        Vector2 rawWorldMouse = ScreenToWorld(screenMouse);
        Vector2 snappedWorldMouse = SnapRoadPoint(rawWorldMouse);

        UpdateCamera(key, mouse, dt);
        UpdateShortcuts(key);

        bool leftClick = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
        bool overTopBar = mouse.Position.Y < TopBarHeight;

        if (leftClick && HandleUiClick(mouse.Position))
        {
            // Kliknutí zachytilo tlačítko v horní liště
        }
        else if (!overTopBar)
        {
            UpdateTool(key, mouse, rawWorldMouse, snappedWorldMouse);
        }

        _prevKey = key;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    private bool HandleUiClick(Point mousePos)
    {
        foreach (var btn in _buttons)
        {
            if (btn.Bounds.Contains(mousePos))
            {
                btn.OnClick?.Invoke();
                return true;
            }
        }
        return false;
    }

    private void UpdateCamera(KeyboardState key, MouseState mouse, float dt)
    {
        float speed = 650f / _zoom;
        Vector2 dir = Vector2.Zero;

        if (key.IsKeyDown(Keys.Up) || (_tool != Tool.Road && key.IsKeyDown(Keys.W))) dir.Y -= 1;
        if (key.IsKeyDown(Keys.Down) || (_tool != Tool.Road && key.IsKeyDown(Keys.S))) dir.Y += 1;
        if (key.IsKeyDown(Keys.Left) || (_tool != Tool.Road && key.IsKeyDown(Keys.A))) dir.X -= 1;
        if (key.IsKeyDown(Keys.Right) || (_tool != Tool.Road && key.IsKeyDown(Keys.D))) dir.X += 1;

        if (dir != Vector2.Zero)
        {
            dir.Normalize();
            _cameraPos += dir * speed * dt;
        }

        if (mouse.MiddleButton == ButtonState.Pressed && _prevMouse.MiddleButton == ButtonState.Pressed)
        {
            Vector2 delta = (mouse.Position - _prevMouse.Position).ToVector2();
            _cameraPos -= delta / _zoom;
        }

        int wheelDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
        if (wheelDelta != 0)
        {
            float oldZoom = _zoom;
            _zoom = Math.Clamp(_zoom + Math.Sign(wheelDelta) * 0.12f, 0.25f, 3f);

            Vector2 screenMouse = mouse.Position.ToVector2();
            _cameraPos += ScreenToWorld(screenMouse, oldZoom) - ScreenToWorld(screenMouse, _zoom);
        }
    }

    private void UpdateShortcuts(KeyboardState key)
    {
        if (Pressed(key, Keys.D1)) { CommitRoadIfReady(); _tool = Tool.Road; }
        if (Pressed(key, Keys.D2)) { CommitRoadIfReady(); _tool = Tool.Roundabout; }
        if (Pressed(key, Keys.D3)) { CommitRoadIfReady(); _tool = Tool.Square; }
        if (Pressed(key, Keys.D4)) { CommitRoadIfReady(); _tool = Tool.Building; }

        if (key.IsKeyDown(Keys.Q)) _buildingRotation -= 1.8f * (float)TargetElapsedTime.TotalSeconds;
        if (key.IsKeyDown(Keys.E)) _buildingRotation += 1.8f * (float)TargetElapsedTime.TotalSeconds;

        if (Pressed(key, Keys.C)) _colorIdx = (_colorIdx + 1) % _colors.Length;
        if (Pressed(key, Keys.F5)) SaveMap();
        if (Pressed(key, Keys.F9)) LoadMap();
        if (Pressed(key, Keys.Z)) UndoLast();
    }

    private void SaveMap()
    {
        CommitRoadIfReady();
        string savedPath = _map.Save();
        Window.Title = $"Ulozeno: {savedPath}";
    }

    private void LoadMap()
    {
        _roadPoints.Clear();
        _dragging = false;
        _map = MapData.Load();
        Window.Title = $"Nacteno: {MapData.GetMapPath()}";
    }

    private void UndoLast()
    {
        if (_tool == Tool.Road && _roadPoints.Count > 0)
        {
            _roadPoints.RemoveAt(_roadPoints.Count - 1);
            return;
        }

        if (_tool == Tool.Road && _map.Roads.Count > 0) _map.Roads.RemoveAt(_map.Roads.Count - 1);
        else if (_tool == Tool.Roundabout && _map.Roundabouts.Count > 0) _map.Roundabouts.RemoveAt(_map.Roundabouts.Count - 1);
        else if (_tool == Tool.Square && _map.Squares.Count > 0) _map.Squares.RemoveAt(_map.Squares.Count - 1);
        else if (_tool == Tool.Building && _map.Buildings.Count > 0) _map.Buildings.RemoveAt(_map.Buildings.Count - 1);
    }

    private void UpdateTool(KeyboardState key, MouseState mouse, Vector2 rawWorldMouse, Vector2 snappedWorldMouse)
    {
        bool leftClick = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
        bool rightClick = mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released;

        switch (_tool)
        {
            case Tool.Road:
                if (leftClick)
                {
                    // Zabráníme duplicitnímu stejnému bodu hned po sobě
                    if (_roadPoints.Count == 0 || Vector2.Distance(_roadPoints[^1], snappedWorldMouse) > 4f)
                        _roadPoints.Add(snappedWorldMouse);
                }

                if (Pressed(key, Keys.Enter) || (rightClick && _roadPoints.Count >= 2))
                {
                    CommitRoadIfReady();
                }
                else if (rightClick && _roadPoints.Count < 2)
                {
                    _roadPoints.Clear();
                }
                break;

            case Tool.Roundabout:
                if (leftClick)
                {
                    _map.Roundabouts.Add(new RoundaboutData
                    {
                        Center = rawWorldMouse,
                        Radius = 120f,
                        RoadWidth = 56f,
                        SidewalkWidth = 84f,
                        HasCenterIsland = true
                    });
                }
                break;

            case Tool.Square:
                if (leftClick)
                {
                    _dragging = true;
                    _dragStart = rawWorldMouse;
                }
                else if (mouse.LeftButton == ButtonState.Released && _dragging)
                {
                    _dragging = false;
                    var rect = MakeRect(_dragStart, rawWorldMouse);
                    if (rect.Width > 20 && rect.Height > 20)
                    {
                        _map.Squares.Add(new SquareZoneData
                        {
                            Name = $"Namesti {_map.Squares.Count + 1}",
                            Position = new Vector2(rect.X, rect.Y),
                            Width = rect.Width,
                            Height = rect.Height
                        });
                    }
                }
                break;

            case Tool.Building:
                if (leftClick)
                {
                    _dragging = true;
                    _dragStart = rawWorldMouse;
                }
                else if (mouse.LeftButton == ButtonState.Released && _dragging)
                {
                    _dragging = false;
                    var rect = MakeRect(_dragStart, rawWorldMouse);
                    if (rect.Width > 15 && rect.Height > 15)
                    {
                        Color c = _colors[_colorIdx];
                        _map.Buildings.Add(new BuildingData
                        {
                            Position = new Vector2(rect.Center.X, rect.Center.Y),
                            Width = rect.Width,
                            Height = rect.Height,
                            Rotation = _buildingRotation,
                            R = c.R,
                            G = c.G,
                            B = c.B
                        });
                    }
                }
                break;
        }
    }

    // Přichytí bod silnice k existujícímu uzlu jiné silnice nebo na obvod kruháče
    private Vector2 SnapRoadPoint(Vector2 pos)
    {
        float snapDist = SnapRadiusWorld / Math.Max(0.5f, _zoom);
        Vector2 best = pos;
        float bestDist = snapDist;

        // 1. Uzly existujících silnic
        foreach (var road in _map.Roads)
        {
            foreach (var pt in road.ControlPoints)
            {
                float d = Vector2.Distance(pos, pt);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = pt;
                }
            }
        }

        // 2. Okraj kruháče (vzdálenost od středu kruháče == rb.Radius)
        foreach (var rb in _map.Roundabouts)
        {
            Vector2 toPos = pos - rb.Center;
            float distFromCenter = toPos.Length();
            if (distFromCenter > 0.001f)
            {
                Vector2 rimPoint = rb.Center + Vector2.Normalize(toPos) * rb.Radius;
                float dRim = Vector2.Distance(pos, rimPoint);
                if (dRim < bestDist)
                {
                    bestDist = dRim;
                    best = rimPoint;
                }
            }
        }

        return best;
    }

    private void CommitRoadIfReady()
    {
        if (_roadPoints.Count >= 2)
        {
            _map.Roads.Add(new RoadData
            {
                Name = $"Ulice {_map.Roads.Count + 1}",
                RoadWidth = 64f,
                SidewalkWidth = 96f,
                Surface = "Asphalt",
                SmoothCurve = true,
                ControlPoints = new List<Vector2>(_roadPoints)
            });
        }
        _roadPoints.Clear();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(74, 120, 70));

        var mouse = Mouse.GetState();
        Vector2 rawWorldMouse = ScreenToWorld(mouse.Position.ToVector2());
        Vector2 snappedWorldMouse = SnapRoadPoint(rawWorldMouse);

        _spriteBatch.Begin(transformMatrix: GetCameraMatrix());

        DrawGrid(128);
        DrawSquares();
        DrawRoads();
        DrawRoundabouts();
        DrawRoadSnapNodes();
        DrawBuildings();
        DrawPreview(rawWorldMouse, snappedWorldMouse);

        _spriteBatch.End();

        DrawTopUiBar(mouse.Position);

        base.Draw(gameTime);
    }

    private void DrawTopUiBar(Point mousePos)
    {
        _spriteBatch.Begin();
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, TopBarHeight), new Color(24, 26, 30, 235));
        _spriteBatch.Draw(_pixel, new Rectangle(0, TopBarHeight - 2, GraphicsDevice.Viewport.Width, 2), Color.White * 0.15f);

        foreach (var btn in _buttons)
        {
            bool hover = btn.Bounds.Contains(mousePos);
            bool selected = btn.IsSelected?.Invoke() ?? false;

            Color bg = selected
                ? Color.Lerp(btn.AccentColor, Color.White, 0.25f)
                : hover
                    ? new Color(70, 75, 85)
                    : new Color(45, 48, 56);

            _spriteBatch.Draw(_pixel, btn.Bounds, bg);

            // Barevný pruh nahoře na tlačítku pro snadné rozlišení
            _spriteBatch.Draw(_pixel, new Rectangle(btn.Bounds.X, btn.Bounds.Y, btn.Bounds.Width, 4), btn.AccentColor);

            // Rámeček
            DrawRectBorder(btn.Bounds, selected ? Color.Yellow : Color.White * 0.35f, 1);

            // Text popisku (vykreslený jednoduchým rastrovým písmem bez nutnosti SpriteFont)
            DrawPixelText(btn.Label, btn.Bounds.X + 8, btn.Bounds.Y + 11, Color.White, 1);
        }

        // Ukazatel aktivní barvy budovy vpravo na liště
        int swatchX = GraphicsDevice.Viewport.Width - 34;
        Rectangle swatch = new(swatchX, 9, 22, 22);
        _spriteBatch.Draw(_pixel, swatch, _colors[_colorIdx]);
        DrawRectBorder(swatch, Color.White, 1);

        _spriteBatch.End();
    }

    private void DrawRoadSnapNodes()
    {
        if (_tool != Tool.Road) return;

        foreach (var road in _map.Roads)
        {
            foreach (var pt in road.ControlPoints)
            {
                DrawCircle(pt, 5f, Color.Yellow * 0.85f);
            }
        }
    }

    private void DrawGrid(int step)
    {
        Color col = Color.White * 0.08f;
        const int span = 3200;
        for (int x = -span; x <= span; x += step)
            DrawSegment(new Vector2(x, -span), new Vector2(x, span), col, 1f);
        for (int y = -span; y <= span; y += step)
            DrawSegment(new Vector2(-span, y), new Vector2(span, y), col, 1f);
    }

    private void DrawSquares()
    {
        foreach (var sq in _map.Squares)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle((int)sq.Position.X, (int)sq.Position.Y, (int)sq.Width, (int)sq.Height),
                new Color(180, 170, 148)
            );
        }
    }

    private void DrawRoads()
    {
        foreach (var road in _map.Roads)
        {
            List<Vector2> path = road.SmoothCurve
                ? BuildSmoothCurve(road.ControlPoints, 12)
                : road.ControlPoints;

            DrawPathStrip(path, road.SidewalkWidth, new Color(155, 155, 160));
            DrawPathStrip(path, road.RoadWidth, new Color(50, 52, 56));
            DrawPathStrip(path, 3f, new Color(225, 225, 210) * 0.65f);
        }

        if (_roadPoints.Count > 0)
        {
            List<Vector2> previewPath = BuildSmoothCurve(_roadPoints, 12);
            DrawPathStrip(previewPath, 96f, new Color(155, 155, 160) * 0.7f);
            DrawPathStrip(previewPath, 64f, new Color(50, 52, 56) * 0.85f);

            foreach (var pt in _roadPoints)
                DrawCircle(pt, 6f, Color.Yellow);
        }
    }

    private void DrawRoundabouts()
    {
        foreach (var rb in _map.Roundabouts)
        {
            DrawCircle(rb.Center, rb.Radius + (rb.SidewalkWidth - rb.RoadWidth) * 0.5f, new Color(155, 155, 160));
            DrawCircle(rb.Center, rb.Radius, new Color(50, 52, 56));

            if (rb.HasCenterIsland)
            {
                float islandR = Math.Max(24f, rb.Radius - rb.RoadWidth);
                DrawCircle(rb.Center, islandR, new Color(68, 112, 64));
            }
        }
    }

    private void DrawBuildings()
    {
        foreach (var b in _map.Buildings)
        {
            _spriteBatch.Draw(
                _pixel,
                b.Position,
                null,
                new Color(b.R, b.G, b.B),
                b.Rotation,
                new Vector2(0.5f, 0.5f),
                new Vector2(b.Width, b.Height),
                SpriteEffects.None,
                0f
            );
        }
    }

    private void DrawPreview(Vector2 rawWorldMouse, Vector2 snappedWorldMouse)
    {
        if (_tool == Tool.Road)
        {
            bool isSnapped = Vector2.Distance(rawWorldMouse, snappedWorldMouse) > 0.01f;
            if (isSnapped)
            {
                DrawCircle(snappedWorldMouse, 12f, Color.LimeGreen * 0.6f);
            }

            if (_roadPoints.Count > 0)
            {
                var temp = new List<Vector2>(_roadPoints) { snappedWorldMouse };
                var curve = BuildSmoothCurve(temp, 10);
                DrawPathStrip(curve, 64f, Color.CornflowerBlue * 0.35f);
            }
        }
        else if (_tool == Tool.Roundabout)
        {
            DrawCircle(rawWorldMouse, 120f, Color.Cyan * 0.3f);
        }
        else if (_dragging)
        {
            var rect = MakeRect(_dragStart, rawWorldMouse);
            if (_tool == Tool.Square)
            {
                _spriteBatch.Draw(_pixel, rect, Color.Khaki * 0.45f);
            }
            else if (_tool == Tool.Building)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Vector2(rect.Center.X, rect.Center.Y),
                    null,
                    _colors[_colorIdx] * 0.65f,
                    _buildingRotation,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(rect.Width, rect.Height),
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }

    private void DrawRectBorder(Rectangle r, Color color, int thickness)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
    }

    // Kompaktní 5x7 rastrový font, aby texty na tlačítkách fungovaly bez .spritefont
    private void DrawPixelText(string text, int x, int y, Color color, int scale)
    {
        int cursorX = x;
        foreach (char rawCh in text)
        {
            char ch = char.ToUpperInvariant(rawCh);
            if (ch == ' ')
            {
                cursorX += 4 * scale;
                continue;
            }

            string[] glyph = GetGlyph5x7(ch);
            for (int row = 0; row < 7; row++)
            {
                string line = glyph[row];
                for (int col = 0; col < 5; col++)
                {
                    if (line[col] == '#')
                    {
                        _spriteBatch.Draw(
                            _pixel,
                            new Rectangle(cursorX + col * scale, y + row * scale, scale, scale),
                            color
                        );
                    }
                }
            }
            cursorX += 6 * scale;
        }
    }

    private static string[] GetGlyph5x7(char ch) => ch switch
    {
        'A' => new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        'B' => new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
        'C' => new[] { ".####", "#....", "#....", "#....", "#....", "#....", ".####" },
        'D' => new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
        'E' => new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
        'F' => new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
        'H' => new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        'I' => new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
        'K' => new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
        'L' => new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
        'M' => new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#", "#...#", "#...#" },
        'N' => new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
        'O' => new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
        'P' => new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
        'R' => new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
        'S' => new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
        'T' => new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
        'U' => new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
        'V' => new[] { "#...#", "#...#", "#...#", "#...#", ".#.#.", ".#.#.", "..#.." },
        'Z' => new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
        '1' => new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
        '2' => new[] { ".###.", "#...#", "....#", "..##.", ".#...", "#....", "#####" },
        '3' => new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." },
        '4' => new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
        '5' => new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
        '9' => new[] { ".###.", "#...#", "#...#", ".####", "....#", "...#.", ".###." },
        '[' => new[] { ".###.", ".#...", ".#...", ".#...", ".#...", ".#...", ".###." },
        ']' => new[] { ".###.", "...#.", "...#.", "...#.", "...#.", "...#.", ".###." },
        _ => new[] { ".....", ".....", ".....", ".....", ".....", "..#..", "....." }
    };

    private static List<Vector2> BuildSmoothCurve(List<Vector2> pts, int subdivisions)
    {
        var result = new List<Vector2>();
        if (pts.Count < 2)
        {
            result.AddRange(pts);
            return result;
        }

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 p0 = i == 0 ? pts[i] : pts[i - 1];
            Vector2 p1 = pts[i];
            Vector2 p2 = pts[i + 1];
            Vector2 p3 = i + 2 < pts.Count ? pts[i + 2] : pts[i + 1];

            for (int step = 0; step < subdivisions; step++)
            {
                float t = step / (float)subdivisions;
                result.Add(Vector2.CatmullRom(p0, p1, p2, p3, t));
            }
        }

        result.Add(pts[^1]);
        return result;
    }

    private void DrawPathStrip(List<Vector2> pts, float thickness, Color color)
    {
        float radius = thickness * 0.5f;
        for (int i = 0; i < pts.Count - 1; i++)
        {
            DrawSegment(pts[i], pts[i + 1], color, thickness);
            DrawCircle(pts[i], radius, color);
        }
        if (pts.Count > 0)
            DrawCircle(pts[^1], radius, color);
    }

    private void DrawSegment(Vector2 a, Vector2 b, Color color, float thickness)
    {
        Vector2 diff = b - a;
        float len = diff.Length();
        if (len < 0.001f) return;

        float angle = (float)Math.Atan2(diff.Y, diff.X);
        _spriteBatch.Draw(
            _pixel,
            a,
            null,
            color,
            angle,
            new Vector2(0f, 0.5f),
            new Vector2(len, thickness),
            SpriteEffects.None,
            0f
        );
    }

    private void DrawCircle(Vector2 center, float radius, Color color)
    {
        if (radius <= 0.5f) return;
        Vector2 origin = new Vector2(_circle.Width * 0.5f, _circle.Height * 0.5f);
        float scale = (radius * 2f) / _circle.Width;
        _spriteBatch.Draw(_circle, center, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private Texture2D CreateCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(GraphicsDevice, size, size);
        Color[] data = new Color[size * size];
        float r = size * 0.5f;
        float rSq = r * r;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r;
                float dy = y + 0.5f - r;
                float dSq = dx * dx + dy * dy;

                if (dSq <= rSq)
                {
                    float aa = Math.Clamp(r - (float)Math.Sqrt(dSq), 0f, 1f);
                    data[y * size + x] = Color.White * aa;
                }
                else
                {
                    data[y * size + x] = Color.Transparent;
                }
            }
        }

        tex.SetData(data);
        return tex;
    }

    private static Rectangle MakeRect(Vector2 a, Vector2 b)
    {
        return new Rectangle(
            (int)Math.Min(a.X, b.X),
            (int)Math.Min(a.Y, b.Y),
            (int)Math.Abs(a.X - b.X),
            (int)Math.Abs(a.Y - b.Y)
        );
    }

    private Matrix GetCameraMatrix()
    {
        return Matrix.CreateTranslation(new Vector3(-_cameraPos, 0f)) *
               Matrix.CreateScale(_zoom, _zoom, 1f) *
               Matrix.CreateTranslation(new Vector3(GraphicsDevice.Viewport.Width * 0.5f, GraphicsDevice.Viewport.Height * 0.5f, 0f));
    }

    private Vector2 ScreenToWorld(Vector2 screenPos, float? zoomOverride = null)
    {
        float z = zoomOverride ?? _zoom;
        Matrix mat = Matrix.CreateTranslation(new Vector3(-_cameraPos, 0f)) *
                     Matrix.CreateScale(z, z, 1f) *
                     Matrix.CreateTranslation(new Vector3(GraphicsDevice.Viewport.Width * 0.5f, GraphicsDevice.Viewport.Height * 0.5f, 0f));
        return Vector2.Transform(screenPos, Matrix.Invert(mat));
    }

    private bool Pressed(KeyboardState now, Keys key) =>
        now.IsKeyDown(key) && !_prevKey.IsKeyDown(key);
}