using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.GraphicsProfile = GraphicsProfile.Reach;

        _graphics.IsFullScreen = false;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;

        // Úplné vypnutí limitů a VSyncu
        _graphics.SynchronizeWithVerticalRetrace = false;
        IsFixedTimeStep = false;

        IsMouseVisible = true;
    }

    public List<GameObject> _gameObjects;
    private Player _player;
    private Car _car;

    protected override void Initialize()
    {
        _gameObjects = new List<GameObject>();

        base.Initialize();
    }

    Texture2D _pixelTexture;

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        Texture2D carTexture = new Texture2D(GraphicsDevice, 32, 64);
        Color[] carColors = new Color[32 * 64];
        Array.Fill(carColors, Color.Red); // Nastavení všech pixelů na červenou barvu
        carTexture.SetData(carColors);

        Texture2D playerTexture = new Texture2D(GraphicsDevice, 32, 32);
        Color[] playerColors = new Color[32 * 32];
        Array.Fill(playerColors, Color.Black); // Nastavení všech pixelů na červenou barvu
        playerTexture.SetData(playerColors);

        // Vytvoříme auto a přidáme ho k ostatním objektům
        _player = new Player(playerTexture, new Vector2(400, 300), this);
        

        _car = new Car(carTexture, new Vector2(500, 300));
        _gameObjects.Add(_car);

        // Prázdná textura pro budovy
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _gameObjects.Add(new Building(_pixelTexture, new Vector2(200, 150), 200, 100, Color.Brown));
        _gameObjects.Add(new Building(_pixelTexture, new Vector2(600, 400), 150, 150, Color.DarkSlateGray));
        _gameObjects.Add(new Building(_pixelTexture, new Vector2(-100, -200), 300, 120, Color.Gray));

        _gameObjects.Add(_player);
    }

    protected override void Update(GameTime gameTime)
    {
        foreach (var obj in _gameObjects)
        {
            obj.Update(gameTime);
        }
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkGray);

        float screenWidth = GraphicsDevice.Viewport.Width;
        float screenHeight = GraphicsDevice.Viewport.Height;

        // Kamera
        Matrix cameraTransform = Matrix.CreateTranslation(-_player.Position.X, -_player.Position.Y, 0) *
                         Matrix.CreateTranslation(screenWidth / 2f, screenHeight / 2f, 0);

        // Drawing
        _spriteBatch.Begin(transformMatrix: cameraTransform);

        foreach (var obj in _gameObjects)
        {
            obj.Draw(_spriteBatch);
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
