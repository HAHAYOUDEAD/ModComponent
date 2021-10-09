﻿namespace ModComponentAPI
{
	internal enum ModPowderIngredientType
	{
		None,
		Salt,
		Yeast
	}

	internal class ModPowderIngredient
	{
		public ModPowderIngredientType ingredientType;
		public float weightRequired;
	}
}