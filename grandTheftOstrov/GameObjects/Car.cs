using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class Car : GameObject
{
    public float Rotation = 0f;
    public float MaxSpeed = 1000f;
    public float MaxReverseSpeed = 300f;
    public float Speed = 0f;
    public Vector2 Velocity = Vector2.Zero;

    public float Acceleration = 300f;
    public float BrakeForce = 1000f;
    public float SlowDownOverTime = 300f;
    public float TurnSpeed = 3f;

    public float ReverseDelay = 1f;
    private float _currentReverseDelay = 0f;

    public Game1 game;
    public bool isControlled = false;
    public Texture2D Texture;
    public Color ObjectColor = Color.White;

    public Car(Texture2D texture, Vector2 startPosition, Game1 game)
    {
        Texture = texture;
        Position = startPosition;
        this.game = game;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var kstate = Keyboard.GetState();

        if (isControlled) Control(deltaTime, kstate);
        else EngineBrake(deltaTime, 1);

        Vector2 forward = new Vector2((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));
        Vector2 desiredVelocity = forward * Speed;

        float grip = 5f;

        if (kstate.IsKeyDown(Keys.Space) && isControlled)
        {
            grip = 1f;
            EngineBrake(deltaTime, 0.5f);
        }

        // Zjistíme fyzickou rychlost
        float actualPhysicalSpeed = Velocity.Length();

        if (Speed == 0 && actualPhysicalSpeed < 15f && !kstate.IsKeyDown(Keys.Space))
        {
            Velocity = Vector2.Zero;
        }

        Velocity = Vector2.Lerp(Velocity, desiredVelocity, grip * deltaTime);
        Position += Velocity * deltaTime;
    }

    private void Control(float deltaTime, KeyboardState kstate)
    {
        float actualPhysicalSpeed = Velocity.Length();
        float movementFactor = Math.Clamp(actualPhysicalSpeed / 50f, 0f, 1f);
        float understeerFactor = 1f - (actualPhysicalSpeed / MaxSpeed) * 0.2f;
        float currentTurnSpeed = TurnSpeed * movementFactor * understeerFactor;

        int direction = Speed >= 0 ? 1 : -1;

        if (kstate.IsKeyDown(Keys.A))
            Rotation -= currentTurnSpeed * direction * deltaTime;
        if (kstate.IsKeyDown(Keys.D))
            Rotation += currentTurnSpeed * direction * deltaTime;

        if (kstate.IsKeyDown(Keys.S))
        {
            Brake(deltaTime);
        }
        else if (kstate.IsKeyDown(Keys.W))
        {
            _currentReverseDelay = ReverseDelay;
            Accelerate(deltaTime);
        }
        else
        {
            _currentReverseDelay = ReverseDelay;
            EngineBrake(deltaTime, 1);
        }
    }

    private void Accelerate(float deltaTime)
    {
        if (Speed < 0) Speed += BrakeForce * deltaTime;
        else Speed += Acceleration * deltaTime;
        Speed = Math.Clamp(Speed, -MaxReverseSpeed, MaxSpeed);
    }

    private void Brake(float deltaTime)
    {
        if (Speed > 0)
        {
            Speed -= BrakeForce * deltaTime;
            if (Speed < 0) Speed = 0;
        }
        else if (Speed == 0)
        {
            _currentReverseDelay -= deltaTime;
            if (_currentReverseDelay <= 0) Speed -= Acceleration * deltaTime;
        }
        else
        {
            Speed -= Acceleration * deltaTime;
        }
        Speed = Math.Clamp(Speed, -MaxReverseSpeed, MaxSpeed);
    }

    private void EngineBrake(float deltaTime, float mult)
    {
        if (Speed > 0)
        {
            Speed -= SlowDownOverTime * deltaTime * mult;
            if (Speed < 0) Speed = 0;
        }
        else if (Speed < 0)
        {
            Speed += SlowDownOverTime * deltaTime * mult;
            if (Speed > 0) Speed = 0;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Texture != null)
        {
            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            spriteBatch.Draw(Texture, Position, null, ObjectColor, Rotation - MathF.PI / 2, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}