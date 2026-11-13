using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Building : GameObject
{
    public int Width;
    public int Height;

    public Texture2D Texture;
    public Color ObjectColor = Color.White;

    public Rectangle rect;

    // Konstruktor - co se stane, když budovu vytvoříme
    public Building(Texture2D texture, Vector2 position, int width, int height, Color color)
    {
        Texture = texture; 
        Position = position;   
        ObjectColor = color;   
        Width = width;
        Height = height;

        rect = new Rectangle((int)position.X, (int)position.Y, width, height);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(Texture, new Rectangle((int)Position.X, (int)Position.Y, Width, Height), ObjectColor);
    }
}

