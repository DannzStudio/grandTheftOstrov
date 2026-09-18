using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class Car : GameObject
{
    public float Rotation = 0f;
    public float Speed = 300f;
    public float TurnSpeed = 8f;

    public Car(Texture2D texture, Vector2 startPosition)
    {
        Texture = texture;
        Position = startPosition;
    }

    // Třída si sama řeší vstupy a pohyb
    public override void Update(GameTime gameTime)
    {
        var kstate = Keyboard.GetState();
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (kstate.IsKeyDown(Keys.A) || kstate.IsKeyDown(Keys.Left))
            Rotation -= TurnSpeed * deltaTime;
        if (kstate.IsKeyDown(Keys.D) || kstate.IsKeyDown(Keys.Right))
            Rotation += TurnSpeed * deltaTime;

        if (kstate.IsKeyDown(Keys.W) || kstate.IsKeyDown(Keys.Up))
        {
            Position.X += (float)Math.Cos(Rotation) * Speed * deltaTime;
            Position.Y += (float)Math.Sin(Rotation) * Speed * deltaTime;
        }
        if (kstate.IsKeyDown(Keys.S) || kstate.IsKeyDown(Keys.Down))
        {
            Position.X -= (float)Math.Cos(Rotation) * Speed * deltaTime;
            Position.Y -= (float)Math.Sin(Rotation) * Speed * deltaTime;
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