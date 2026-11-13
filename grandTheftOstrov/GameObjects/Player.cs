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
    public Rectangle colRect;
    public Rectangle useRect;
    int useOffset = 20;
    Vector2 offsetSize;

    private KeyboardState _previousKeyState;

    public Player(Texture2D texture, Vector2 startPosition, Game1 game)
    {
        Texture = texture;
        Position = startPosition;
        this.game = game;

        colRect = new Rectangle((int)(startPosition.X - texture.Width / 2f), (int)(startPosition.Y - texture.Height / 2f), texture.Width, texture.Height);

        offsetSize = new Vector2(texture.Width + useOffset * 2, texture.Height + useOffset * 2);
        useRect = new Rectangle((int)(startPosition.X - offsetSize.X / 2f), (int)(startPosition.Y - offsetSize.Y / 2f), (int)offsetSize.X, (int)offsetSize.Y);

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

        if (kstate.IsKeyDown(Keys.F) && !_previousKeyState.IsKeyDown(Keys.F))
        {
            if (!isInCar)
            {
                foreach (GameObject obj in game._gameObjects)
                {
                    if (obj is Car car)
                    {
                        if (this.useRect.Intersects(car.rect))
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
                Position.X += (float)Math.Cos(ridingObj.Rotation - MathF.PI / 2) * 37;
                Position.Y += (float)Math.Sin(ridingObj.Rotation - MathF.PI / 2) * 37;
                ridingObj = null;
                isInCar = false;
                
            }
        }

        if (isInCar)
        {
            this.Position = ridingObj.Position;
        }

        colRect.X = (int)(Position.X - (Texture.Width / 2f));
        colRect.Y = (int)(Position.Y - (Texture.Height / 2f));

        useRect.X = (int)(Position.X - (offsetSize.X / 2f));
        useRect.Y = (int)(Position.Y - (offsetSize.Y / 2f));

        _previousKeyState = kstate;
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