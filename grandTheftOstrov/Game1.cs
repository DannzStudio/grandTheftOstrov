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
    private float _turnSpeed = 8f;  // Rychlost zatáčení

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _carPosition = new Vector2(400, 300); // Startovní pozice uprostřed okna
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _carTexture = Texture2D.FromFile(GraphicsDevice, "car.png");

        // Vytvoření prázdné textury 1x1 pixel
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

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkGray);

        // 1. Zjistíme aktuální velikost okna
        float screenWidth = _graphics.PreferredBackBufferWidth;
        float screenHeight = _graphics.PreferredBackBufferHeight;

        // 2. Vytvoříme transformační matici kamery
        // Nejprve posuneme svět do mínusu o pozici auta a pak ho posuneme do poloviny obrazovky
        Matrix cameraTransform = Matrix.CreateTranslation(-_carPosition.X, -_carPosition.Y, 0) *
                                 Matrix.CreateTranslation(screenWidth / 2f, screenHeight / 2f, 0);

        // 3. Spustíme kreslení a předáme mu naši kameru
        _spriteBatch.Begin(transformMatrix: cameraTransform);

        // Kreslení auta (zůstává úplně stejné)
        Vector2 origin = new Vector2(_carTexture.Width / 2f, _carTexture.Height / 2f);

        _spriteBatch.Draw(
            _carTexture,
            _carPosition,
            null,
            Color.White,
            _carRotation,
            origin,
            0.02f, // Tvoje zmenšené měřítko
            SpriteEffects.None,
            0f
        );

        _spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(200, 150, 200, 100), // Budova 200x100 pixelů
            Color.Brown // Barva budovy
        );

        // Vykreslení další budovy pro lepší orientaci
        _spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(600, 400, 150, 150),
            Color.DarkSlateGray
        );

        // Sem bys pak dal ten foreach na vykreslování NPC a domů...
        // foreach (var npc in npcs) { npc.Draw(_spriteBatch); }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}