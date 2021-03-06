﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace CocosSharpMathGame
{
    internal class UIElement : GameObjectSprite
    {
        static internal CCSpriteSheet spriteSheet = new CCSpriteSheet("ui.plist");
        internal bool IsCircleButton { get; private set; }
        internal bool SwallowTouch { get; set; } = true;
        internal bool TouchMustEndOnIt { get; set; }
        protected bool Pressed { get; set; } = false;
        internal bool Pressable { get; set; } = true;
        internal float RadiusFactor { get; set; } = 0.5f;
        internal UIElement(string textureName) : base(spriteSheet.Frames.Find(_ => _.TextureFilename.Equals(textureName)))
        {

        }

        internal void MakeClickable(bool touchMustEndOnIt=true, bool IsCircleButton=false, bool swallowTouch=true)
        {
            this.IsCircleButton = IsCircleButton;
            SwallowTouch = swallowTouch;
            TouchMustEndOnIt = touchMustEndOnIt;
            // add a touch listener
            var touchListener = new CCEventListenerTouchAllAtOnce();
            touchListener.OnTouchesBegan = OnTouchesBegan;
            touchListener.OnTouchesMoved = OnTouchesMoved;
            touchListener.OnTouchesEnded = OnTouchesEnded;
            touchListener.OnTouchesCancelled = OnTouchesEnded;
            AddEventListener(touchListener, this);
        }

        private protected void OnTouchesBegan(List<CCTouch> touches, CCEvent touchEvent)
        {
            if (Pressable && MyVisible && TouchStartedOnIt(touches[0]))
            {
                if (SwallowTouch) touchEvent.StopPropogation();
                Pressed = true;
                OnTouchesBeganUI(touches, touchEvent);
            }
        }
        /// <summary>
        /// Override this to do work when clicked
        /// </summary>
        /// <param name="touches"></param>
        /// <param name="touchEvent"></param>
        private protected virtual void OnTouchesBeganUI(List<CCTouch> touches, CCEvent touchEvent)
        {

        }

        private protected void OnTouchesMoved(List<CCTouch> touches, CCEvent touchEvent)
        {
            if (MyVisible && Pressed)
            {
                if (SwallowTouch) touchEvent.StopPropogation();
                OnTouchesMovedUI(touches, touchEvent);
            }
        }
        /// <summary>
        /// Override this to do work when pressed and the touch moves
        /// </summary>
        /// <param name="touches"></param>
        /// <param name="touchEvent"></param>
        private protected virtual void OnTouchesMovedUI(List<CCTouch> touches, CCEvent touchEvent)
        {

        }

        private protected void OnTouchesEnded(List<CCTouch> touches, CCEvent touchEvent)
        {
            if (MyVisible && (TouchMustEndOnIt ? TouchIsOnIt(touches[0]) : true) && Pressed)
            {
                if (SwallowTouch) touchEvent.StopPropogation();
                OnTouchesEndedUI(touches, touchEvent);
            }
            Pressed = false;
        }
        /// <summary>
        /// Override this to do work when pressed and released
        /// </summary>
        /// <param name="touches"></param>
        /// <param name="touchEvent"></param>
        private protected virtual void OnTouchesEndedUI(List<CCTouch> touches, CCEvent touchEvent)
        {

        }

        internal bool TouchStartedOnIt(CCTouch touch)
        {
            return IsCircleButton ? TouchStartedOnItCircle(touch) : TouchStartedOnItBox(touch);
        }
        internal bool TouchIsOnIt(CCTouch touch)
        {
            return IsCircleButton ? TouchIsOnItCircle(touch) : TouchIsOnItBox(touch);
        }
        internal bool TouchStartedOnItCircle(CCTouch touch)
        {
            return touch.StartLocation.IsNear(BoundingBoxTransformedToWorld.Center, BoundingBoxTransformedToWorld.Size.Width * RadiusFactor);
        }
        internal bool TouchIsOnItCircle(CCTouch touch)
        {
            return touch.Location.IsNear(BoundingBoxTransformedToWorld.Center, BoundingBoxTransformedToWorld.Size.Width * RadiusFactor);
        }
        internal bool TouchStartedOnItBox(CCTouch touch)
        {
            return BoundingBoxTransformedToWorld.ContainsPoint(touch.StartLocation);
        }
        internal bool TouchIsOnItBox(CCTouch touch)
        {
            return BoundingBoxTransformedToWorld.ContainsPoint(touch.Location);
        }
    }
}
