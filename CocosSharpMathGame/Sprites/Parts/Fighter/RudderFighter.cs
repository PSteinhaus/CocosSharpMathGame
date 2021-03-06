﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace CocosSharpMathGame
{
    internal class RudderFighter : Part
    {
        public RudderFighter() : base("rudderFighter.png")
        {
            SetHealthAndMaxHealth(11);
            // set your types
            Types = new Type[] { Type.RUDDER };
            NormalAnchorPoint = new CCPoint(0.5f, 0f);

            // specify the mass points
            MassPoints = CreateDiamondMassPoints(4f);

            // specify the collision type
            CollisionType = Collisions.CreateDiamondCollisionPolygon(this);

            // give the rudder ManeuverAbility
            ManeuverAbility = new ManeuverAbility(0,0, (float)Math.Pow(10, 5) * 2.5f, (float)Math.Pow(10, 5) * 6f);
        }
    }
    internal class RudderFighterShiny : Part
    {
        public RudderFighterShiny() : base("rudderFighterShiny.png")
        {
            SetHealthAndMaxHealth(18);
            // set your types
            Types = new Type[] { Type.RUDDER };
            NormalAnchorPoint = new CCPoint(0.5f, 0f);

            // specify the mass points
            MassPoints = CreateDiamondMassPoints(4f);

            // specify the collision type
            CollisionType = Collisions.CreateDiamondCollisionPolygon(this);

            // give the rudder ManeuverAbility
            ManeuverAbility = new ManeuverAbility(0, 0, (float)Math.Pow(10, 5) * 1.5f, (float)Math.Pow(10, 5) * 7f);
        }
    }
}
