using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


public class GameObject
{
    public Vector2 Position;
    

    // Klíčové slovo 'virtual' znamená, že specifické objekty (auto, budova) 
    // si mohou tuto funkci později upravit podle sebe.
    public virtual void Update(GameTime gameTime)
    {
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {

    }
}

