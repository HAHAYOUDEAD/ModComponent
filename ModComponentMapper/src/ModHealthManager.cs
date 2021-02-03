﻿using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnhollowerBaseLib.Attributes;

/* did a first pass through; HAS ISSUES!!!!!!!!!!!!!
 * A BUNCH need to be declared
 * Two issues
 * The first was adding console commands, which I fixed.
 * The second was a serialization issue; I added inheritance to Il2CppSystem.Object and that fixed things
 */
//may have some inlined methods at the bottom

namespace ModComponentMapper
{
    public class AlcoholUptake //needs declared
    {
        public float amountPerGameSecond;

        public float remainingGameSeconds;

        public static AlcoholUptake Create(float amount, float gameSeconds)
        {
            AlcoholUptake result = new AlcoholUptake();

            result.amountPerGameSecond = amount / gameSeconds;
            result.remainingGameSeconds = gameSeconds;

            return result;
        }
    }

    //needs declared!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    public class ModHealthManagerData : Il2CppSystem.Object //added inheritance to fix Data serialization issue
    {
        public float alcoholPermille;
        public AlcoholUptake[] uptakes;
    }

    //needs declared!!!!!!!!!!!!
    public class SaveProxy : Il2CppSystem.Object //added inheritance to fix Data serialization issue
    {
        public string data;

        public SaveProxy()
        {
            this.data = "";
        }
        public SaveProxy(System.IntPtr intPtr) : base(intPtr) { }
    }

    [HarmonyPatch(typeof(Condition), "UpdateBlurEffect")]//Exists
    internal class Condition_UpdateBlurEffect
    {
        public static void Prefix(Condition __instance, ref float percentCondition, ref bool lowHealthStagger)
        {
            lowHealthStagger = percentCondition <= __instance.m_HPToStartBlur || ModHealthManager.ShouldStagger();
            percentCondition = Math.Min(percentCondition, __instance.m_HPToStartBlur * (1 - ModHealthManager.GetAlcoholBlurValue()) + 0.01f);

            if (!lowHealthStagger)
            {
                GameManager.GetVpFPSCamera().m_BasePitch = Mathf.Lerp(GameManager.GetVpFPSCamera().m_BasePitch, 0.0f, 0.01f);
                GameManager.GetVpFPSCamera().m_BaseRoll = Mathf.Lerp(GameManager.GetVpFPSCamera().m_BaseRoll, 0.0f, 0.01f);
            }
        }
    }

    [HarmonyPatch(typeof(Freezing), "CalculateBodyTemperature")]//Exists
    internal class Freezing_CalculateBodyTemperature
    {
        public static void Postfix(ref float __result)
        {
            __result += ModHealthManager.GetBodyTempBonus();
        }
    }

    [HarmonyPatch(typeof(Frostbite), "CalculateBodyTemperatureWithoutClothing")]//Exists
    internal class Frostbite_CalculateBodyTemperatureWithoutClothing
    {
        public static void Postfix(ref float __result)
        {
            __result += ModHealthManager.GetFrostbiteTempBonus();
        }
    }

    [HarmonyPatch(typeof(GameManager), "Start")]//Exists
    internal class GameManagerStartPatch
    {
        public static void Postfix(PlayerManager __instance)
        {
            ModHealthManager.instance = __instance.gameObject.AddComponent<ModHealthManager>();
        }
    }

    internal class ModHealthManager : MonoBehaviour //Needs declared
    {
        private const float MIN_PERMILLE_FOR_BLUR = 0.5f;
        private const float MAX_PERMILLE_FOR_BLUR = 2.5f;
        private const float MIN_PERMILLE_FOR_STAGGERING = 1f;

        private const float ALCOHOL_TO_PERMILLE = 18;
        private const float MIN_UPTAKE_SCALE = 0.1f;

        private const float PERMILLE_REDUCTION_PER_GAME_SECOND = 0.15f / 3600f;
        private const float THIRST_PER_PERMILLE_SECOND = 25f / 3600f;
        private const float FATIGUE_PER_PERMILLE_SECOND = 4.583f / 3600f;
        private const float FROSTBITE_TEMP_BONUS_PER_PERMILLE = 3;
        private const float BODY_TEMP_BONUS_PER_PERMILLE = -2;

        internal static ModHealthManager instance;

        private List<AlcoholUptake> alcoholUptakes = new List<AlcoholUptake>();
        private float permille;

        private StatMonitor thirstMonitor = new StatMonitor();
        private StatMonitor fatigueMonitor = new StatMonitor();

        private ModHealthManager()
        {
            ResetStatMonitors();
        }

        public ModHealthManager(IntPtr intPtr) : base(intPtr) { }

        public static void DrankAlcohol(float amount, float uptakeGameSeconds)
        {
            if (instance == null)
            {
                Implementation.Log("ModHealthManager not initialized.");
                return;
            }

            Hunger hunger = GameManager.GetHungerComponent();
            float hungerScale = Mathf.Clamp01(Math.Max(MIN_UPTAKE_SCALE, hunger.GetCalorieReserves() / hunger.m_MaxReserveCalories));
            float scaledUptakeGameSeconds = uptakeGameSeconds * hungerScale;
            instance.alcoholUptakes.Add(AlcoholUptake.Create(amount, scaledUptakeGameSeconds));
        }

        public static float GetAlcoholPermille()
        {
            return instance.permille;
        }

        public static StatMonitor GetFatigueMonitor()
        {
            return instance.fatigueMonitor;
        }

        public static StatMonitor GetThirstMonitor()
        {
            return instance.thirstMonitor;
        }

        internal static float GetAlcoholBlurValue()
        {
            return Mathf.Clamp01((instance.permille - MIN_PERMILLE_FOR_BLUR) / (MAX_PERMILLE_FOR_BLUR - MIN_PERMILLE_FOR_BLUR));
        }

        internal static float GetBodyTempBonus()
        {
            return instance.permille * BODY_TEMP_BONUS_PER_PERMILLE;
        }

        internal static ModHealthManagerData GetData()
        {
            ModHealthManagerData result = new ModHealthManagerData();

            result.alcoholPermille = instance.permille;
            result.uptakes = instance.alcoholUptakes.ToArray<AlcoholUptake>();

            return result;
        }

        internal static float GetFrostbiteTempBonus()
        {
            return instance.permille * FROSTBITE_TEMP_BONUS_PER_PERMILLE;
        }

        internal static void Initialize()
        {
            //Wulf: two commands, because apparently different regions don't express this in the same unit
            //uConsole.RegisterCommand("set_alcohol_permille", new uConsole.DebugCommand(SetAlcoholPermille));//<=================================================
            //uConsole.RegisterCommand("set_alcohol_percent", new uConsole.DebugCommand(SetAlcoholPercent));//<========
            uConsole.RegisterCommand("set_alcohol_permille", new Action(SetAlcoholPermille));
            uConsole.RegisterCommand("set_alcohol_percent", new Action(SetAlcoholPercent));
        }

        internal static void SetData(ModHealthManagerData data)
        {
            if (data == null)
            {
                return;
            }

            instance.permille = data.alcoholPermille;

            instance.alcoholUptakes.Clear();
            if (data.uptakes != null)
            {
                instance.alcoholUptakes.AddRange(data.uptakes);
            }

            instance.ResetStatMonitors();
        }

        internal static bool ShouldStagger()
        {
            return GetAlcoholPermille() >= MIN_PERMILLE_FOR_STAGGERING;
        }

        internal void Update()
        {
            float elapsedGameSeconds = GameManager.GetTimeOfDayComponent().GetTODSeconds(Time.deltaTime);
            if (elapsedGameSeconds <= 0)
            {
                return;
            }

            UpdateStatMonitors(elapsedGameSeconds);
            ProcessAlcohol(elapsedGameSeconds);
        }

        private static void SetAlcoholPercent()
        {
            if (uConsole.GetNumParameters() != 1)
            {
                Debug.Log("  exactly one parameter required");
                return;
            }

            instance.permille = uConsole.GetFloat() * 10f;
        }

        private static void SetAlcoholPermille()
        {
            if (uConsole.GetNumParameters() != 1)
            {
                Debug.Log("  exactly one parameter required");
                return;
            }

            instance.permille = uConsole.GetFloat();
        }

        [HideFromIl2Cpp]
        private void ProcessAlcohol(float elapsedGameSeconds)
        {
            for (int i = alcoholUptakes.Count - 1; i >= 0; i--)
            {
                AlcoholUptake uptake = alcoholUptakes[i];
                permille += elapsedGameSeconds * uptake.amountPerGameSecond * ALCOHOL_TO_PERMILLE;
                uptake.remainingGameSeconds -= elapsedGameSeconds;

                if (uptake.remainingGameSeconds <= 0)
                {
                    alcoholUptakes.RemoveAt(i);
                }
            }

            if (permille > 0)
            {
                permille -= elapsedGameSeconds * PERMILLE_REDUCTION_PER_GAME_SECOND;
            }

            GameManager.GetThirstComponent().AddThirst(elapsedGameSeconds * permille * THIRST_PER_PERMILLE_SECOND);
            GameManager.GetFatigueComponent().AddFatigue(elapsedGameSeconds * permille * FATIGUE_PER_PERMILLE_SECOND);
        }

        [HideFromIl2Cpp]
        private void ResetStatMonitors()
        {
            thirstMonitor.value = GameManager.GetThirstComponent().m_CurrentThirst;
            thirstMonitor.hourlyBaseline = GameManager.GetThirstComponent().m_ThirstIncreasePerDay / 24 * GameManager.GetExperienceModeManagerComponent().GetThirstRateScale();
            thirstMonitor.hourlyChange = 0;
            thirstMonitor.offset = 1;
            thirstMonitor.scale = 0.2f;
            thirstMonitor.debug = true;

            fatigueMonitor.value = GameManager.GetFatigueComponent().m_CurrentFatigue;
            fatigueMonitor.hourlyBaseline = GameManager.GetFatigueComponent().m_FatigueIncreasePerHourStanding * GameManager.GetExperienceModeManagerComponent().GetFatigueRateScale();
            fatigueMonitor.hourlyBaseline = 0;
            fatigueMonitor.hourlyChange = 0;
            fatigueMonitor.offset = 0;
            fatigueMonitor.scale = 1;
            fatigueMonitor.scale = GameManager.GetExperienceModeManagerComponent().GetFatigueRateScale();
        }

        [HideFromIl2Cpp]
        private void UpdateStatMonitors(float elapsedGameSeconds)
        {
            thirstMonitor.Update(GameManager.GetThirstComponent().m_CurrentThirst, elapsedGameSeconds);
            fatigueMonitor.Update(GameManager.GetFatigueComponent().m_CurrentFatigue, elapsedGameSeconds);
        }
    }

    [HarmonyPatch(typeof(SaveGameSystem), "RestoreGlobalData")]//exists <================================================save system
    internal class SaveGameSystem_RestoreGlobalData
    {
        public static void Postfix(string name)
        {
            string serializedProxy = SaveGameSlots.LoadDataFromSlot(name, "ModHealthManager");
            //SaveProxy proxy = Utils.DeserializeObject<SaveProxy>(serializedProxy);
            SaveProxy proxy = new SaveProxy();
            if (!string.IsNullOrEmpty(serializedProxy))
            {
                proxy = MelonLoader.TinyJSON.JSON.Load(serializedProxy).Make<SaveProxy>();
            }
            ModHealthManager.SetData(GetData(proxy));
        }

        private static ModHealthManagerData GetData(SaveProxy proxy)
        {
            if (proxy == null || string.IsNullOrEmpty(proxy.data))
            {
                return null;
            }

            //return Utils.DeserializeObject<ModHealthManagerData>(proxy.data);
            return MelonLoader.TinyJSON.JSON.Load(proxy.data).Make<ModHealthManagerData>();
        }
    }

    [HarmonyPatch(typeof(SaveGameSystem), "SaveGlobalData")]//Exists
    internal class SaveGameSystem_SaveGlobalData
    {
        public static void Postfix(SaveSlotType gameMode, string name)
        {
            SaveProxy proxy = new SaveProxy();
            //proxy.data = Utils.SerializeObject(ModHealthManager.GetData()); //<=============================================================================
            proxy.data = MelonLoader.TinyJSON.JSON.Dump(ModHealthManager.GetData());
            //added inheritance to Il2CppSystem.Object ; should fix the serialization issue
            //SaveGameSlots.SaveDataToSlot(gameMode, SaveGameSystem.m_CurrentEpisode, SaveGameSystem.m_CurrentGameId, name, "ModHealthManager", Utils.SerializeObject(proxy));//<=========
            SaveGameSlots.SaveDataToSlot(gameMode, SaveGameSystem.m_CurrentEpisode, SaveGameSystem.m_CurrentGameId, name, "ModHealthManager", MelonLoader.TinyJSON.JSON.Dump(proxy));
        }
    }

    internal class StatMonitor //Needs declared
    {
        public bool debug;
        public float hourlyBaseline;
        public float hourlyChange;
        public float offset;
        public float scale;
        public float value;

        public float getRateOfChange()
        {
            float result;
            if (ModUtils.AlmostZero(hourlyChange))
            {
                result = 0;
            }
            else if (hourlyBaseline > 0)
            {
                result = Mathf.Min(hourlyChange / hourlyBaseline, 1) + Mathf.Max(0, hourlyChange - hourlyBaseline) * scale;
            }
            else
            {
                result = hourlyChange * scale;
            }

            return result;
        }

        public void Update(float currentValue, float elapsedGameSeconds)
        {
            float delta = currentValue - value;

            hourlyChange = Mathf.Lerp(hourlyChange, 3600f * delta / elapsedGameSeconds, 0.05f);
            value = currentValue;
        }
    }

    [HarmonyPatch(typeof(StatusBar), "GetRateOfChangeFatigue")]//inlined?
    internal class StatusBarGetRateOfChangeFatigue
    {
        public static bool Prefix(StatusBar __instance, ref float __result)
        {
            var fatigueMonitor = ModHealthManager.GetFatigueMonitor();
            __result = fatigueMonitor.getRateOfChange();

            return false;
        }
    }

    [HarmonyPatch(typeof(StatusBar), "GetRateOfChangeThirst")]//inlined?
    internal class StatusBarGetRateOfChangeThirst
    {
        public static bool Prefix(StatusBar __instance, ref float __result)
        {
            var thirstMonitor = ModHealthManager.GetThirstMonitor();
            __result = thirstMonitor.getRateOfChange();

            return false;
        }
    }
}
