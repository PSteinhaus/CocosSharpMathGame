﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace CocosSharpMathGame
{
    internal class BodyPotato : Part
    {
        public BodyPotato() : base("bodyPotato.png")
        {
            SetHealthAndMaxHealth(10);
            NormalAnchorPoint = CCPoint.AnchorMiddle;
            // set your types
            Types = new Type[] { Type.BODY };

            // add mount points for two single wings (and a rivaling double wing mount)
            var wingMount1 = new PartMount(this, new CCPoint((ContentSize.Width * 0.75f), (ContentSize.Height / 2) + 5), Type.SINGLE_WING);
            var wingMount2 = new PartMount(this, new CCPoint((ContentSize.Width * 0.75f), (ContentSize.Height / 2) - 5), Type.SINGLE_WING);
            var doubleWingMount = new PartMount(this, new CCPoint((ContentSize.Width * 0.72f), ContentSize.Height / 2), Type.WINGS);
            doubleWingMount.PossiblyBlockingPartMounts.Add(wingMount1);
            doubleWingMount.PossiblyBlockingPartMounts.Add(wingMount2);
            wingMount1.PossiblyBlockingPartMounts.Add(doubleWingMount);
            wingMount2.PossiblyBlockingPartMounts.Add(doubleWingMount);
            // add mount points for two rudders
            var rudderMount1 = new PartMount(this, new CCPoint(2f, (ContentSize.Height / 2) + 1), Type.RUDDER);
            var rudderMount2 = new PartMount(this, new CCPoint(2f, (ContentSize.Height / 2) - 1), Type.RUDDER);
            // add a mount for a rotor or weapon
            var rotorMount = new PartMount(this, new CCPoint(ContentSize.Width - 3, ContentSize.Height / 2), Type.ROTOR, Type.GUN);
            rotorMount.MaxTurningAngle = 30f;

            PartMounts = new PartMount[] { wingMount1, wingMount2, doubleWingMount, rudderMount1, rudderMount2, rotorMount };

            // specify the collision polygon
            CollisionType = Collisions.CreateDiamondCollisionPolygon(this);

            // specify the mass points
            MassPoints = CreateDiamondMassPoints(40);
        }
    }

    internal class BodyPotatoShiny : Part
    {
        public BodyPotatoShiny() : base("bodyPotatoShiny.png")
        {
            SetHealthAndMaxHealth(18);
            NormalAnchorPoint = CCPoint.AnchorMiddle;
            // set your types
            Types = new Type[] { Type.BODY };

            // add mount points for two single wings (and a rivaling double wing mount)
            var wingMount1 = new PartMount(this, new CCPoint((ContentSize.Width * 0.75f), (ContentSize.Height / 2) + 5), Type.SINGLE_WING);
            var wingMount2 = new PartMount(this, new CCPoint((ContentSize.Width * 0.75f), (ContentSize.Height / 2) - 5), Type.SINGLE_WING);
            var doubleWingMount = new PartMount(this, new CCPoint((ContentSize.Width * 0.65f), ContentSize.Height / 2), Type.WINGS);
            doubleWingMount.Dz = 2;
            doubleWingMount.PossiblyBlockingPartMounts.Add(wingMount1);
            doubleWingMount.PossiblyBlockingPartMounts.Add(wingMount2);
            wingMount1.PossiblyBlockingPartMounts.Add(doubleWingMount);
            wingMount2.PossiblyBlockingPartMounts.Add(doubleWingMount);
            // add mount points for two rudders
            var rudderMount1 = new PartMount(this, new CCPoint(2f, (ContentSize.Height / 2) + 1), Type.RUDDER);
            var rudderMount2 = new PartMount(this, new CCPoint(2f, (ContentSize.Height / 2) - 1), Type.RUDDER);
            // add a mount for a rotor or weapon
            var rotorMount = new PartMount(this, new CCPoint(ContentSize.Width - 3, ContentSize.Height / 2), Type.ROTOR, Type.GUN);
            rotorMount.MaxTurningAngle = 40f;

            PartMounts = new PartMount[] { wingMount1, wingMount2, doubleWingMount, rudderMount1, rudderMount2, rotorMount };

            // specify the collision polygon
            CollisionType = Collisions.CreateDiamondCollisionPolygon(this);

            // specify the mass points
            MassPoints = CreateDiamondMassPoints(35);
        }
    }
}
