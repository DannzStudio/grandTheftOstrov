using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


public class GameObject
{
    public Vector2 Position;
    public Texture2D Texture;
    public Color ObjectColor = Color.White;

    // Klíčové slovo 'virtual' znamená, že specifické objekty (auto, budova) 
    // si mohou tuto funkci později upravit podle sebe.
    public virtual void Update(GameTime gameTime)
    {
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (Texture != null)
        {
            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            spriteBatch.Draw(Texture, Position, null, ObjectColor, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}

