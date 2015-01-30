﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GuiMainMeun : MonoBehaviour
{
		public Animator[] myMainMenuAnimators;

		// Use this for initialization
		void Start ()
		{
	
		}

		public void CloseMainMenu ()
		{
				foreach (Animator tempAnimat in myMainMenuAnimators) {
						Button tmpButton = tempAnimat.gameObject.GetComponent<Button> ();
						tmpButton.interactable = false;
						tempAnimat.SetTrigger ("CloseMainMenu");
				}
		}
	
		// Update is called once per frame
		void Update ()
		{
	
		}
}