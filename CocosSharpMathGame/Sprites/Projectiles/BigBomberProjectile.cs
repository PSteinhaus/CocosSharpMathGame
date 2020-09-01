﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;
using CSharpMath;

namespace CocosSharpMathGame
{
    internal class BigBomberProjectile : Projectile
    {
        internal BigBomberProjectile()
        {
            Velocity = 3000f;
            LifeTime = 0.70f;
            Damage = 3.5f;
        }
    }
}
