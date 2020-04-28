﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace CocosSharpMathGame
{
    abstract internal class UIElement : GameObjectSprite
    {
        static protected CCSpriteSheet spriteSheet = new CCSpriteSheet("ui.plist");
        protected bool Pressed { get; set; } = false;
        internal UIElement(string textureName) : base(spriteSheet.Frames.Find(_ => _.TextureFilename.Equals(textureName)))
        {

        }

        internal void MakeClickable(Action<List<CCTouch>,CCEvent> onTouchesBegan, Action<List<CCTouch>, CCEvent> onTouchesMoved = null, Action<List<CCTouch>, CCEvent> onTouchesEnded=null, Action<List<CCTouch>, CCEvent> onTouchesCancelled = null, bool touchMustEndOnIt=true, bool IsCircleButton=false)
        {
            Func<CCTouch, bool> touchStartedOnIt = null;
            Func<CCTouch, bool> touchIsOnIt = null;
            if (IsCircleButton)
            {
                touchStartedOnIt = TouchStartedOnItCircle;
                touchIsOnIt = TouchIsOnItCircle;
            }
            else
            {
                touchStartedOnIt = TouchStartedOnIt;
                touchIsOnIt = TouchIsOnIt;
            }
            // add a touch listener
            var touchListener = new CCEventListenerTouchAllAtOnce();
            // DEBUG: I don't yet know whether it is necessary to check for visibility; experiments will tell
            touchListener.OnTouchesBegan = (arg1, arg2) =>                                     { if (Visible && touchStartedOnIt(arg1[0]))                                  { Pressed = true;  onTouchesBegan(arg1, arg2); } };
            if(onTouchesMoved!=null) touchListener.OnTouchesMoved = (arg1, arg2) =>            { if (Visible && Pressed)                                                                       onTouchesMoved(arg1, arg2); };
            if (onTouchesEnded != null) touchListener.OnTouchesEnded = (arg1, arg2) =>         { if (Visible && touchMustEndOnIt ? touchIsOnIt(arg1[0]) : true && Pressed)  { Pressed = false; onTouchesEnded(arg1, arg2); } };
            else                        touchListener.OnTouchesEnded = (arg1, arg2) =>                                                                                        Pressed = false;
            if (onTouchesCancelled != null) touchListener.OnTouchesCancelled = (arg1, arg2) => { if (Visible && Pressed)                                                    { Pressed = false; onTouchesCancelled(arg1, arg2); } };
            else                            touchListener.OnTouchesCancelled = (arg1, arg2) =>                                                                                Pressed = false;
            AddEventListener(touchListener, this);
        }

        internal bool TouchStartedOnIt(CCTouch touch)
        {
            return BoundingBoxTransformedToWorld.ContainsPoint(touch.StartLocation);
        }
        internal bool TouchIsOnIt(CCTouch touch)
        {
            return BoundingBoxTransformedToWorld.ContainsPoint(touch.Location);
        }
        internal bool TouchStartedOnItCircle(CCTouch touch)
        {
            return touch.StartLocation.IsNear(BoundingBoxTransformedToWorld.Center, ScaledContentSize.Width / 2);
        }
        internal bool TouchIsOnItCircle(CCTouch touch)
        {
            return touch.Location.IsNear(BoundingBoxTransformedToWorld.Center, ScaledContentSize.Width / 2);
        }
    }
}