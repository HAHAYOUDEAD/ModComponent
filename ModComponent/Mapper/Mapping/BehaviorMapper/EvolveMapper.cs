﻿using ModComponentAPI;
using ModComponent.Utils;
using UnityEngine;

namespace ModComponentMapper.ComponentMapper
{
	internal static class EvolveMapper
	{
		internal static void Configure(ModBaseComponent modComponent) => Configure(ComponentUtils.GetGameObject(modComponent));
		internal static void Configure(GameObject prefab)
		{
			ModEvolveBehaviour modEvolveComponent = ComponentUtils.GetComponent<ModEvolveBehaviour>(prefab);
			if (modEvolveComponent == null) return;

			EvolveItem evolveItem = ComponentUtils.GetOrCreateComponent<EvolveItem>(modEvolveComponent);
			evolveItem.m_ForceNoAutoEvolve = false;
			evolveItem.m_GearItemToBecome = GetTargetItem(modEvolveComponent.TargetItemName, modEvolveComponent.name);
			evolveItem.m_RequireIndoors = modEvolveComponent.IndoorsOnly;
			evolveItem.m_StartEvolvePercent = 0;
			evolveItem.m_TimeToEvolveGameDays = Mathf.Clamp(modEvolveComponent.EvolveHours / 24f, 0.01f, 1000);
		}

		private static GearItem GetTargetItem(string targetItemName, string reference)
		{
			GameObject targetItem = Resources.Load(targetItemName)?.Cast<GameObject>();
			if (ComponentUtils.GetModComponent(targetItem) != null)
			{
				// if this a modded item, map it now (no harm if it was already mapped earlier)
				Mapper.Map(targetItem);
			}

			return ModUtils.GetItem<GearItem>(targetItemName, reference);
		}
	}
}