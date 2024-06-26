﻿using Il2Cpp;
using Il2CppTLD.IntBackedUnit;
using MelonLoader;
using ModComponent.API;
using ModComponent.API.Components;
using ModComponent.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ModComponent.Mapper.ComponentMappers;

internal static class FoodMapper
{
	internal static void Configure(ModBaseComponent modComponent)
	{
		ModFoodComponent? modFoodComponent = modComponent.TryCast<ModFoodComponent>();
		if (modFoodComponent == null)
		{
			return;
		}

		FoodItem foodItem = ModComponent.Utils.ComponentUtils.GetOrCreateComponent<FoodItem>(modFoodComponent);
		GearItem gearItem = ModComponent.Utils.ComponentUtils.GetOrCreateComponent<GearItem>(modFoodComponent);


		foodItem.m_Nutrients = new Il2CppSystem.Collections.Generic.List<FoodItem.Nutrient>();
		if(modFoodComponent.VitaminC > 0)
		{
			FoodItem.Nutrient vitc = new FoodItem.Nutrient();
			vitc.m_Amount = modFoodComponent.VitaminC;
			vitc.m_Nutrient = new Il2CppTLD.Gameplay.AssetReferenceNutrientDefinition("13a8bda1e12982e428b7551cc01b01df");
			foodItem.m_Nutrients.Add(vitc);
		}

		foodItem.m_CaloriesTotal = modFoodComponent.Calories;
		foodItem.m_CaloriesRemaining = modFoodComponent.Calories;
		foodItem.m_ReduceThirst = modFoodComponent.ThirstEffect;

		foodItem.m_ChanceFoodPoisoning = Mathf.Clamp01(modFoodComponent.FoodPoisoning / 100f);
		foodItem.m_ChanceFoodPoisoningLowCondition = Mathf.Clamp01(modFoodComponent.FoodPoisoningLowCondition / 100f);
		foodItem.m_ChanceFoodPoisoningRuined = Mathf.Clamp01(modFoodComponent.FoodPoisoningLowCondition / 100f);
		foodItem.m_DailyHPDecayInside = ItemMapper.GetDecayPerStep(modFoodComponent.DaysToDecayIndoors, modFoodComponent.MaxHP);
		foodItem.m_DailyHPDecayOutside = ItemMapper.GetDecayPerStep(modFoodComponent.DaysToDecayOutdoors, modFoodComponent.MaxHP);

		foodItem.m_TimeToEatSeconds = Mathf.Clamp(1, modFoodComponent.EatingTime, 10);
		foodItem.m_TimeToOpenAndEatSeconds = Mathf.Clamp(1, modFoodComponent.EatingTime, 10) + 5;
		foodItem.m_EatingAudio = modFoodComponent.EatingAudio;
		foodItem.m_OpenAndEatingAudio = modFoodComponent.EatingPackagedAudio;
		foodItem.m_Packaged = !string.IsNullOrEmpty(foodItem.m_OpenAndEatingAudio);

		foodItem.m_IsDrink = modFoodComponent.Drink;
		foodItem.m_IsFish = modFoodComponent.Fish;
		foodItem.m_IsMeat = modFoodComponent.Meat;
		foodItem.m_IsRawMeat = modFoodComponent.Raw;
		foodItem.m_IsNatural = modFoodComponent.Natural;
		foodItem.m_MustConsumeAll = false;
		foodItem.m_ParasiteRiskPercentIncrease = ModComponent.Utils.ModUtils.NotNull(modFoodComponent.ParasiteRiskIncrements);

		foodItem.m_PercentHeatLossPerMinuteIndoors = 1;
		foodItem.m_PercentHeatLossPerMinuteOutdoors = 2;

		if (modFoodComponent.Opening)
		{
			foodItem.m_GearRequiredToOpen = true;
			foodItem.m_OpenedWithCanOpener = modFoodComponent.OpeningWithCanOpener;
			foodItem.m_OpenedWithHatchet = modFoodComponent.OpeningWithHatchet;
			foodItem.m_OpenedWithKnife = modFoodComponent.OpeningWithKnife;

			if (modFoodComponent.OpeningWithSmashing)
			{
				SmashableItem smashableItem = ModComponent.Utils.ComponentUtils.GetOrCreateComponent<SmashableItem>(modFoodComponent);
				smashableItem.m_MinPercentLoss = 10;
				smashableItem.m_MaxPercentLoss = 30;
				smashableItem.m_TimeToSmash = 6;
				smashableItem.m_SmashAudio = "Play_EatingSmashCan";
			}

			if (modFoodComponent.Canned)
			{
				foodItem.m_GearPrefabHarvestAfterFinishEatingNormal = AssetBundleUtils.LoadAsset<GameObject>("GEAR_RecycledCan");
			}
		}

		if (modFoodComponent.AffectRest)
		{
			FatigueBuff fatigueBuff = ModComponent.Utils.ComponentUtils.GetOrCreateComponent<FatigueBuff>(modFoodComponent);
			fatigueBuff.m_InitialPercentDecrease = modFoodComponent.InstantRestChange;
			fatigueBuff.m_RateOfIncreaseScale = 0.5f;
			fatigueBuff.m_DurationHours = modFoodComponent.RestFactorMinutes / 60f;
		}

		if (modFoodComponent.AffectCold)
		{
			FreezingBuff freezingBuff = ModComponent.Utils.ComponentUtils.GetOrCreateComponent<FreezingBuff>(modFoodComponent);
			freezingBuff.m_InitialPercentDecrease = modFoodComponent.InstantColdChange;
			freezingBuff.m_RateOfIncreaseScale = 0.5f;
			freezingBuff.m_DurationHours = modFoodComponent.ColdFactorMinutes / 60f;
		}

		if (modFoodComponent.AffectCondition)
		{
			ConditionRestBuff conditionRestBuff = ModComponent.Utils.ComponentUtils.GetOrCreateComponent<ConditionRestBuff>(modFoodComponent);
			conditionRestBuff.m_ConditionRestBonus = modFoodComponent.ConditionRestBonus;
			conditionRestBuff.m_NumHoursRestAffected = modFoodComponent.ConditionRestMinutes / 60f;
		}

		if (modFoodComponent.ContainsAlcohol)
		{
			AlcoholComponent alcohol = ModComponent.Utils.ComponentUtils.GetOrCreateComponent<AlcoholComponent>(modFoodComponent);
			alcohol.AmountTotal = modFoodComponent.WeightKG * modFoodComponent.AlcoholPercentage * 0.01f;
			alcohol.AmountRemaining = alcohol.AmountTotal;
			alcohol.UptakeSeconds = modFoodComponent.AlcoholUptakeMinutes * 60;
		}
		if (modFoodComponent.Fish)
		{
			FoodWeight fw = ModComponent.Utils.ComponentUtils.GetOrCreateComponent<FoodWeight>(modFoodComponent);
			fw.m_CaloriesPerKG = (foodItem.m_CaloriesTotal / gearItem.WeightKG.m_Units);
			fw.m_MaxWeight = gearItem.WeightKG * 1.2f;
			fw.m_MinWeight = gearItem.WeightKG * 0.8f;
			gearItem.m_FoodWeight = fw;
			gearItem.WeightKG = ItemWeight.Zero;

		}

		gearItem.m_FoodItem = foodItem;

		HoverIconsToShow hoverIconsToShow = ModComponent.Utils.ComponentUtils.GetOrCreateComponent<HoverIconsToShow>(modFoodComponent);
		hoverIconsToShow.m_HoverIcons = new HoverIconsToShow.HoverIcons[] { HoverIconsToShow.HoverIcons.Food };
	}
}
