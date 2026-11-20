using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class Player : GameObject
{
    public float Speed = 300f;
    public bool isInCar = false;
    Car ridingObj;

    public Texture2D Texture;
    public Color ObjectColor = Color.White;

    private Game1 game;

    public Collider UseHitbox = new Collider();
    private int useOffset = 20;
    private Vector2 offsetSize;

    private KeyboardState _previousKeyState;

    public Player(Texture2D texture, Vector2 startPosition, Game1 game)
    {
        Texture = texture;
        Position = startPosition;
        this.game = game;

        offsetSize = new Vector2(Texture.Width + useOffset * 2, Texture.Height + useOffset * 2);

        Hitbox.Update(Position, Texture.Width, Texture.Height, 0f);
        UseHitbox.Update(Position, (int)offsetSize.X, (int)offsetSize.Y, 0f);
    }

    public override void Update(GameTime gameTime)
    {
        var kstate = Keyboard.GetState();
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (kstate.IsKeyDown(Keys.A) && !isInCar)
            Position.X -= Speed * deltaTime;
        if (kstate.IsKeyDown(Keys.D) && !isInCar)
            Position.X += Speed * deltaTime;
        if (kstate.IsKeyDown(Keys.W) && !isInCar)
            Position.Y -= Speed * deltaTime;
        if (kstate.IsKeyDown(Keys.S) && !isInCar)
            Position.Y += Speed * deltaTime;

        Hitbox.Update(Position, Texture.Width, Texture.Height, 0f);
        UseHitbox.Update(Position, (int)offsetSize.X, (int)offsetSize.Y, 0f);

        if (kstate.IsKeyDown(Keys.F) && !_previousKeyState.IsKeyDown(Keys.F))
        {
            if (!isInCar)
            {
                foreach (GameObject obj in game._gameObjects)
                {
                    if (obj is Car car)
                    {
                        if (this.UseHitbox.Intersects(car.Hitbox, out _, out _))
                        {
                            car.isControlled = true;
                            this.isInCar = true;
                            ridingObj = car;
                            break;
                        }
                    }
                }
            }
            else
            {
                ridingObj.isControlled = false;

                Vector2 oldPos = Position;

                Position.X += (float)Math.Cos(ridingObj.Rotation - MathF.PI / 2) * 37;
                Position.Y += (float)Math.Sin(ridingObj.Rotation - MathF.PI / 2) * 37;

                Hitbox.Update(Position, Texture.Width, Texture.Height, 0f);
                bool steppingInHitbox = false;
                foreach (GameObject obj in game._gameObjects)
                {
                    if (obj is not Player player)
                    {
                        if (Hitbox.Intersects(obj.Hitbox, out Vector2 normal, out float depth))
                        {
                            steppingInHitbox = true;
                        }
                    }
                }
                if (steppingInHitbox)
                {
                    Position = oldPos;
                    Hitbox.Update(Position, Texture.Width, Texture.Height, 0f);
                    Position.X += (float)Math.Cos(ridingObj.Rotation + MathF.PI / 2) * 37;
                    Position.Y += (float)Math.Sin(ridingObj.Rotation + MathF.PI / 2) * 37;
                }

                ridingObj = null;
                isInCar = false;
            }
        }

        if (isInCar)
        {
            this.Position = ridingObj.Position;
        }

        _previousKeyState = kstate;

        if(!isInCar) ResolveCollisions();
    }

    private void ResolveCollisions()
    {
        foreach (GameObject obj in game._gameObjects)
        {
            if (obj is not Player player)
            {
                if (Hitbox.Intersects(obj.Hitbox, out Vector2 normal, out float depth))
                {
                    Position += normal * (depth + 0.05f);

                    Hitbox.Update(Position, Texture.Width, Texture.Height, 0f);
                    UseHitbox.Update(Position, (int)offsetSize.X, (int)offsetSize.Y, 0f);
                }
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Texture != null && !isInCar)
        {
            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            spriteBatch.Draw(Texture, Position, null, ObjectColor, 0, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}