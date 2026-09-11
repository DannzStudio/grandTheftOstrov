using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private Texture2D _carTexture;
    private Texture2D _pixelTexture;
    private Vector2 _carPosition;
    private float _carRotation; // Z rotace (v radiánech)

    private float _carSpeed = 500f; // Pixely za vteřinu
    private float _turnSpeed = 4f;  // Rychlost zatáčení

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        // Tímto řádkem donutíme hru běžet i na velmi slabých grafikách
        _graphics.GraphicsProfile = GraphicsProfile.Reach;

        int monitorWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        int monitorHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

        // 2. Nastavíme oknu velikost přesně podle monitoru
        _graphics.PreferredBackBufferWidth = monitorWidth;
        _graphics.PreferredBackBufferHeight = monitorHeight;

        // 3. Zapneme fullscreen
        _graphics.IsFullScreen = true;

        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _carPosition = new Vector2(_graphics.PreferredBackBufferHeight / 2, _graphics.PreferredBackBufferWidth / 2); // Startovní pozice uprostřed okna
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Vygenerujeme dočasné auto: Červený obdélník 40x80 pixelů
        _carTexture = new Texture2D(GraphicsDevice, 80, 40);
        Color[] carColor = new Color[40 * 80];
        Array.Fill(carColor, Color.Red); // Vyplníme pole červenou barvou
        _carTexture.SetData(carColor);

        // Prázdná textura pro budovy
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        var kstate = Keyboard.GetState();
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Zatáčení (A, D nebo šipky)
        if (kstate.IsKeyDown(Keys.A) || kstate.IsKeyDown(Keys.Left))
            _carRotation -= _turnSpeed * deltaTime;
        if (kstate.IsKeyDown(Keys.D) || kstate.IsKeyDown(Keys.Right))
            _carRotation += _turnSpeed * deltaTime;

        // Jízda dopředu a dozadu (W, S)
        if (kstate.IsKeyDown(Keys.W) || kstate.IsKeyDown(Keys.Up))
        {
            _carPosition.X += (float)Math.Cos(_carRotation) * _carSpeed * deltaTime;
            _carPosition.Y += (float)Math.Sin(_carRotation) * _carSpeed * deltaTime;
        }
        if (kstate.IsKeyDown(Keys.S) || kstate.IsKeyDown(Keys.Down))
        {
            _carPosition.X -= (float)Math.Cos(_carRotation) * _carSpeed * deltaTime;
            _carPosition.Y -= (float)Math.Sin(_carRotation) * _carSpeed * deltaTime;
        }
        if(kstate.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkGray);

        // 1. Zjistíme aktuální velikost okna
        float screenWidth = GraphicsDevice.Viewport.Width;
        float screenHeight = GraphicsDevice.Viewport.Height;

        // Kamera (zůstává stejná)
        Matrix cameraTransform = Matrix.CreateTranslation(-_carPosition.X, -_carPosition.Y, 0) *
                                 Matrix.CreateTranslation(screenWidth / 2f, screenHeight / 2f, 0);

        // 3. Spustíme kreslení a předáme mu naši kameru
        _spriteBatch.Begin(transformMatrix: cameraTransform);

        // Kreslení auta
        Vector2 origin = new Vector2(_carTexture.Width / 2f, _carTexture.Height / 2f);

        _spriteBatch.Draw(
            _carTexture,
            _carPosition,
            null,
            Color.White,
            _carRotation,
            origin,
            1f, // Zmenšené měřítko
            SpriteEffects.None,
            0f
        );

        // Kreslení testovací budovy 1
        _spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(200, 150, 200, 100),
            Color.Brown
        );

        // Kreslení testovací budovy 2
        _spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(600, 400, 150, 150),
            Color.DarkSlateGray
        );

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
