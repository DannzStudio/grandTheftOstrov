using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private Texture2D _carTexture;
    private Vector2 _carPosition;
    private float _carRotation; // Z rotace (v radiánech)

    private float _carSpeed = 200f; // Pixely za vteřinu
    private float _turnSpeed = 3f;  // Rychlost zatáčení

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

        // Jednoduché načtení PNG přímo ze složky
        _carTexture = Texture2D.FromFile(GraphicsDevice, "car.png");
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
        GraphicsDevice.Clear(Color.DarkGray); // Silnice je lepší šedá než modrá

        _spriteBatch.Begin();

        // Nastavíme střed rotace přesně do poloviny obrázku auta
        Vector2 origin = new Vector2(_carTexture.Width / 2f, _carTexture.Height / 2f);

        // Samotné vykreslení s aplikovanou Z rotací
        _spriteBatch.Draw(
            _carTexture,
            _carPosition,
            null,
            Color.White,
            _carRotation,
            origin,
            0.01f,
            SpriteEffects.None,
            0f
        );

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}