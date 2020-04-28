﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace CocosSharpMathGame
{
    /// <summary>
    /// has no moveable camera; is drawn last (on top of everything else);
    /// holds the GUI for the PlayScene
    /// </summary>
    public class GUILayer : CCLayerColor
    {
        internal ExecuteOrdersButton ExecuteOrdersButton { get; private set; } = new ExecuteOrdersButton();
        private PlayLayer PlayLayer { get; set; }
        public GUILayer(PlayLayer playLayer) : base(CCColor4B.Transparent)
        {
            PlayLayer = playLayer;
            AddChild(ExecuteOrdersButton);
        }
        protected override void AddedToScene()
        {
            base.AddedToScene();
            var bounds = VisibleBoundsWorldspace;
            ExecuteOrdersButton.Position = ExecuteOrdersButton.Position = new CCPoint(bounds.MinX + ExecuteOrdersButton.ScaledContentSize.Width, bounds.MaxY - ExecuteOrdersButton.ScaledContentSize.Height);
        }

        internal void ExecuteOrders()
        {
            PlayLayer.ExecuteOrders();
        }
    }
}