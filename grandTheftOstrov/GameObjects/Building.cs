using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Building : GameObject
{
    public int Width;
    public int Height;

    // Konstruktor - co se stane, když budovu vytvoříme
    public Building(Texture2D texture, Vector2 position, int width, int height, Color color)
    {
        Texture = texture;     // Zděděno z GameObject
        Position = position;   // Zděděno z GameObject
        ObjectColor = color;   // Zděděno z GameObject
        Width = width;
        Height = height;
    }

    // Přepíšeme základní kreslení (override), protože budovy
    // teď kreslíme roztáhnutím 1x1 pixelu do obdélníku (Rectangle)
    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(Texture, new Rectangle((int)Position.X, (int)Position.Y, Width, Height), ObjectColor);
    }
}

