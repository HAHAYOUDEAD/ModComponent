﻿using ModComponentAPI;
using ModComponent.Utils;
using UnityEngine;

namespace ModComponentMapper.ComponentMapper
{
	internal static class CarryingCapacityMapper
	{
		internal static void Configure(ModBaseComponent modComponent) => Configure(ComponentUtils.GetGameObject(modComponent));
		public static void Configure(GameObject prefab)
		{
			ModCarryingCapacityBehaviour capacityComponent = ComponentUtils.GetComponent<ModCarryingCapacityBehaviour>(prefab);
			if (capacityComponent == null) return;

			CarryingCapacityBuff capacityBuff = ComponentUtils.GetOrCreateComponent<CarryingCapacityBuff>(capacityComponent);

			capacityBuff.m_IsWorn = ComponentUtils.GetComponent<ModClothingComponent>(capacityComponent) != null
				|| ComponentUtils.GetComponent<ClothingItem>(capacityComponent) != null;

			capacityBuff.m_CarryingCapacityBuffValues = new CarryingCapacityBuff.BuffValues()
			{
				m_MaxCarryCapacityKGBuff = capacityComponent.MaxCarryCapacityKGBuff
			};
		}
	}
}
