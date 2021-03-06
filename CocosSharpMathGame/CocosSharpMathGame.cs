﻿using Xamarin.Forms;
using CocosSharp;
using System;

namespace CocosSharpMathGame
{
	public class App : Application
	{
		public HangarLayer CurrentHangarLayer { get { return HangarLayer.GlobalHangarLayer; } }
		public bool FinishedLoading = false;
		public App ()
		{
			// The root page of your application
			MainPage = new CocosSharpMathGame.MainPage(this);
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected async override void OnSleep ()
		{
			// Handle when your app sleeps
			if (CurrentHangarLayer != null && FinishedLoading)
				await CurrentHangarLayer.SaveToFile();
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}

		public void OnBackPressed()
		{
			// return from the MODIFY_AIRCRAFT state (which would also be possible by double tapping)
			if (CurrentHangarLayer != null && CurrentHangarLayer.Parent != null && CurrentHangarLayer.State == HangarLayer.HangarState.MODIFY_AIRCRAFT && CurrentHangarLayer.GUILayer.DragAndDropObject == null)
				CurrentHangarLayer.StartTransition(HangarLayer.HangarState.WORKSHOP);
			// or return from the SCRAPYARD_CHALLENGE state (which would also be possible by tapping)
			else if (CurrentHangarLayer != null && CurrentHangarLayer.Parent != null && CurrentHangarLayer.State == HangarLayer.HangarState.SCRAPYARD_CHALLENGE)
				CurrentHangarLayer.StartTransition(HangarLayer.HangarState.SCRAPYARD);
		}
	}
}

