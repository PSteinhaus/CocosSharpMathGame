﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace CocosSharpMathGame
{
    internal class TestDoubleWing : Part
    {
        internal TestDoubleWing() : base("testDoubleWing.png")
        {
            // set your types
            Types = new Type[] { Type.WINGS };
            NormalAnchorPoint = CCPoint.AnchorMiddle;

            // add mounts points for guns
            var gunMount1 = new PartMount(this, new CCPoint(ContentSize.Width / 2, (ContentSize.Height - (ContentSize.Height / 4))), Type.GUN);
            var gunMount2 = new PartMount(this, new CCPoint(ContentSize.Width / 2,                       (ContentSize.Height / 4)), Type.GUN);
            gunMount1.Dz = -1;
            gunMount2.Dz = -1;
            PartMounts = new PartMount[] { gunMount1, gunMount2 };

            // specify the collision polygon
            CollisionType = Collisions.CreateDiamondCollisionPolygon(this);

            // specify the mass points
            MassPoints = CreateDiamondMassPoints(30);
        }
    }
}