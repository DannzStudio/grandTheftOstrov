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

    private List<GameObject> _gameObjects;
    private Car _player;

    protected override void Initialize()
    {
        _gameObjects = new List<GameObject>();

        base.Initialize();
    }

    Texture2D _pixelTexture;

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        Texture2D tempCarTex = new Texture2D(GraphicsDevice, 40, 80);
        Color[] carColor = new Color[40 * 80];
        Array.Fill(carColor, Color.Red);
        tempCarTex.SetData(carColor);

        // Vytvoříme auto a přidáme ho k ostatním objektům
        _player = new Car(tempCarTex, new Vector2(400, 300));
        _gameObjects.Add(_player);

        // Prázdná textura pro budovy
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _gameObjects.Add(new Building(_pixelTexture, new Vector2(200, 150), 200, 100, Color.Brown));
        _gameObjects.Add(new Building(_pixelTexture, new Vector2(600, 400), 150, 150, Color.DarkSlateGray));
        _gameObjects.Add(new Building(_pixelTexture, new Vector2(-100, -200), 300, 120, Color.Gray));
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

        // Kamera (zůstává stejná)
        Matrix cameraTransform = Matrix.CreateTranslation(-_player.Position.X, -_player.Position.Y, 0) *
                         Matrix.CreateTranslation(screenWidth / 2f, screenHeight / 2f, 0);

        // 3. Spustíme kreslení a předáme mu naši kameru
        _spriteBatch.Begin(transformMatrix: cameraTransform);

        foreach (var obj in _gameObjects)
        {
            obj.Draw(_spriteBatch);
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
