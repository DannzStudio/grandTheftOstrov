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


    public float BounceFactor = 0.5f;     
    public float WallSlideDamping = 0.92f;

    public float ReverseDelay = 0.2f;
    private float _currentReverseDelay = 0f;

    public Game1 game;
    public bool isControlled = false;
    public Texture2D Texture;
    public Color ObjectColor = Color.White;

    public float SpeedNormalized => Math.Clamp(Velocity.Length() / MaxSpeed, 0f, 1f);

    public Car(Texture2D texture, Vector2 startPosition, Game1 game)
    {
        Texture = texture;
        Position = startPosition;
        this.game = game;

        Hitbox.Update(Position, Texture.Width, Texture.Height, Rotation - MathF.PI / 2);
    }

    private struct SkidMark
    {
        public Vector2 Position;
        public float Rotation;
        public float Alpha;
    }

    private readonly List<SkidMark> _skidMarks = new List<SkidMark>();

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var kstate = Keyboard.GetState();

        float oldRotation = Rotation;

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

        float actualPhysicalSpeed = Velocity.Length();
        if (Speed == 0 && actualPhysicalSpeed < 15f && !kstate.IsKeyDown(Keys.Space))
        {
            Velocity = Vector2.Zero;
        }

        Velocity = Vector2.Lerp(Velocity, desiredVelocity, grip * deltaTime);
        Position += Velocity * deltaTime;

        Vector2 right = new Vector2(-forward.Y, forward.X);
        float lateralSpeed = Math.Abs(Vector2.Dot(Velocity, right));

        bool isDrifting = (Keyboard.GetState().IsKeyDown(Keys.Space) && isControlled && Velocity.Length() > 10f)
                          || lateralSpeed > 500f;

        SkidMarks(forward, isDrifting, deltaTime);

        ResolveBuildingCollisions(forward, oldRotation);
    }

    private void ResolveBuildingCollisions(Vector2 forward, float oldRotation)
    {
        float visualRotation = Rotation - MathF.PI / 2;
        Hitbox.Update(Position, Texture.Width, Texture.Height, visualRotation);

        bool anyCollision = false;

        for (int pass = 0; pass < 2; pass++)
        {
            bool collidedThisPass = false;

            foreach (GameObject obj in game._gameObjects)
            {
                if (obj is Building building)
                {
                    if (Hitbox.Intersects(building.Hitbox, out Vector2 normal, out float depth))
                    {
                        anyCollision = true;
                        collidedThisPass = true;

                        Position += normal * (depth + 0.05f);
                        Hitbox.Update(Position, Texture.Width, Texture.Height, visualRotation);

                        float intoWallSpeed = Vector2.Dot(Velocity, normal);

                        if (intoWallSpeed < 0f)
                        {
                            Vector2 normalComponent = normal * intoWallSpeed;
                            Velocity -= normalComponent * (1f + BounceFactor);

                            Velocity *= WallSlideDamping;
                        }
                    }
                }
            }

            if (!collidedThisPass)
                break;
        }

        if (anyCollision)
        {
            Hitbox.Update(Position, Texture.Width, Texture.Height, Rotation - MathF.PI / 2);
            foreach (GameObject obj in game._gameObjects)
            {
                if (obj is Building building && Hitbox.Intersects(building.Hitbox, out Vector2 n, out float d))
                {
                    Rotation = oldRotation;
                    Position += n * (d + 0.05f);
                    Hitbox.Update(Position, Texture.Width, Texture.Height, Rotation - MathF.PI / 2);
                    break;
                }
            }

            float projectedForward = Vector2.Dot(Velocity, forward);
            if (Speed > 0f && projectedForward < 0f) Speed = 0f;
            if (Speed < 0f && projectedForward > 0f) Speed = 0f;
            if (Velocity.Length() < 8f) Velocity = Vector2.Zero;
        }
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

    private void SkidMarks(Vector2 forward, bool isDrifting, float deltaTime)
    {
        if (isDrifting)
        {
            Vector2 rearAxle = Position - forward * (Texture.Height * 0.35f);
            float visualRot = Rotation - MathF.PI / 2f;

            _skidMarks.Add(new SkidMark
            {
                Position = rearAxle,
                Rotation = visualRot,
                Alpha = 0.45f
            });

            if (_skidMarks.Count > 600)
                _skidMarks.RemoveAt(0);
        }

        for (int i = _skidMarks.Count - 1; i >= 0; i--)
        {
            var mark = _skidMarks[i];
            mark.Alpha -= 0.15f * deltaTime;
            if (mark.Alpha <= 0f)
                _skidMarks.RemoveAt(i);
            else
                _skidMarks[i] = mark;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Texture != null)
        {
            Vector2 markOrigin = new Vector2(Texture.Width * 0.4f, 3f);
            foreach (var mark in _skidMarks)
            {
                spriteBatch.Draw(
                    Texture,
                    mark.Position,
                    null,
                    Color.Black * mark.Alpha,
                    mark.Rotation,
                    markOrigin,
                    new Vector2(1f, 0.3f),
                    SpriteEffects.None,
                    0f
                );
            }

            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            spriteBatch.Draw(Texture, Position, null, ObjectColor, Rotation - MathF.PI / 2, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}