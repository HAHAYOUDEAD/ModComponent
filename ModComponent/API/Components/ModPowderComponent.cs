﻿using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using Il2CppTLD.Gear;
using Il2CppTLD.IntBackedUnit;
using MelonLoader.TinyJSON;
using ModComponent.Utils;
using UnityEngine;

namespace ModComponent.API.Components;

[MelonLoader.RegisterTypeInIl2Cpp(false)]
public partial class ModPowderComponent : ModBaseComponent
{
	/// <summary>
	/// The type of powder this container holds. "Gunpowder"
	/// </summary>
	public PowderType ModPowderType;

	/// <summary>
	/// The maximum weight this container can hold.
	/// </summary>
	public float CapacityKG;

	/// <summary>
	/// The percent probability that this container will be found full.
	/// </summary>
	public float ChanceFull = 100f;

	void Awake()
	{
		CopyFieldHandler.UpdateFieldValues(this);

		PowderItem powderItem = this.GetComponent<PowderItem>();
		GearItem gearItem = this.GetComponent<GearItem>();
		if (powderItem && gearItem && !gearItem.m_BeenInspected && ChanceFull != 100f)
		{
			if (!RandomUtils.RollChance(ChanceFull))
			{
				powderItem.m_Weight = new ItemWeight((long)(powderItem.m_WeightLimit.m_Units * RandomUtils.Range(0.125f, 1f)));
			}
		}
	}

	public ModPowderComponent(System.IntPtr intPtr) : base(intPtr) { }

	[HideFromIl2Cpp]
	internal override void InitializeComponent(ProxyObject dict, string className = "ModPowderComponent")
	{
		base.InitializeComponent(dict, className);
		this.ModPowderType = ScriptableObject.CreateInstance<PowderType>();
		this.CapacityKG = dict.GetVariant(className, "CapacityKG");
		this.ChanceFull = dict.GetVariant(className, "ChanceFull");
	}
}
