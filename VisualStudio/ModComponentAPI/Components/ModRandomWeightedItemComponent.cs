﻿using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace ModComponentAPI
{
	public class ModRandomWeightedItemComponent : ModComponent
	{
		public string[] ItemNames = new string[0];

		public int[] ItemWeights = new int[0];

		public ModRandomWeightedItemComponent(System.IntPtr intPtr) : base(intPtr) { }

		void Awake()
		{
			CopyFieldHandler.UpdateFieldValues<ModRandomWeightedItemComponent>(this);
		}
		void Update()
		{
			if (ModComponentMain.Settings.options.disableRandomItemSpawns) return;
			if (this.ItemNames is null || this.ItemNames.Length == 0)
			{
				Logger.LogWarning("'{0}' had an invalid list of potential spawn items.", this.name);
				GameObject.Destroy(this.gameObject);
				return;
			}
			if (this.ItemWeights is null || this.ItemWeights.Length == 0)
			{
				Logger.LogWarning("'{0}' had an invalid list of item spawn weights.", this.name);
				GameObject.Destroy(this.gameObject);
				return;
			}
			if (this.ItemWeights.Length != this.ItemNames.Length)
			{
				Logger.LogWarning("The lists of item names and spawn weights for '{0}' had unequal length.", this.name);
				GameObject.Destroy(this.gameObject);
				return;
			}

			int index = this.GetIndex();
			GameObject prefab = Resources.Load(this.ItemNames[index])?.Cast<GameObject>();
			if (prefab is null)
			{
				Logger.LogWarning("Could not use '{0}' to spawn random item '{1}'", this.name, this.ItemNames[index]);
				GameObject.Destroy(this.gameObject);
				return;
			}

			GameObject gear = GameObject.Instantiate(prefab, this.transform.position, this.transform.rotation);
			gear.name = prefab.name;
			DisableObjectForXPMode xpmode = gear?.GetComponent<DisableObjectForXPMode>();
			if (xpmode != null) GameObject.Destroy(xpmode);
			GameObject.Destroy(this.gameObject);
		}

		[HideFromIl2Cpp]
		private int GetIndex()
		{
			if (this.ItemNames.Length == 1) return 0;

			int randomValue = ModComponentMapper.RandomUtils.Range(0, GetTotalWeight());
			int runningTotal = 0;
			int count = 0;
			foreach (int weight in this.ItemWeights)
			{
				runningTotal += weight;
				if (runningTotal > randomValue) return count;
				else count++;
			}
			Logger.LogError("Bug found while running 'GetIndex' for '{0}'. For loop did not return a value.", this.name);
			return ItemNames.Length - 1; //should never happen
		}

		[HideFromIl2Cpp]
		private int GetTotalWeight()
		{
			if (this.ItemWeights is null) return 0;
			int result = 0;
			foreach (int weight in this.ItemWeights)
			{
				result += weight;
			}
			return result;
		}
	}
}