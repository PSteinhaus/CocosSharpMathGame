﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace CocosSharpMathGame
{
    internal class TestEngine : Part
    {
        public TestEngine() : base("testEngine.png")
        {
            // set your types
            Types = new Type[] { Type.ENGINE };
            NormalAnchorPoint = CCPoint.AnchorMiddle;

            // specify the mass points
            MassPoints = new MassPoint[] { new MassPoint(ContentSize.Width / 2, ContentSize.Height / 2, 30) };

            // specify the collision polygon
            CollisionType = Collisions.CreateDiamondCollisionPolygon(this);

            // give the engine maneuver abilities
            ManeuverAbility = new ManeuverAbility((float)Math.Pow(10, 5) * 0.25f, (float)Math.Pow(10, 5) * 3.0f);//, (float)Math.Pow(10, 5)*0.1f, (float)Math.Pow(10, 5));
        }


    }
}
