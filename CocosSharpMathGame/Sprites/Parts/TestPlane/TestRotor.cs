﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace CocosSharpMathGame
{
    internal class TestRotor : Part
    {
        public TestRotor() : base("testRotor.png")
        {
            SetHealthAndMaxHealth(8);
            // set your types
            Types = new Type[] { Type.ROTOR };
            NormalAnchorPoint = new CCPoint(3 / ContentSize.Width, 0.5f);

            // specify the mass points
            MassPoints = CreateDiamondMassPoints(10f);

            // specify the collision polygon
            CollisionType = Collisions.CreateDiamondCollisionPolygon(this);

            // give the rotor ManeuverAbility
            ManeuverAbility = new ManeuverAbility((float)Math.Pow(10, 5) * 1.25f, (float)Math.Pow(10, 5) * 13.0f);
        }
    }

    internal class TestRotorShiny : Part
    {
        public TestRotorShiny() : base("rotorTestShiny.png")
        {
            SetHealthAndMaxHealth(11);
            // set your types
            Types = new Type[] { Type.ROTOR };
            NormalAnchorPoint = new CCPoint(3 / ContentSize.Width, 0.5f);

            // specify the mass points
            MassPoints = CreateDiamondMassPoints(11f);

            // specify the collision polygon
            CollisionType = Collisions.CreateDiamondCollisionPolygon(this);

            // give the rotor ManeuverAbility
            ManeuverAbility = new ManeuverAbility((float)Math.Pow(10, 5) * 7.25f, (float)Math.Pow(10, 5) * 17.0f);
        }
    }
}
