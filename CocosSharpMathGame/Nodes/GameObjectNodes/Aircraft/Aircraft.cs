﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace CocosSharpMathGame
{
    /// <summary>
    /// Aircrafts are objects in the sky that are assembled from parts
    /// which react to collision.
    /// </summary>
    internal partial class Aircraft : GameObjectNode, ICollidible, IDrawNodeUser, IStreamSaveable
    {
        internal float Health
        {
            get
            {
                float health = 0;
                foreach (var part in TotalParts)
                    health += part.Health;
                return health;
            }
        }
        internal float MaxHealth
        {
            get
            {
                float maxHealth = 0;
                foreach (var part in TotalParts)
                    maxHealth += part.MaxHealth;
                return maxHealth;
            }
        }
        /// <summary>
        /// when the aircraft dies it starts changing its vertexZ continously;
        /// because of that it cannot use the global draw nodes and has to utilise new private draw nodes;
        /// </summary>
        protected CCDrawNode HighNodeWhenDead { get; set; }
        protected CCDrawNode LowNodeWhenDead { get; set; }
        internal float Velocity
        {
            get
            {
                return VelocityVector.Length;
            }
        }
        internal enum State
        {
            ACTIVE, SHOT_DOWN
        }
        internal State MyState = State.ACTIVE;
        public CollisionType CollisionType { get; set; } = new CollisionTypeBoundingBox();
        private int[] PowerUps = new int[Enum.GetNames(typeof(PowerUp.PowerType)).Length];
        internal int GetPowerUpCount(PowerUp.PowerType pType)
        {
            return PowerUps[(byte)pType];
        }
        internal void ChangePowerUpCount(PowerUp.PowerType pType, int diff)
        {
            PowerUps[(byte)pType] += diff;
        }
        internal List<PowerUp.PowerType> AvailablePowerUps()
        {
            var avPowUps = new List<PowerUp.PowerType>();
            for (int i = 0; i < PowerUps.Length; i++)
                if (PowerUps[i] != 0)
                    avPowUps.Add((PowerUp.PowerType)i);
            // at last always add the normal move option
            if (!avPowUps.Contains(PowerUp.PowerType.NORMAL))
                avPowUps.Add(PowerUp.PowerType.NORMAL);
            return avPowUps;
        }
        internal PowerUp.PowerType SelectedPower { get { return FlightPathControlNode.SelectedPower; } }
        internal Dictionary<PowerUp.PowerType, int> WeightedPowerUpsAwarded { get; private set; }
        /// <summary>
        /// the minimal velocity that any aircraft needs to have to not fall out of the sky
        /// </summary>
        internal const float V_MIN = 40f;
        /// <summary>
        /// Calculate and return the maximum (straight) velocity that this aircraft can reach, based upon its ManeuverPolygon
        /// </summary>
        internal float MaxVelocity
        {
            get
            {
                GetManeuverParts(out IEnumerable<Part> leftSideParts, out IEnumerable<Part> rightSideParts);
                float EkinRight = 0;
                float EkinLeft = 0;
                float ErotRight = 0;
                float ErotLeft = 0;
                float ErotBonusRight = 0;
                float ErotBonusLeft = 0;
                foreach (var part in rightSideParts)
                {
                    GetEkinAndErotOfPart(part, out float EkinMin, out float EkinMax, out float ErotMin, out float ErotMax, out float ErotBonusMin, out float ErotBonusMax);
                    EkinRight += EkinMax;
                    ErotRight += ErotMax;
                    ErotBonusRight += ErotBonusMax;
                }
                foreach (var part in leftSideParts)
                {
                    GetEkinAndErotOfPart(part, out float EkinMin, out float EkinMax, out float ErotMin, out float ErotMax, out float ErotBonusMin, out float ErotBonusMax);
                    EkinLeft += EkinMax;
                    ErotLeft += ErotMax;
                    ErotBonusLeft += ErotBonusMax;
                }

                float Clamp(float value, float min, float max)
                {
                    return (value < min) ? min : (value > max) ? max : value;
                }
                float CalcEkin()
                {
                    float Ekin = EkinRight + EkinLeft + (ErotRight + ErotLeft - (float)Math.Abs(ErotRight - ErotLeft)) +
                        ((ErotRight - ErotLeft > 0) ? Clamp(ErotBonusLeft - ErotBonusRight, 0, (float)Math.Abs(ErotRight - ErotLeft)) : Clamp(ErotBonusRight - ErotBonusLeft, 0, (float)Math.Abs(ErotRight - ErotLeft)));
                    return Ekin;
                }
                float v = (float)Math.Sqrt((2 * CalcEkin()) / Mass) * Constants.STANDARD_SCALE;
                return v;
            }
        }
        protected FlightPathControlNode FlightPathControlNode { get; set; }
        internal Team Team { get; set; }
        internal void PrepareForExecuteOrders()
        {
            switch (SelectedPower)
            {
                case PowerUp.PowerType.HEAL:
                    Heal();
                    break;
                default:
                    break;
            }
            if (SelectedPower != PowerUp.PowerType.NORMAL)
                ChangePowerUpCount(SelectedPower, -1);
        }
        internal void PowerChanged(PowerUp.PowerType oldPower)
        {
            UpdateManeuverPolygon();
            if (oldPower == PowerUp.PowerType.BACK_TURN || oldPower == PowerUp.PowerType.BOOST)
                FlightPathControlNode.ResetHeadPosition();
            //shieldDrawNode.Clear();
            switch (SelectedPower)
            {
                case PowerUp.PowerType.BOOST:
                    {
                        // increase the size of the maneuver polygon
                        const float BOOST_SCALE = 2f;
                        ManeuverPolygon.Scale(BOOST_SCALE);
                        FlightPathControlNode.ResetHeadPosition();
                    }
                    break;
                case PowerUp.PowerType.BACK_TURN:
                    {
                        // change the maneuver polygon to a special back-turn one 
                        ChangeManeuverPolygonToBackTurn();
                        FlightPathControlNode.ResetHeadPosition();
                    }
                    break;
                case PowerUp.PowerType.SHIELD:
                    {
                        // draw the shield using a workaround for line width
                        //shieldDrawNode.DrawSolidCircle(CCPoint.Zero, ContentSize.Width + 5f, CCColor4B.White);
                        //shieldDrawNode.DrawSolidCircle(CCPoint.Zero, ContentSize.Width + 4f, CCColor4B.Black);
                    }
                    break;
            }
        }

        private void ChangeManeuverPolygonToBackTurn()
        {
            const float BACK_DISTANCE = 140f;
            ManeuverPolygon = new PolygonWithSplines(new CCPoint[] { new CCPoint(-40, BACK_DISTANCE), new CCPoint(-41f, BACK_DISTANCE), new CCPoint(-41, BACK_DISTANCE+1), new CCPoint(-40, BACK_DISTANCE+1) });
            ManeuverPolygon.RotateBy(MyRotation);
            ManeuverPolygon.MoveBy(PositionX, PositionY);
        }

        internal void Heal()
        {
            foreach (var part in TotalParts)
                part.Repair(part.MaxHealth);
            PartsChanged();
        }
        private protected List<Tuple<int,MathChallenge>> WeightedChallenges { get; set; }
        internal MathChallenge GetChallenge()
        {
            if (WeightedChallenges != null && WeightedChallenges.Any())
            {
                // get the weight sum
                int sum = 0;
                foreach (var tuple in WeightedChallenges)
                {
                    sum += tuple.Item1;
                }
                // choose
                int choice = new Random().Next(sum);
                sum = 0;
                foreach (var tuple in WeightedChallenges)
                {
                    if (choice <= sum)
                        return tuple.Item2.CreateFromSelf();
                    sum += tuple.Item1;
                }
            }
            else
            {
                // generate a random unlocked challenge
                var challenges = MathChallenge.GetAllChallengeModels();
                while (true)
                {
                    var challenge = challenges[(new Random()).Next(challenges.Length)];
                    if (!challenge.Locked)
                        return challenge.CreateFromSelf();
                }
            }
            return null;
        }
        private AI ai;
        internal AI AI {
            get { return ai; }
            set
            {
                ai = value;
                if (ai != null)
                    ai.Aircraft = this;
            }
        }
        internal CCPoint FlightPathHeadPos
        {
            get
            {
                return FlightPathControlNode.FlightPathHeadPos;
            }
        }
        private bool controlledByPlayer = false;
        internal bool ControlledByPlayer { 
            get { return controlledByPlayer; }
            set
            {
                controlledByPlayer = value;
                if (!controlledByPlayer && FlightPathControlNode!=null)
                    FlightPathControlNode.Visible = false;
            }
        }
        /// <summary>
        /// DEBUG
        /// This drawnode draws the manveuver polygon (if IsManeuverPolygonDrawn == true)
        /// </summary>
        ///
        /*
        private CCDrawNode maneuverPolygonDrawNode = new CCDrawNode();
        internal bool IsManeuverPolygonDrawn {
            get
            {
                return maneuverPolygonDrawNode.Visible;
            }
            set
            {
                maneuverPolygonDrawNode.Visible = value;
            }
        }
        */

        /// <summary>
        /// defines where the aircraft can move to this turn
        /// </summary>
        internal PolygonWithSplines ManeuverPolygon { get; private set; }
        internal PolygonWithSplines ManeuverPolygonUntransformed { get; private protected set; } 
        /// <summary>
        /// Apply all transformations applying to the aircraft on the ManeuverPolygonUntransformed to calculate the new ManeuverPolygon.
        /// </summary>
        internal void UpdateManeuverPolygon()
        {
            ManeuverPolygon = (PolygonWithSplines)ManeuverPolygonUntransformed.Clone();
            ManeuverPolygon.RotateBy(MyRotation);
            ManeuverPolygon.MoveBy(PositionX, PositionY);
        }
        /// <summary>
        /// Aircrafts are physical objects. They have mass.
        /// </summary>
        internal float Mass { get; private protected set; }
        /// <summary>
        /// The mass of an aircraft is distributed in space. Therefore a center of mass can be calculated.
        /// </summary>
        internal CCPoint CenterOfMass { get; private protected set; }
        /// <summary>
        /// Aircrafts can be rotated by force. How much force is necessary for a certain rotation is defined by the moment of intertia.
        /// </summary>
        internal float MomentOfInertia { get; private protected set; }
        private bool inWorkshopConfiguration = false;
        internal bool InWorkshopConfiguration
        {
            get { return inWorkshopConfiguration; }
            set
            {
                if (value != inWorkshopConfiguration)
                {
                    if (value == true)
                        Body?.EnterWorkshopConfiguration();
                    else
                        Body?.EnterHangarConfiguration();
                    inWorkshopConfiguration = value;
                }
            }
        }
        internal CCSize CalcContentSize(out float xMin, out float yMin)
        {
            if (!TotalParts.Any()) { xMin = 0; yMin = 0; return CCSize.Zero; }
            // go through the bounding boxes of all parts and get the new total dimensions
            xMin = float.PositiveInfinity; yMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity; float yMax = float.NegativeInfinity;
            foreach (var part in TotalParts)
            {
                CCRect box = part.BoundingBoxTransformedToParent;
                if (box.MinX < xMin) xMin = box.MinX;
                if (box.MinY < yMin) yMin = box.MinY;
                if (box.MaxX > xMax) xMax = box.MaxX;
                if (box.MaxY > yMax) yMax = box.MaxY;
            }
            return new CCSize(xMax - xMin, yMax - yMin);
        }
        private void UpdateContentSize(bool movePartsAccordingly=true)
        {
            ContentSize = CalcContentSize(out float xMin, out float yMin);
            if (movePartsAccordingly)
                // move all parts to account for the change
                foreach (var part in TotalParts)
                {
                    part.PositionX -= xMin;
                    part.PositionY -= yMin;
                }
        }

        /// <summary>
        /// Gives access to all parts of this aircraft that have a ManeuverAbility
        /// </summary>
        /// <param name="leftSide">parts higher than the center of mass</param>
        /// <param name="rightSide">parts lower than the center of mass</param>
        internal void GetManeuverParts(out IEnumerable<Part> leftSide, out IEnumerable<Part> rightSide)
        {
            leftSide = new List<Part>();
            rightSide = new List<Part>();
            var centerOfMass = CenterOfMass;
            foreach (Part part in TotalParts)
                if (part.MyState != Part.State.DESTROYED && part.ManeuverAbility != null)
                {
                    CCPoint LeftLowerCornerOfPart = part.PosLeftLower;
                    // check whether left or right side
                    // Y is used because unrotated all things face east and so "left" means "higher than the center of mass".
                    if (part.PositionY > Body.Position.Y/*centerOfMass.Y*/)
                        ((List<Part>)leftSide).Add(part);
                    else
                        ((List<Part>)rightSide).Add(part);
                }
            // sort each list, so that the parts that have the highest ratio of Erot to Ekin come first
            // in other words sort by ratio of Ekin to Erot
            int CompareFunction(Part part1, Part part2)
            {
                // calculate the rotational and kinetic energy for each part
                GetEkinAndErotOfPart(part1, out float EkinMin1, out float EkinMax1, out float ErotMin1, out float ErotMax1, out float ErotBonusMin1, out float ErotBonusMax1);
                GetEkinAndErotOfPart(part2, out float EkinMin2, out float EkinMax2, out float ErotMin2, out float ErotMax2, out float ErotBonusMin2, out float ErotBonusMax2);
                float ratio1 = EkinMax1 / (ErotMax1 + ErotBonusMax1);
                float ratio2 = EkinMax2 / (ErotMax2 + ErotBonusMax2);
                return ratio1.CompareTo(ratio2);
            }
            ((List<Part>)leftSide).Sort(CompareFunction);
            ((List<Part>)rightSide).Sort(CompareFunction);
        }

        internal void GetEkinAndErotOfPart(Part part, out float EkinMin, out float EkinMax, out float ErotMin, out float ErotMax, out float ErotBonusMin, out float ErotBonusMax)
        {
            EkinMin = 0; EkinMax = 0; ErotMin = 0; ErotMax = 0; ErotBonusMin = 0; ErotBonusMax = 0;
            if (part.ManeuverAbility == null) return;
            // get the angle relative to the center of mass
            float angle = Constants.DxDyToRadians(CenterOfMass.X - part.PositionX, CenterOfMass.Y - part.PositionY);
            EkinMin = part.ManeuverAbility.PowerMin * (float)Math.Abs(Math.Cos(angle));
            EkinMax = part.ManeuverAbility.PowerMax * (float)Math.Abs(Math.Cos(angle));
            ErotMin = part.ManeuverAbility.PowerMin - EkinMin;
            ErotMax = part.ManeuverAbility.PowerMax - EkinMax;
            ErotBonusMin = part.ManeuverAbility.RotationBonusMin;
            ErotBonusMax = part.ManeuverAbility.RotationBonusMax;
        }
        internal void RestrictChildrenToTotalParts()
        {
            RemoveAllChildren();
            // remove all children that are not contained in the current parts
            foreach (var part in TotalParts)
                AddChild(part, part.CalcZOrder());
        }
        /// <summary>
        /// You lost/gained a part, some parts moved, or something else happened that entails the need to recalculate part-emergent-data.
        /// </summary>
        internal void PartsChanged(bool deathPossible = false)
        {
            TotalParts = Body == null ? new List<Part>() : Body.TotalParts;
            var oldContentSize = ScaledContentSize;
            // recalculate all part-emergent data
            // update the ContentSize and move all parts to fit into it
            UpdateContentSize();
            Mass = 0;
            foreach (var part in TotalParts)
                Mass += part.MassSingle;
            //Console.WriteLine("Mass: " + Mass);

            CenterOfMass = Body != null ? Body.CenterOfMass : CCPoint.Zero;
            //Console.WriteLine("Height: " + ContentSize.Height);
            //Console.WriteLine("Width: " + ContentSize.Width);
            //Console.WriteLine("Center of mass: " + CenterOfMass);

            MomentOfInertia = Body != null ? Body.MomentOfInertia : 0;

            // change the AnchorPoint to the center of mass
            var oldAnchor = AnchorPoint;
            ResetAnchorPoint();
            // account for the movement of the anchor
            CCSize contentSize = ScaledContentSize;
            if (oldContentSize.Width > ScaledContentSize.Width || oldContentSize.Height > ScaledContentSize.Height)
                contentSize = oldContentSize;
            float dx = (AnchorPoint.X - oldAnchor.X) * contentSize.Width;
            float dy = (AnchorPoint.Y - oldAnchor.Y) * contentSize.Height;
            PositionX += dx;
            PositionY += dy;

            // recalculate the maneuver polygon
            CalculateManeuverPolygon(deathPossible);
        }

        internal void ResetAnchorPoint()
        {
            NormalAnchorPoint = (ContentSize.Width != 0 && ContentSize.Height != 0) ? new CCPoint(CenterOfMass.X / ContentSize.Width, CenterOfMass.Y / ContentSize.Height) : CCPoint.AnchorMiddle;
        }

        protected Part body = null;
        /// <summary>
        /// the head of the part-hierarchy
        /// </summary>
        internal Part Body
        {
            get
            {
                return body;
            }
            set
            {
                // first remove the old body (and everything connected to it) if there is one
                if (body != null)
                {
                    RemoveAllChildren();
                    // reset any possibly awarded power-ups
                    ResetPowerUps();
                }
                body = value;
                if (value != null)
                {
                    foreach (var part in body.TotalParts)
                        AddChild(part, part.CalcZOrder());
                    foreach (var mount in body.PartMounts)
                        mount.UpdateMountedPartPosition();
                    // award some power-ups depending on the body type
                    switch (body)
                    {
                        case BodyBalloon b:
                            ChangePowerUpCount(PowerUp.PowerType.HEAL, 2);
                            ChangePowerUpCount(PowerUp.PowerType.BOOST, 3);
                            break;
                        case BodyBat b:
                            ChangePowerUpCount(PowerUp.PowerType.BACK_TURN, 2);
                            break;
                        case BodyPotato b:
                            ChangePowerUpCount(PowerUp.PowerType.BOOST, 2);
                            break;
                        case TestBody b:
                            ChangePowerUpCount(PowerUp.PowerType.SHIELD, 1);
                            ChangePowerUpCount(PowerUp.PowerType.BOOST, 1);
                            break;
                        case BodyBigBomber b:
                            ChangePowerUpCount(PowerUp.PowerType.HEAL, 1);
                            ChangePowerUpCount(PowerUp.PowerType.SHIELD, 2);
                            ChangePowerUpCount(PowerUp.PowerType.BOOST, 1);
                            break;
                        case BodyFighter b:
                            ChangePowerUpCount(PowerUp.PowerType.SHIELD, 2);
                            ChangePowerUpCount(PowerUp.PowerType.BOOST, 2);
                            break;
                        case BodyScrap b:
                            ChangePowerUpCount(PowerUp.PowerType.SHIELD, 1);
                            ChangePowerUpCount(PowerUp.PowerType.BACK_TURN, 1);
                            break;
                        default:
                            break;
                    }
                }
                PartsChanged();
            }
        }

        internal void ResetPowerUps()
        {
            for (int i = 0; i < PowerUps.Length; i++)
                PowerUps[i] = 0;
        }

        internal void RotateBy(float degree)
        {
            MyRotation += degree;
        }

        internal void MoveBy(float dx, float dy)
        {
            PositionX += dx;
            PositionY += dy;
        }

        internal void MoveTo(CCPoint destination)
        {
            Position = destination;
            // use the latter instead if actually necessary for some purpose
            /*
            float dx = destination.X - PositionX;
            float dy = destination.Y - PositionY;
            MoveBy(dx, dy);
            */
        }

        internal void UpdateManeuverPolygonToThis(PolygonWithSplines untransformedPolygon)
        {
            ManeuverPolygonUntransformed = untransformedPolygon;
            UpdateManeuverPolygon();
            // draw it (DEBUG)
            /*
            if (IsManeuverPolygonDrawn)
            {
                maneuverPolygonDrawNode.Clear();
                maneuverPolygonDrawNode.DrawPolygon(untransformedPolygon.Points, untransformedPolygon.Points.Length, CCColor4B.Transparent ,2f, CCColor4B.White);
            }
            */
        }

        /// <summary>
        /// Calculate a maneuver polygon based on the part-emergent data (power, mass, etc.)
        /// and then set your ManeuverPolygon to this new polygon.
        /// </summary>
        internal void CalculateManeuverPolygon(bool deathPossible)
        {
            //Console.WriteLine("Calculation of maneuver polygon started.");
            const float PARTITIONS = 10; // defines how detailed each part is powered up (higher is more detailed)
            // first get all the relevant parts
            GetManeuverParts(out IEnumerable<Part> leftSideParts, out IEnumerable<Part> rightSideParts);
            // calculate how much kinetic energy is necessary to keep the aircraft in the sky
            //float mass = Mass;
            //Console.WriteLine("Mass: " + mass);
            //float EkinNeeded = 0.5f * mass * V_MIN * V_MIN;
            //Console.WriteLine("EkinNeeded: " + EkinNeeded);
            // first calculate the values for Erot and Ekin when all parts are turned down as much as possible
            float EkinTurnedDownRight = 0;
            float EkinTurnedDownLeft = 0;
            float ErotTurnedDownRight = 0;
            float ErotTurnedDownLeft = 0;
            float ErotBonusTurnedDownRight = 0;
            float ErotBonusTurnedDownLeft = 0;
            float ErotBonusMaxRight = 0;
            float ErotBonusMaxLeft = 0;
            foreach (var part in rightSideParts)
            {
                GetEkinAndErotOfPart(part, out float EkinMin, out float EkinMax, out float ErotMin, out float ErotMax, out float ErotBonusMin, out float ErotBonusMax);
                EkinTurnedDownRight += EkinMin;
                ErotTurnedDownRight += ErotMin;
                ErotBonusTurnedDownRight += ErotBonusMin;
                ErotBonusMaxRight += ErotBonusMax;
            }
            foreach (var part in leftSideParts)
            {
                GetEkinAndErotOfPart(part, out float EkinMin, out float EkinMax, out float ErotMin, out float ErotMax, out float ErotBonusMin, out float ErotBonusMax);
                EkinTurnedDownLeft += EkinMin;
                ErotTurnedDownLeft += ErotMin;
                ErotBonusTurnedDownLeft += ErotBonusMin;
                ErotBonusMaxLeft += ErotBonusMax;
            }
            float[] rightSideCoefficients = new float[rightSideParts.Count()];
            float rightSideBonusCoefficient = 0;
            float[] leftSideCoefficients  = new float[leftSideParts.Count()];
            float leftSideBonusCoefficient = 0;
            float EkinRight = EkinTurnedDownRight;
            float EkinLeft = EkinTurnedDownLeft;
            float ErotRight = ErotTurnedDownRight;
            float ErotLeft = ErotTurnedDownLeft;
            float ErotBonusRight = ErotBonusTurnedDownRight;
            float ErotBonusLeft = ErotBonusTurnedDownLeft;
            List<CCPoint> controlPoints = new List<CCPoint>();
            void CalcBaseValues()
            {
                ResetValues();
                for (int j = 0; j < rightSideParts.Count(); j++)
                {
                    var part = rightSideParts.ElementAt(j);
                    float coefficient = rightSideCoefficients[j];
                    GetEkinAndErotOfPart(part, out float EkinMin, out float EkinMax, out float ErotMin, out float ErotMax, out float ErotBonusMin, out float ErotBonusMax);
                    EkinRight += (EkinMax - EkinMin) * coefficient;
                    ErotRight += (ErotMax - ErotMin) * coefficient;
                }
                ErotBonusRight += (ErotBonusMaxRight - ErotBonusTurnedDownRight) * rightSideBonusCoefficient;
                for (int j = 0; j < leftSideParts.Count(); j++)
                {
                    var part = leftSideParts.ElementAt(j);
                    float coefficient = leftSideCoefficients[j];
                    GetEkinAndErotOfPart(part, out float EkinMin, out float EkinMax, out float ErotMin, out float ErotMax, out float ErotBonusMin, out float ErotBonusMax);
                    EkinLeft += (EkinMax - EkinMin) * coefficient;
                    ErotLeft += (ErotMax - ErotMin) * coefficient;
                }
                ErotBonusLeft += (ErotBonusMaxLeft - ErotBonusTurnedDownLeft) * leftSideBonusCoefficient;
            }
            void ResetValues()
            {
                EkinRight = EkinTurnedDownRight;
                EkinLeft = EkinTurnedDownLeft;
                ErotRight = ErotTurnedDownRight;
                ErotLeft = ErotTurnedDownLeft;
                ErotBonusRight = ErotBonusTurnedDownRight;
                ErotBonusLeft = ErotBonusTurnedDownLeft;
            }
            float Clamp(float value, float min, float max)
            {
                return (value < min) ? min : (value > max) ? max : value;
            }
            float CalcEkin()
            {
                float Ekin = EkinRight + EkinLeft + (ErotRight + ErotLeft - (float)Math.Abs(ErotRight - ErotLeft)) +
                    ((ErotRight - ErotLeft > 0) ? Clamp(ErotBonusLeft - ErotBonusRight, 0, (float)Math.Abs(ErotRight - ErotLeft)) : Clamp(ErotBonusRight - ErotBonusLeft, 0, (float)Math.Abs(ErotRight - ErotLeft)));
                return Ekin;
            }
            float CalcErot()
            {
                return ErotRight + ErotBonusRight - ErotLeft - ErotBonusLeft;
            }
            /*
            bool RightSideIsTurnedDown()
            {
                for (int i = 0; i < rightSideCoefficients.Length; i++)
                    if (rightSideCoefficients[i] > 0.001) return false;
                return true;
            }
            bool RightSideIsTurnedUp()
            {
                for (int i = 0; i < rightSideCoefficients.Length; i++)
                    if (rightSideCoefficients[i] < 0.999) return false;
                return true;
            }
            bool LeftSideIsTurnedDown()
            {
                for (int i = 0; i < leftSideCoefficients.Length; i++)
                    if (leftSideCoefficients[i] > 0.001) return false;
                return true;
            }
            bool LeftSideIsTurnedUp()
            {
                for (int i = 0; i < leftSideCoefficients.Length; i++)
                    if (leftSideCoefficients[i] < 0.999) return false;
                return true;
            }
            */
            // Try to turn up the jth parts coefficient and return whether it was an increase or not
            bool TurnUpRightSide(int j, float coefficient)
            {
                if (rightSideCoefficients[j] < coefficient)
                {
                    rightSideCoefficients[j] = coefficient;
                    return true;
                }
                else
                    return false;
            }
            bool TurnUpLeftSide(int j, float coefficient)
            {
                if (leftSideCoefficients[j] < coefficient)
                {
                    leftSideCoefficients[j] = coefficient;
                    return true;
                }
                else
                    return false;
            }
            bool TurnDownRightSide(int j, float coefficient)
            {
                if (rightSideCoefficients[j] > coefficient)
                {
                    rightSideCoefficients[j] = coefficient;
                    return true;
                }
                else
                    return false;
            }
            bool TurnDownLeftSide(int j, float coefficient)
            {
                if (leftSideCoefficients[j] > coefficient)
                {
                    leftSideCoefficients[j] = coefficient;
                    return true;
                }
                else
                    return false;
            }
            bool CalcValuesAndTryToAddPoint()
            {
                CalcBaseValues();
                float Ekin = CalcEkin();
                //if (Ekin >= EkinNeeded)
                float Erot = CalcErot();
                var newPoint = EnergyToDestination(Ekin, Erot);
                if (!controlPoints.Any() || !newPoint.Equals(controlPoints.Last()))
                    controlPoints.Add(newPoint);
                return true;
            }
            bool IncreaseUntilEkinIsMet()
            {
                // DEBUG: for now there is no EkinNeeded, so just return true
                return true;
                /*
                float Ekin = CalcEkin();
                while (Ekin < EkinNeeded && !(LeftSideIsTurnedUp() && RightSideIsTurnedUp()))
                {
                    // turn up what you can to reach EkinNeeded
                    // turn up both sides equally; start with ekin-engines first (reverse order)
                    for (int j=rightSideCoefficients.Length-1; j>=0; j--)
                    {
                        for (float i = 1; i <= PARTITIONS * 2; i++)
                        {
                            bool turnedUp = TurnUpRightSide(j, i / (PARTITIONS * 2));
                            if (turnedUp)
                            {
                                if (CalcValuesAndTryToAddPoint())
                                    return true;
                                else
                                    goto leftSide;
                            }
                        }
                    }
                    leftSide:
                    for (int j = leftSideCoefficients.Length - 1; j >= 0; j--)
                    {
                        for (float i = 1; i <= PARTITIONS * 2; i++)
                        {
                            bool turnedUp = TurnUpLeftSide(j, i / (PARTITIONS * 2));
                            if (turnedUp)
                            {
                                if (CalcValuesAndTryToAddPoint())
                                    return true;
                                else
                                    goto end;
                            }
                        }
                    }
                end:;
                }
                return false;
                */
            }
            // 1. to find the first point turn up the right side 
            // turn the right bonus on max
            rightSideBonusCoefficient = 1f;
            for (int j=0; j<rightSideParts.Count(); j++)
            {
                // to get the first legitimate point turn up the engines on the right side until you reach the minimum kinetic energy
                // then keep turning it up until all is turned up
                for (float i=0; i<PARTITIONS; i++)
                {
                    rightSideCoefficients[j] = (i + 1) / PARTITIONS;
                    //CalcBaseValues();
                    //float Ekin = CalcEkin();
                    //float Erot = CalcErot();
                    //if (Ekin >= EkinNeeded)
                    //    controlPoints.Add(EnergyToDestination(Ekin, Erot));
                }
            }
            // 2. next increase Ekin by turning up the left side in reverse (least rotating engines first)
            for (int j = leftSideParts.Count()-1; j >= 0; j--)
            {
                // now slowly add kinetic energy and as little rotational energy as possible (therefore the list had to be reversed)
                // go on and turn on all engines on the left side
                for (float i = 0; i < PARTITIONS; i++)
                {
                    bool turnedUp = TurnUpLeftSide(j, (i + 1) / PARTITIONS);
                    if (turnedUp)
                    {
                        CalcValuesAndTryToAddPoint();
                    }
                }
            }
            // 3. slowly turn off the right rot bonus
            for (float i = PARTITIONS-1; i >= 0; i--)
            {
                rightSideBonusCoefficient = i / PARTITIONS;
                CalcValuesAndTryToAddPoint();
            }
            // 4. slowly turn on the left rot bonus
            for (float i = 1; i <= PARTITIONS; i++)
            {
                leftSideBonusCoefficient = i / PARTITIONS;
                CalcValuesAndTryToAddPoint();
            }
            // 5. turn down the right side (high rotation engines first)
            for (int j = 0; j < rightSideParts.Count(); j++)
            {
                // 
                for (float i = PARTITIONS - 1; i >= 0; i--)
                {
                    bool turnedDown = TurnDownRightSide(j, i / PARTITIONS);
                    if (turnedDown)
                    {
                        // try to add the new point
                        if (!CalcValuesAndTryToAddPoint())
                        {
                            // but if EkinNeeded is not met make sure it is
                            IncreaseUntilEkinIsMet();
                        }
                    }
                }
            }
            // 6. turn down the left side in reverse (reduce Ekin first)
            for (int j = leftSideParts.Count() - 1; j >= 0; j--)
            {
                // now slowly remove kinetic energy and as little rotational energy as possible
                // go on and turn on off engines on the left side until the necessary kinetic energy is no longer met
                for (float i = PARTITIONS - 1; i >= 0; i--)
                {
                    bool turnedDown = TurnDownLeftSide(j, i / PARTITIONS);
                    if (turnedDown)
                    {
                        // try to add the new point
                        if (!CalcValuesAndTryToAddPoint())
                        {
                            // but if EkinNeeded is not met make sure it is
                            IncreaseUntilEkinIsMet();
                        }
                    }
                }
            }
            // now reduce ErotBonusLeft slowly to zero
            for (float j = PARTITIONS - 1; j >= 0; j--)
            {
                leftSideBonusCoefficient = j / PARTITIONS;
                if (!CalcValuesAndTryToAddPoint())
                    IncreaseUntilEkinIsMet();
            }
            // increase ErotBonusRight to maximum
            for (float j = 1; j <= PARTITIONS; j++)
            {
                rightSideBonusCoefficient = j / PARTITIONS;
                if (!CalcValuesAndTryToAddPoint())
                    IncreaseUntilEkinIsMet();
            }
            // turn down the left side (rotational first) and turn up the right side
            // make sure there is enough Ekin
                for (int j = 0; j < leftSideParts.Count(); j++)
                {
                    for (float i = PARTITIONS - 1; i >= 0; i--)
                    {
                        bool turnedDown = TurnDownLeftSide(j, i / PARTITIONS);
                        if (turnedDown)
                        {
                        if (!CalcValuesAndTryToAddPoint())
                                IncreaseUntilEkinIsMet();
                        }
                    }
                }
            // turn up the right side (rotational first)
            
            for (int j = 0; j < rightSideParts.Count(); j++)
            {
                for (float i = 1; i <= PARTITIONS; i++)
                {
                    bool turnedUp = TurnUpRightSide(j, i / PARTITIONS);
                    if (turnedUp)
                    {
                        if (!CalcValuesAndTryToAddPoint())
                            IncreaseUntilEkinIsMet();
                    }
                }
            }
            

            //Console.WriteLine("Algorithm found " + controlPoints.Count() + " points");
            //foreach (var point in controlPoints)
                //Console.WriteLine(point);
            //Console.WriteLine(controlPoints);
            // special case: not enough points found
            // in this case set the maneuverPolygon to a small predefined square in front of the aircraft
            // and this also means that the aircraft lost its ability to fly, so kill it
            if (controlPoints.Count() < 5)
            {
                controlPoints.Clear();
                var vel = Velocity;
                if (Velocity < V_MIN)
                    vel = V_MIN;
                float range = vel * Constants.TURN_DURATION * 0.4f;
                controlPoints.Add(new CCPoint(range, 1));
                controlPoints.Add(new CCPoint(range + 1, 1));
                controlPoints.Add(new CCPoint(range + 1, -1));
                controlPoints.Add(new CCPoint(range, -1));
                if (deathPossible)
                    Die();
            }
            // now create the polygon and update
            var newManeuverPolygon = new PolygonWithSplines(controlPoints.ToArray());
            CalcBaseValues();
            // make sure the maneuverPolygon isn't too close to CCPoint.Zero
            if (EnergyToDestination(CalcEkin(), 0).Length < 30f)
            {
                var oldPivot = newManeuverPolygon.PivotPoint;
                newManeuverPolygon.MoveBy(10f, 0);
                newManeuverPolygon.PivotPoint = oldPivot;
            }
            UpdateManeuverPolygonToThis(newManeuverPolygon);
        }

        /// <summary>
        /// When an enemy aircraft is no longer inside an active chunk it calls this function.
        /// This function causes it to deallocate unnecessary memory (clouds, pathpoints).
        /// </summary>
        internal void PrepareForStandby()
        {
            foreach (var part in TotalParts)
            {
                if (part.ManeuverAbility != null && part.ManeuverAbility.CloudTailNode != null)
                    part.ManeuverAbility.CloudTailNode.Clear();
            }
            FlightPathControlNode.ClearPathPoints();
        }

        internal void Die()
        {
            MyState = State.SHOT_DOWN;
            HighNodeWhenDead = new CCDrawNode();
            LowNodeWhenDead = new CCDrawNode();
            LowNodeWhenDead.BlendFunc  = CCBlendFunc.NonPremultiplied;
            HighNodeWhenDead.BlendFunc = CCBlendFunc.NonPremultiplied;
            Parent.AddChild(HighNodeWhenDead);
            Parent.AddChild(LowNodeWhenDead);
        }

        // don't update the cloud nodes at every frame
        internal const byte CLOUD_FRAME_COUNTDOWN = 1;
        internal static byte CloudFrameCountdown = CLOUD_FRAME_COUNTDOWN;
        public void UseDrawNodes(CCDrawNode highNode, CCDrawNode lowNode)
        {
            var correctHighNode = highNode;
            var correctLowNode = lowNode;
            if (MyState == State.SHOT_DOWN)
            {
                correctHighNode = HighNodeWhenDead;
                correctLowNode = LowNodeWhenDead;
                // clear the nodes before using them
                HighNodeWhenDead.Clear();
                LowNodeWhenDead.Clear();
            }
            else if (SelectedPower == PowerUp.PowerType.SHIELD)
            {
                // draw the shield bubble
                lowNode.DrawSolidCircle(Position, ScaledContentSize.Width * 0.95f, CCColor4B.White);
                lowNode.DrawSolidCircle(Position, ScaledContentSize.Width * 0.95f - 16f, CCColor4B.Black);
            }
            foreach (var part in TotalParts)
            {
                if (part.ManeuverAbility != null && part.ManeuverAbility.CloudTailNode != null)
                    part.ManeuverAbility.CloudTailNode.UseDrawNodes(correctHighNode, correctLowNode);

                foreach (var damageTail in part.DamageCloudTailNodes)
                    damageTail.UseDrawNodes(correctHighNode, correctLowNode);

                // DEBUG: draw the collision polygon of each part
                /*
                var poly = (Polygon)((CollisionTypePolygon)part.CollisionType).collisionPolygon.Clone();
                poly.TransformAccordingToGameObject(part);
                correctHighNode.DrawPolygon(poly.Points, poly.Points.Length, CCColor4B.Transparent, 2f, CCColor4B.Aquamarine);
                */
            }
        }

        /// <summary>
        /// Calculates where the aircraft is going to end up in one turn, based on the energetic data given
        /// </summary>
        /// <param name="Ekin"></param>
        /// <param name="Erot"></param>
        /// <param name="ErotBonus"></param>
        /// <returns></returns>
        internal CCPoint EnergyToDestination(float Ekin, float Erot)
        {
            // Erot = 1/2 * I * w^2     ->    w = sqrt((2 * Erot) / I)
            float w = (float)Math.Sqrt((2 * Math.Abs(Erot)) / MomentOfInertia);
            w = w * Math.Sign(Erot);
            // w * T = phiMax
            // distance = phiMax * R
            // -> R = distance / phiMax
            // to find the distance analyse Ekin:
            // distance = v * T
            // Ekin = 1/2 * m * v^2
            // v = sqrt((2 * Ekin) / m)
            float v = (float)Math.Sqrt((2 * Ekin) / Mass) * Constants.STANDARD_SCALE;
            float distance = v * Constants.TURN_DURATION;
            float phiMax = w * Constants.TURN_DURATION;
            float radius = (float)Math.Abs(distance / phiMax);
            // now we know the radius of the circle on which the flight path lies
            // to get the position of the circle check whether the the curve goes left (w > 0), right (w < 0), or straight (w = 0)
            // and place the circle accordingly
            CCPoint posCircle = new CCPoint(0, w > 0 ? radius : w < 0 ? -radius : 0);
            CCPoint destination = new CCPoint(0,0);
            // if the path is not straight
            if (w != 0)
            {
                if (w > 0)  // circle is above
                {
                    destination = CCPoint.RotateByAngle(CCPoint.Zero, posCircle, phiMax);
                }
                if (w < 0)  // circle is below
                {
                    destination = CCPoint.RotateByAngle(CCPoint.Zero, posCircle, phiMax);
                }
            }
            else
            {
                destination = new CCPoint(distance, 0);
            }
            return destination;
        }

        internal void RotateTo(float direction)
        {
            RotateBy(direction - MyRotation);
        }

        internal CCPoint VelocityVector { get; private protected set; }

        /// <summary>
        /// Execute your orders for dt seconds
        /// </summary>
        /// <param name="dt">the time since the last Update call</param>
        /// <returns>whether the aircraft is done executing it orders</returns>
        internal bool ExecuteOrders(float dt)
        {
            // first make the controls invisible
            FlightPathControlNode.Visible = false;
            // for now all aircrafts can do is follow their flight path
            // calculate the velocity
            var oldPosition = Position;
            // advance dt seconds on the path
            bool finished = FlightPathControlNode.Advanche(dt);
            VelocityVector = new CCPoint((PositionX - oldPosition.X) / dt, (PositionY - oldPosition.Y) / dt);
            // let your parts act 
            foreach (var part in TotalParts)
                part.ExecuteOrders(dt);
            // fall from the sky if dead
            if (MyState == State.SHOT_DOWN)
                ChangeVertexZ(dt * (-500 + VertexZ / 2));
            return finished;
        }

        protected void ChangeVertexZ(float value)
        {
            VertexZ += value;
            if (!((int)VertexZ).Equals(ZOrder))
                ZOrder = (int)VertexZ;
            // also update your draw nodes
            if (HighNodeWhenDead != null)
            {
                HighNodeWhenDead.VertexZ = VertexZ;
                HighNodeWhenDead.ZOrder  = ZOrder + 1;
            }
            if (LowNodeWhenDead != null)
            {
                LowNodeWhenDead.VertexZ = VertexZ;
                LowNodeWhenDead.ZOrder  = ZOrder - 1;
            }
            if (VertexZ < Constants.VERTEX_Z_GROUND)
                TouchTheGround();
        }

        /// <summary>
        /// Falling aircrafts meet the groud at some point.
        /// This function is called when that happens.
        /// </summary>
        protected void TouchTheGround()
        {
            // for now all that happens is that the aircraft is to be removed
            ToBeRemoved = true;
        }

        internal Aircraft() : base()
        {
            ControlledByPlayer = false;
            PartsChanged();
            // DEBUG:
            /*
            ChangePowerUpCount(PowerUp.PowerType.SHIELD, 2);
            ChangePowerUpCount(PowerUp.PowerType.HEAL, 2);
            ChangePowerUpCount(PowerUp.PowerType.BOOST, 2);
            ChangePowerUpCount(PowerUp.PowerType.BACK_TURN, 2);
            */
            /*
            AddChild(maneuverPolygonDrawNode);
            maneuverPolygonDrawNode.Scale = 1 / Constants.STANDARD_SCALE;
            IsManeuverPolygonDrawn = false;
            */
        }

        internal void TryToSetFlightPathHeadTo(CCPoint position)
        {
            FlightPathControlNode.MoveHeadToClosestPointInsideManeuverPolygon(position);
        }

        internal void ResetFlightPathNode()
        {
            FlightPathControlNode.ResetHeadPosition();
            // DEBUG
           // Console.WriteLine("ZOrder: " + FlightPathControlNode.ZOrder);
        }

        internal void ChangeColor(CCColor3B newColor)
        {
            foreach (var part in TotalParts)
                part.Color = newColor;
        }

        internal void ColorByPlayLayer(CCColor4B planeColor)
        {
            foreach (var part in TotalParts)
            {
                if (part.MyState == Part.State.ACTIVE)
                    part.Color = new CCColor3B(planeColor);
                else
                    part.Color = Part.PlaneToDeathColor(planeColor);
            }
        }

        //private CCDrawNode shieldDrawNode = new CCDrawNode();

        protected override void AddedToScene()
        {
            base.AddedToScene();
            // DrawNodes have no Size, therefore we need to position them correctly at the center of the node
            //maneuverPolygonDrawNode.Position = new CCPoint(ContentSize.Width/2, ContentSize.Height / 2);
            //shieldDrawNode.Position = new CCPoint(ContentSize.Width / 2, ContentSize.Height / 2);
            //AddChild(shieldDrawNode, 2000);
            //shieldDrawNode.BlendFunc = CCBlendFunc.Additive; // additive mode is necessary to properly draw circles with line width using a workaround
            // add the FlightPathControlNode as a brother below you
            FlightPathControlNode = new FlightPathControlNode(this);
            if (Parent is PlayLayer pl)
                pl.AddChild(FlightPathControlNode, ZOrder - 1);
        }
        /// <summary>
        /// Should only be called while the aircraft is actually still a child (i.e. before it has been removed)
        /// </summary>
        public override void PrepareForRemoval()
        {
            // remove your brothers (FlightPathControlNode & CloudTailNode)
            if (Parent != null && Parent is PlayLayer)
            {
                Parent.RemoveChild(FlightPathControlNode);
                if (HighNodeWhenDead?.Parent == Parent)
                    Parent.RemoveChild(HighNodeWhenDead);
                if (LowNodeWhenDead?.Parent == Parent)
                    Parent.RemoveChild(LowNodeWhenDead);
            }
        }

        /// <summary>
        /// searches and returns all parts that this aircraft is made of
        /// starting at the body and then searching recursively
        /// </summary>
        internal List<Part> TotalParts { get; set; }
        internal bool ToBeRemoved { get; private set; } = false;

        internal Squadron Squadron { get; set; }

        /// <summary>
        /// Set into a state so that the planning phase can act properly on this aircraft
        /// </summary>
        internal void PrepareForPlanningPhase()
        {
            UpdateManeuverPolygon();
            FlightPathControlNode.ResetHead();  // reset the chosen powerup back to normal mode
            FlightPathControlNode.ResetHeadPosition();
            if (AI != null && MyState != State.SHOT_DOWN && (Squadron == null || !Squadron.InFormation))
            {
                AI.ActInPlanningPhase();
            }
            if (ControlledByPlayer && MyState != State.SHOT_DOWN)
                FlightPathControlNode.Visible = true;
        }

        internal IEnumerable<Aircraft> PlayerAircraftsInLevel()
        {
            var playLayer = (Layer as PlayLayer);
            if (playLayer != null)
                return playLayer.PlayerAircrafts;
            else
                return null;
        }

        internal bool IsActive()
        {
            return ControlledByPlayer || (Squadron != null ? Squadron.IsActive((PlayLayer)Layer) : ((PlayLayer)Layer).PosIsActive(Position));
        }

        internal IEnumerable<Aircraft> ActiveAircraftsInLevel()
        {
            var playLayer = (Layer as PlayLayer);
            if (playLayer != null)
                return playLayer.ActiveAircrafts;
            else
                return null;
        }
        /// <summary>
        /// Since it's very unlikely that I'll ever need more than 256 saving/loading commands I use a byte enum here.
        /// </summary>
        protected enum StreamEnum : byte
        {
            STOP = 0, AI = 1, BODY = 2
        }
        /// <summary>
        /// Writes all data to the stream that is necessary for recreating this aircraft later.
        /// </summary>
        /// <param name="stream"></param>
        public void WriteToStream(BinaryWriter writer)
        {
            // save the AI (is technically unnecessary as of now, but it gives me the flexibility to be able to save and load custom AIs later if wanted)
            if (AI != null)
            {
                writer.Write((byte)StreamEnum.AI);
                AI.WriteToStream(writer);
            }
            // save the body
            // since this works by recursion all other parts (which are mounted) are saved as well
            writer.Write((byte)StreamEnum.BODY);
            Body.WriteToStream(writer);
            writer.Write((byte)StreamEnum.STOP);
        }

        public static Aircraft CreateFromStream(BinaryReader reader)
        {
            Aircraft createdAircraft = new Aircraft();
            bool reading = true;
            while (reading)
            {
                StreamEnum nextEnum = (StreamEnum)reader.ReadByte();
                switch(nextEnum)
                {
                    case StreamEnum.AI:
                        {
                            createdAircraft.AI = AI.CreateFromStream(reader);
                        }
                        break;
                    case StreamEnum.BODY:
                        {
                            Part.CreateFromStream(reader, createdAircraft, isBody:true);
                        }
                        break;
                    case StreamEnum.STOP:
                    default:
                        reading = false;
                        break;
                }
            }
            return createdAircraft;
        }
    }
}
