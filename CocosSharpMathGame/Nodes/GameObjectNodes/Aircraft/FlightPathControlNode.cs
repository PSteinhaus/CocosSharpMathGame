﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace CocosSharpMathGame
{
    internal class FlightPathControlNode : GameObjectNode
    {
        private FlightPathNode FlightPathNode { get; set; }
        private FlightPathHead FlightPathHead { get; set; }
        internal PowerUp.PowerType SelectedPower { get { return FlightPathHead.SelectedPower; } }
        internal Aircraft Aircraft { get; set; }
        internal CCPoint FlightPathHeadPos
        {
            get
            {
                return FlightPathHead.Position;
            }
        }
        internal FlightPathControlNode(Aircraft aircraft)
        {
            if (!aircraft.ControlledByPlayer)
                Visible = false;
            Aircraft = aircraft;
            FlightPathNode = new FlightPathNode();
            FlightPathHead = new FlightPathHead();
            AddChild(FlightPathNode);
            AddChild(FlightPathHead);
            Scale = 1f;
        }

        protected override void AddedToScene()
        {
            base.AddedToScene();
            ResetHeadPosition();
        }

        internal void ResetHeadPosition()
        {
            float pathLength = Aircraft.ScaledContentSize.Width;
            // get the mathematical direction of the aircraft
            Constants.CCDegreesToDxDy(Aircraft.MyRotation, out float dx, out float dy);
            MoveHeadToClosestPointInsideManeuverPolygon( new CCPoint(Aircraft.PositionX + dx*pathLength, Aircraft.PositionY + dy*pathLength) );
        }
        internal void MoveHeadToClosestPointInsideManeuverPolygon(CCPoint point)
        {
            // find the closest point that is still inside the maneuver polygon
            CCPoint closestPoint = Aircraft.ManeuverPolygon.ClosestTo(point);
            MoveHeadTo(closestPoint);
        }
        private void MoveHeadTo(CCPoint point)
        {
            MoveHeadTo(point.X, point.Y);
        }

        private void MoveHeadTo(float x, float y)
        {
            // move the head
            FlightPathHead.MoveTo(x, y);
            // if its a player aircraft make sure it doesn't move too far away from the others
            if (Layer is PlayLayer pl && pl.PlayerAircrafts != null && pl.PlayerAircrafts.Contains(Aircraft))
                FlightPathHead.EnsureProximityToOtherPlayerHeads();
            // recalculate the flight path
            Constants.CCDegreesToDxDy(Aircraft.MyRotation, out float dx, out float dy);
            FlightPathNode.CalculatePath(Aircraft.Position, dx, dy, FlightPathHead.Position);
            if (Aircraft.ControlledByPlayer)
                FlightPathNode.DrawPath();
            // rotate the head according to the direction (not the slope itself) at the end of the flight path
            FlightPathHead.MyRotation = FlightPathNode.DirectionAt(FlightPathNode.Path.Length - 1);
        }

        /// <summary>
        /// Advance the Aircraft on the flight path as far as it can go in dt seconds.
        /// </summary>
        /// <param name="dt">how much time passed since the last Update</param>
        /// <returns>whether the end of the flight path is now reached</returns>
        internal bool Advanche(float dt)
        {
            // calculate how much distance can be crossed in dt
            var pathDifferenceInPercent = dt / Constants.TURN_DURATION;
            var distance = pathDifferenceInPercent * FlightPathNode.PathLength;
            //Console.WriteLine("distance " + distance);
            FlightPathNode.Advance(Aircraft.Position, distance, out CCPoint destination, out float CCfinalDirection);
            Aircraft.MoveTo(destination);
            Aircraft.RotateTo(CCfinalDirection);
            return destination.Equals(FlightPathNode.EndPoint);
        }

        internal void ClearPathPoints()
        {
            FlightPathNode.ClearPath();
        }

        internal void FlightPathHeadOnly()
        {
            FlightPathHead.ShowOnlyHead();
        }

        internal void ResetHead()
        {
            FlightPathHead.Head = PowerUp.FlightPathHeadOptionFromType(PowerUp.PowerType.NORMAL);
        }
    }
}
