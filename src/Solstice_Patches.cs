using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;




namespace Solstice_RV
{


    [HarmonyPatch(typeof(SaveGameSystem), "RestoreGlobalData")]
    internal class SaveGameSystemPatch_RestoreGlobalData
    {
        internal static void Postfix(string name)
        {
            Solstice_RV.LoadData(name);
        }
    }

    [HarmonyPatch(typeof(SaveGameSystem), "SaveGlobalData")]
    internal class SaveGameSystemPatch_SaveGlobalData
    {
        public static void Postfix(SaveSlotType gameMode, string name)
        {
            Solstice_RV.SaveData(gameMode, name);
        }
    }

    [HarmonyPatch(typeof(StatsManager), "Reset")]
    internal class StatsManager_Reset
    {
        internal static void Postfix()
        {
            if (!GameManager.InCustomMode())
            {
                Solstice_RV.Disable();
            }
        }
    }

    [HarmonyPatch(typeof(TimeWidget), "Start")]
    internal class TimeWidget_Start
    {
        internal static void Postfix(TimeWidget __instance)
        {
            TimeWidgetUpdater.Initialize(__instance);
        }
    }

    [HarmonyPatch(typeof(TimeWidget), "Update")]
    internal class TimeWidget_Update
    {
        private static float nextUpdate;

        internal static bool Prefix()
        {
            if (Time.unscaledTime > nextUpdate)
            {
                nextUpdate = Time.unscaledTime + 0.1f;
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(TimeWidget), "UpdateIconPositions")]
    internal class TimeWidget_UpdateIconPositions
    {
        internal static bool Prefix(TimeWidget __instance, float angleDegrees)
        {
            if (!Solstice_RV.Enabled)
            {
                return true;
            }

            TimeWidgetUpdater.Update(__instance, angleDegrees);
            return false;
        }
    }

    [HarmonyPatch(typeof(UniStormWeatherSystem), "Init")]
    internal class UniStormWeatherSystem_Init
    {
        internal static void Postfix(UniStormWeatherSystem __instance)
        {
            Solstice_RV.Init(__instance);
        }
    }

    [HarmonyPatch(typeof(UniStormWeatherSystem), "SetMoonPhase")]
    internal class UniStormWeatherSystem_SetMoonPhase
    {
        internal static void Prefix(UniStormWeatherSystem __instance)
        {
            Solstice_RV.Update();
        }
    }

    [HarmonyPatch(typeof(TODStateConfig), "SetBlended")]
    internal class TODStateConfig_SetBlended
    {
        internal static void Postfix(TODStateConfig __instance, TODStateConfig a, TODStateConfig b, float blend, float blendBiased, int nightStates)
        {
            //undo Wulf's night sky bloom intensity change because its breaks the dawn transition.
            if (Solstice_RV.better_night_sky_installed && (nightStates > 0)) __instance.m_SkyBloomIntensity /= 0.3f;

        }

    }
        [HarmonyPatch(typeof(ActiveEnvironment), "Refresh")]
    internal class ActiveEnvironment_Refresh
    {
        internal static bool Prefix(ActiveEnvironment __instance, WeatherStateConfig wsA, WeatherStateConfig wsB, float weatherBlendFrac, TODBlendState todBlendState, float todBlendFrac, float todBlendBiased, bool isIndoors)
        {
            //   if ((Time.frameCount % 50) == 0)
            {

                ColorGradingSettings settings1A = null;
                ColorGradingSettings settings1B = null;
                ColorGradingSettings settings2A = null;
                ColorGradingSettings settings2B = null;

                Traverse traverseA = Traverse.Create(__instance).Field("m_WorkA");
                TODStateConfig m_WorkA = traverseA.GetValue<TODStateConfig>();
                Traverse traverseB = Traverse.Create(__instance).Field("m_WorkB");
                TODStateConfig m_WorkB = traverseB.GetValue<TODStateConfig>();

                TODStateConfig[] wsA_TodStates = { wsA.m_NightColors, wsA.m_DawnColors, wsA.m_MorningColors, wsA.m_MiddayColors, wsA.m_AfternoonColors, wsA.m_DuskColors, wsA.m_NightColors, wsA.m_NightColors };
                TODStateConfig[] wsB_TodStates = { wsB.m_NightColors, wsB.m_DawnColors, wsB.m_MorningColors, wsB.m_MiddayColors, wsB.m_AfternoonColors, wsB.m_DuskColors, wsB.m_NightColors, wsA.m_NightColors };

                bool flagtoAdd = false;

                int[] nightstates = { 1, 0, 0, 0, 0, 2, 3 };

                float[] keyangles = { Solstice_RV.dawndusk_angle, Solstice_RV.riseset_angle, Solstice_RV.mornaft_angle, Solstice_RV.getSunAngleAtUnitime(Mathf.Floor(Solstice_RV.unitime()) + 0.5f), Solstice_RV.mornaft_angle, Solstice_RV.riseset_angle, Solstice_RV.dawndusk_angle, Solstice_RV.getSunAngleAtUnitime(Mathf.Floor(Solstice_RV.unitime())) };

                if (GameManager.GetUniStorm().m_NormalizedTime > 1f)
                {
                    GameManager.GetUniStorm().SetNormalizedTime(GameManager.GetUniStorm().m_NormalizedTime - 1f);
                    flagtoAdd = true;
                }


                todBlendState = GameManager.GetUniStorm().GetTODBlendState();
                //   Debug.Log(todBlendState);


                int itod = (int)todBlendState;



                string debugtext;

                float zenith = Solstice_RV.getSunAngleAtUnitime(Mathf.Floor(Solstice_RV.unitime()) + 0.5f);
                float nadir = Solstice_RV.getSunAngleAtUnitime(Mathf.Floor(Solstice_RV.unitime()) + 0.0f);

                float throughpcntzen;
                float throughpcntnad;

                TODStateConfig wsAStartblend;
                TODStateConfig wsAEndblend;
                TODStateConfig wsBStartblend;
                TODStateConfig wsBEndblend;

                TODStateConfig wsAZenblend;
                TODStateConfig wsANadblend;
                TODStateConfig wsBZenblend;
                TODStateConfig wsBNadblend;

                wsAStartblend = wsA_TodStates[itod];
                wsAEndblend = wsA_TodStates[itod + 1];
                wsBStartblend = wsB_TodStates[itod];
                wsBEndblend = wsB_TodStates[itod + 1];

                String namestartblend = Enum.GetNames(typeof(TODBlendState))[(int)todBlendState];
                String nameendblend = Enum.GetNames(typeof(TODBlendState))[((int)todBlendState + 1) % 6];

                float st_ang = keyangles[itod];
                float en_ang = keyangles[itod + 1];

                throughpcntzen = (zenith - st_ang) / (en_ang - st_ang);

                debugtext = (int)(GameManager.GetUniStorm().GetTODBlendState()) + ":" + Solstice_RV.unitime() + Enum.GetNames(typeof(TODBlendState))[(int)todBlendState] + " FC:" + Time.frameCount;

                // do we need a new zen state 
                if (throughpcntzen >= 0 && throughpcntzen <= 1)
                {

                    if (itod == (int)TODBlendState.MorningToMidday)// need to correct against game zenith of 45
                    {
                        throughpcntzen = Mathf.Clamp((zenith - st_ang) / (45f - st_ang), 0, 1);
                    }
                    if (itod == (int)TODBlendState.MiddayToAfternoon)// need to correct against game zenith of 45
                    {
                        throughpcntzen = Mathf.Clamp((zenith - 45f) / (en_ang - 45f), 0, 1);
                    }
                    //debugtext += " mc:" + throughpcntzen;
                    wsAZenblend = Solstice_RV.createNewMidPoint(wsA_TodStates[itod], wsA_TodStates[itod + 1], wsA_TodStates[6 - itod], wsA_TodStates[((5 - itod) % 8 + 8) % 8], throughpcntzen);
                    wsBZenblend = Solstice_RV.createNewMidPoint(wsB_TodStates[itod], wsB_TodStates[itod + 1], wsB_TodStates[6 - itod], wsB_TodStates[((5 - itod) % 8 + 8) % 8], throughpcntzen);
                    if (itod < 3)
                    {
                        en_ang = zenith;
                        nameendblend = "Zen";
                        wsAEndblend = wsAZenblend;
                        wsBEndblend = wsBZenblend;

                    }
                    else
                    {
                        st_ang = zenith;
                        namestartblend = "Zen";
                        wsAStartblend = wsAZenblend;
                        wsBStartblend = wsBZenblend;
                    }
                }

                throughpcntnad = (nadir - st_ang) / (en_ang - st_ang);

                if (throughpcntnad >= 0 && throughpcntnad <= 1)
                {

                    debugtext += " mc:" + throughpcntnad + "[" + itod + "][" + (itod + 1) + "][" + (6 - itod) + "][" + ((5 - itod) % 8 + 8) % 8 + "]";

                    wsANadblend = Solstice_RV.createNewMidPoint(wsA_TodStates[itod], wsA_TodStates[itod + 1], wsA_TodStates[6 - itod], wsA_TodStates[((5 - itod) % 8 + 8) % 8], throughpcntnad);
                    wsBNadblend = Solstice_RV.createNewMidPoint(wsB_TodStates[itod], wsB_TodStates[itod + 1], wsB_TodStates[6 - itod], wsB_TodStates[((5 - itod) % 8 + 8) % 8], throughpcntnad);

                    if (itod > 3)
                    {
                        en_ang = nadir;
                        nameendblend = "Nad";
                        wsAEndblend = wsANadblend;
                        wsBEndblend = wsBNadblend;

                    }
                    else
                    {
                        st_ang = nadir;
                        namestartblend = "Nad";
                        wsAStartblend = wsANadblend;
                        wsBStartblend = wsBNadblend;
                    }
                }



                float newtodBlendFrac = (Solstice_RV.getSunAngleAtUnitime(Solstice_RV.unitime()) - st_ang) / (en_ang - st_ang);
                debugtext += "(" + namestartblend + ":" + nameendblend + ")";
                debugtext += " s:" + st_ang + " e:" + en_ang + " c:" + string.Format("{0:0.00}", Solstice_RV.getSunAngleAtUnitime(Solstice_RV.unitime())) + " =:" + string.Format("{0:0.00}", newtodBlendFrac);

                m_WorkA.SetBlended(wsAStartblend, wsAEndblend, newtodBlendFrac, newtodBlendFrac, nightstates[itod]);
                m_WorkB.SetBlended(wsBStartblend, wsBEndblend, newtodBlendFrac, newtodBlendFrac, nightstates[itod]);

                settings1A = wsA_TodStates[(int)todBlendState].m_ColorGradingSettings;
                settings1B = wsA_TodStates[((int)todBlendState + 1) % 7].m_ColorGradingSettings;
                settings2A = wsB_TodStates[(int)todBlendState].m_ColorGradingSettings;
                settings2B = wsB_TodStates[((int)todBlendState + 1) % 7].m_ColorGradingSettings;

                __instance.m_TodState.SetBlended(m_WorkA, m_WorkB, weatherBlendFrac, weatherBlendFrac, 0);

                if ((Time.frameCount % 500) == 0) Debug.Log(debugtext);
               
                if (isIndoors)
                {
                    __instance.m_TodState.SetIndoors();
                }
                else
                {
                    UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading = GameManager.GetCameraEffects().ColorGrading();
                    if (colorGrading != null)
                    {
                        if ((Time.frameCount % 50) == 0) Debug.Log("[A"+ (int)todBlendState + "][A"+ ((int)todBlendState + 1) % 7 + "[B" + (int)todBlendState + "][B" + ((int)todBlendState + 1) % 7 + "],"+ newtodBlendFrac + ","+ weatherBlendFrac);
                        colorGrading.UpdateLutForTimeOfDay(settings1A, settings1B, settings2A, settings2B, newtodBlendFrac, newtodBlendFrac, weatherBlendFrac);
                        //colorGrading.UpdateLutForTimeOfDay(wsA.m_DawnColors.m_ColorGradingSettings, wsA.m_MorningColors.m_ColorGradingSettings, wsB.m_DawnColors.m_ColorGradingSettings, wsB.m_MorningColors.m_ColorGradingSettings, 0.5f,0.5f, 0.5f);
                    }
                }
                __instance.m_GrassTintScalar = Mathf.Lerp(wsA.m_GrassTintScalar, wsB.m_GrassTintScalar, weatherBlendFrac);

                if (flagtoAdd)
                {
                    GameManager.GetUniStorm().SetNormalizedTime(GameManager.GetUniStorm().m_NormalizedTime + 1f);
                }

            
                return false;
            }
        }


    }


    [HarmonyPatch(typeof(UniStormWeatherSystem), "Update")]
    internal class UniStormWeatherSystem_Update
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call)
                {
                    continue;
                }

                MethodInfo methodInfo = codes[i].operand as MethodInfo;
                if (methodInfo != null && methodInfo.Name == "UpdateSunTransform")
                {
                    codes[i - 1].opcode = OpCodes.Nop;
                    codes[i].opcode = OpCodes.Nop;
                    break;
                }
            }

            return codes;
        }
    }


    [HarmonyPatch(typeof(UniStormWeatherSystem), "UpdateSunTransform")]
    internal class UniStormWeatherSystem_UpdateSunTransform
    {

        internal static bool Prefix(UniStormWeatherSystem __instance)
        {
            if (!Solstice_RV.Enabled)
            {
                return true;
            }

            float lat = Solstice_RV.Latitude;

            float sunAngle = __instance.m_SunAngle;


            Transform transform = __instance.m_SunLight.transform;
            Vector3 suntoearth = new Vector3(0, 0, 1f);
            float zenith = sunAngle;
            suntoearth = Quaternion.Euler(zenith, 0, 0) * suntoearth;

            transform.forward = suntoearth;

            Vector3 earth_axis = new Vector3(0, 0, 1);

            earth_axis = Quaternion.Euler(-lat, 0, 0) * earth_axis;

            transform.Rotate(earth_axis, __instance.m_NormalizedTime * 360f - 180f, Space.World);


            return false;
        }
    }


    [HarmonyPatch(typeof(Weather), "GenerateTempHigh")]
    internal class Weather_GenerateTempHigh
    {
        internal static void Postfix(Weather __instance)
        {
            if (!Solstice_RV.Enabled)
            {
                return;
            }

            Traverse traverse = Traverse.Create(__instance).Field("m_TempHigh");
            float tempHigh = traverse.GetValue<float>();
            traverse.SetValue(tempHigh + Solstice_RV.TemperatureOffset);
        }
    }

    [HarmonyPatch(typeof(Weather), "GenerateTempLow")]
    internal class Weather_GenerateTempLow
    {
        internal static void Postfix(Weather __instance)
        {
            if (!Solstice_RV.Enabled)
            {
                return;
            }

            Traverse traverse = Traverse.Create(__instance).Field("m_TempLow");
            float tempLow = traverse.GetValue<float>();
            traverse.SetValue(tempLow + Solstice_RV.TemperatureOffset);
        }
    }


    [HarmonyPatch(typeof(Panel_FirstAid), "Start")]
    internal class AddSeasonInfo
    {
        private static void Postfix(Panel_FirstAid __instance)
        {
            GameObject newGameObject = NGUITools.AddChild(__instance.m_TimeWidgetPos, __instance.m_ColdStatusLabel.gameObject);
            newGameObject.transform.localPosition = new Vector3(-45, -22, 0);
            Solstice_RV.attachSeasonLabel(newGameObject);
        }
    }


    //fix to set this to exclude night blends as well.
    [HarmonyPatch(typeof(Weather), "IsTooDarkForAction")]

    internal class Weather_IsTooDarkForAction
    {
        private static bool Prefix(ActionsToBlock actionBeingChecked)
        {
            if (!GameManager.GetPlayerManagerComponent().m_ActionsToBlockInDarkness.Contains(actionBeingChecked))
            {
                return false;
            }
            float num = 10f;
            for (int i = 0; i < GearManager.m_Gear.Count; i++)
            {
                if (!(GearManager.m_Gear[i] == null))
                {
                    if (Vector3.Distance(GearManager.m_Gear[i].transform.position, GameManager.GetVpFPSCamera().transform.position) <= num)
                    {
                        if (GearManager.m_Gear[i].IsLitFlare() || GearManager.m_Gear[i].IsLitLamp() || GearManager.m_Gear[i].IsLitMatch() || GearManager.m_Gear[i].IsLitTorch() || GearManager.m_Gear[i].IsLitFlashlight())
                        {
                            return false;
                        }
                    }
                }
            }
            for (int j = 0; j < FireManager.m_Fires.Count; j++)
            {
                if (!(FireManager.m_Fires[j] == null))
                {
                    if (Vector3.Distance(FireManager.m_Fires[j].transform.position, GameManager.GetVpFPSCamera().transform.position) <= num)
                    {
                        if (FireManager.m_Fires[j].IsBurning())
                        {
                            return false;
                        }
                    }
                }
            }
            for (int k = 0; k < AuroraManager.GetAuroraElectrolizerList().Count; k++)
            {
                if (!(AuroraManager.GetAuroraElectrolizerList()[k] == null))
                {
                    if (Vector3.Distance(AuroraManager.GetAuroraElectrolizerList()[k].transform.position, GameManager.GetVpFPSCamera().transform.position) <= num)
                    {
                        if (AuroraManager.GetAuroraElectrolizerList()[k].IsElectrolized())
                        {
                            return false;
                        }
                    }
                }
            }
            if (MissionIlluminationArea.IsInIlluminationArea(GameManager.GetVpFPSCamera().transform.position))
            {
                return false;
            }
            if (GameManager.GetWeatherComponent().IsIndoorScene() && GameManager.GetUniStorm().IsNightOrNightBlend() && !ThreeDaysOfNight.IsActive())
            {
                return true;
            }
            bool flag = GameManager.GetWeatherComponent().IsDenseFog() || GameManager.GetWeatherComponent().IsBlizzard();
            return !GameManager.GetWeatherComponent().IsIndoorScene() && GameManager.GetUniStorm().IsNightOrNightBlend() && flag;
        }
    }

}



