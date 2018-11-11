﻿//  Copyright 2014 Invex Games http://invexgames.com
//	Licensed under the Apache License, Version 2.0 (the "License");
//	you may not use this file except in compliance with the License.
//	You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
//	Unless required by applicable law or agreed to in writing, software
//	distributed under the License is distributed on an "AS IS" BASIS,
//	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//	See the License for the specific language governing permissions and
//	limitations under the License.

using System;
using UnityEngine;

namespace MaterialUI
{
	public class ScreenManager : MonoBehaviour
	{
		public ScreenConfig[] screens;
		[HideInInspector]
		public ScreenConfig currentScreen;
		[HideInInspector]
		public ScreenConfig lastScreen;
        public PatientJournalScreen patientJournalScreen;
        private bool Choice;
        private void Start()
        {
            patientJournalScreen = GameObject.Find("ScreenCSUPatientJournal").GetComponent<PatientJournalScreen>();
            Choice = false;
        }
        public void Set(int index)
		{
            if(index==3&&!Choice)
            {
                patientJournalScreen._TrackerType = TrackerManager.TrackerType.CSU;
                Choice = true;
            }
            else if(index==2 && !Choice)
            {
                patientJournalScreen._TrackerType = TrackerManager.TrackerType.Symptom;
                Choice = true;
            }
            else if(index==6)
            {
                patientJournalScreen._TrackerType = TrackerManager.TrackerType.Symptom;
            }
            else if(index==5)
            {
                patientJournalScreen._TrackerType = TrackerManager.TrackerType.Asthma;
            }
			screens[index].transform.SetAsLastSibling();

			screens[index].Show(currentScreen);
			lastScreen = currentScreen;
			currentScreen = screens[index];
		}

		public void Set(string name)
		{
			for (int i = 0; i < screens.Length; i++)
			{
				if (screens[i].screenName == name)
				{
					Set(i);
					return;
				}
			}
		}

		public void Back()
		{
			lastScreen.ShowWithoutTransition();
			currentScreen.Hide();
			ScreenConfig temp = currentScreen;
			currentScreen = lastScreen;
			lastScreen = temp;
		}
	}
}
