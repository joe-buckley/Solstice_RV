using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

//bug sun shines through old spences


namespace Solstice_RV
{
    [HarmonyPatch(typeof(GameManager), "SetAudioModeForLoadedScene")]
    internal class GameManager_SetAudioModeForLoadedScene
    {
        internal static void Postfix()
        {
            if (!(GameManager.m_ActiveScene == "MainMenu"))
            {
                Solstice_RV.scene_loading = false;
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), "LoadSceneWithLoadingScreen")]
    internal class GameManager_LoadSceneWithLoadingScreen
    {
        internal static void Postfix()
        {
            Solstice_RV.scene_loading = true;
        }
    }

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
            if (!Solstice_RV.isEnabled)
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

                debugtext = Solstice_RV.gggtime(Solstice_RV.unitime()) + " " + Enum.GetNames(typeof(TODBlendState))[(int)todBlendState];

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

                //if ((Time.frameCount % 1600) == 0) Debug.Log(debugtext);

                if (isIndoors)
                {
                    __instance.m_TodState.SetIndoors();
                }
                else
                {
                    UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading = GameManager.GetCameraEffects().ColorGrading();
                    if (colorGrading != null)
                    {
                        //if ((Time.frameCount % 50) == 0) Debug.Log("[A"+ (int)todBlendState + "][A"+ ((int)todBlendState + 1) % 7 + "[B" + (int)todBlendState + "][B" + ((int)todBlendState + 1) % 7 + "],"+ newtodBlendFrac + ","+ weatherBlendFrac);
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
            if (!Solstice_RV.isEnabled)
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
        private static bool Prefix(ActionsToBlock actionBeingChecked, ref bool __result)
        {
            if (!GameManager.GetPlayerManagerComponent().m_ActionsToBlockInDarkness.Contains(actionBeingChecked))
            {
                __result = false;
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
                            __result = false;
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
                            __result = false;
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
                            __result = false;
                            return false;
                        }
                    }
                }
            }
            if (MissionIlluminationArea.IsInIlluminationArea(GameManager.GetVpFPSCamera().transform.position))
            {
                __result = false;
                return false;
            }
            if (GameManager.GetWeatherComponent().IsIndoorScene() && GameManager.GetUniStorm().IsNightOrNightBlend() && !ThreeDaysOfNight.IsActive())
            {
                __result = true;
                return false;
            }
            bool flag = GameManager.GetWeatherComponent().IsDenseFog() || GameManager.GetWeatherComponent().IsBlizzard();
            __result = !GameManager.GetWeatherComponent().IsIndoorScene() && GameManager.GetUniStorm().IsNightOrNightBlend() && flag;
            return false;
        }
    }



    [HarmonyPatch(typeof(Weather), "CalculateCurrentTemperature")]
    internal class Weather_CalculateCurrentTemperature
    {
        private static bool Prefix(Weather __instance, ref float ___m_CurrentTemperature, float ___m_CurrentBlizzardDegreesDrop, float ___m_ArtificalTempIncrease, ref float ___m_CurrentTemperatureWithoutHeatSources, float ___m_LockedAirTemperature)
        {
            if (Solstice_RV.scene_loading) return false;
            if (Solstice_RV.lastTemperatureUpdate == -100f)
            {
                Solstice_RV.lastTemperatureUpdate = -90f;
                return false;
            }
            if (Solstice_RV.lastTemperatureUpdate == -90f)
            {
                Solstice_RV.lastTemperatureUpdate = Solstice_RV.unitime();
                return false;
            }
            WeatherStage curWeather = GameManager.GetWeatherComponent().GetWeatherStage();

            float ins = Solstice_RV.InsulationFactor(curWeather);
            float mins = (Solstice_RV.unitime() - Solstice_RV.lastTemperatureUpdate) * 24 * 60;

            Solstice_RV.lastTemperatureUpdate = Solstice_RV.unitime();


            Solstice_RV.airmassChangePerMin = ___m_CurrentBlizzardDegreesDrop * -0.01f;//yeah lets not hit this inside as temporary 10 deg drop -> -.2 deg/min or 12 deg/hour
            if (___m_CurrentBlizzardDegreesDrop > Solstice_RV.lastBlizDrop) Solstice_RV.airmassChangePerMin *= 15;
            Solstice_RV.lastBlizDrop = ___m_CurrentBlizzardDegreesDrop;

            if (curWeather == WeatherStage.DenseFog) Solstice_RV.airmassChangePerMin = 10f * 0.015f;
            if (curWeather == WeatherStage.LightFog) Solstice_RV.airmassChangePerMin = 10f * 0.001f;
            if (curWeather == WeatherStage.LightSnow) Solstice_RV.airmassChangePerMin = 10f * 0.008f;

            //   Debug.Log("Before Update:" + Solstice_RV.groundTemp + ":" + Solstice_RV.outsideTemp+" mins:"+mins+" unitime"+ Solstice_RV.unitime());
            Solstice_RV.updateTemps(Solstice_RV.unitime(), mins, Solstice_RV.airmassChangePerMin, ins, ref Solstice_RV.groundTemp, ref Solstice_RV.outsideTemp);
            //   Debug.Log("After Update:" + Solstice_RV.groundTemp + ":" + Solstice_RV.outsideTemp + " mins:" + mins);

            float alt_correction = (GameManager.GetPlayerTransform().position.y + Solstice_RV.GetBaseHeight(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)) / -100f;
            float ramp_correction = Solstice_RV.TemperatureOffset;
            float adjusted_outside_temp = Solstice_RV.outsideTemp + alt_correction + ramp_correction;

            //Here is where we apportion indoor temp so all outdoor adjustments should be made before this
            bool flag = false;
            if (__instance.IsIndoorEnvironment())
            {
                flag = (!GameManager.GetPlayerManagerComponent().m_IndoorSpaceTrigger || !GameManager.GetPlayerManagerComponent().m_IndoorSpaceTrigger.m_UseOutdoorTemperature);
            }
            if (flag)
            {
                //indoor space
                adjusted_outside_temp += Solstice_RV.lastoutsideAltitudeAdjustment;
                ___m_CurrentTemperature = (Solstice_RV.groundTemp + adjusted_outside_temp + __instance.m_IndoorTemperatureCelsius) / 3;

                if (ThreeDaysOfNight.IsActive() && ThreeDaysOfNight.GetCurrentDayNumber() == 4)
                {
                    ___m_CurrentTemperature = ThreeDaysOfNight.GetBaselineAirTempIndoors();
                }
            }
            else if (GameManager.GetPlayerManagerComponent().m_IndoorSpaceTrigger)
            {
                //back of cave
                ___m_CurrentTemperature = (Solstice_RV.groundTemp * 2 + adjusted_outside_temp) / 3;// + GameManager.GetPlayerManagerComponent().m_IndoorSpaceTrigger.m_TemperatureDeltaCelsius/4;
            }
            else
            {
                //We are outside outside  
                ___m_CurrentTemperature = adjusted_outside_temp;
                Solstice_RV.lastoutsideAltitudeAdjustment = alt_correction;
                //altitude correction
            }


            if (GameManager.GetSnowShelterManager().PlayerInNonRuinedShelter())
            {
                ___m_CurrentTemperature += GameManager.GetSnowShelterManager().GetTemperatureIncreaseCelsius();
            }

            if (GameManager.GetPlayerInVehicle().IsInside())
            {
                ___m_CurrentTemperature += GameManager.GetPlayerInVehicle().GetTempIncrease();
            }

            if (!__instance.IsIndoorEnvironment())
            {
                float numDays = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused() / 24f;
                ___m_CurrentTemperature -= GameManager.GetExperienceModeManagerComponent().GetOutdoorTempDropCelcius(numDays);
            }

            ___m_CurrentTemperature += ___m_ArtificalTempIncrease;
            ___m_CurrentTemperature += Solstice_RV.playerSunBuff();
            ___m_CurrentTemperature += (float)GameManager.GetFeatColdFusion().GetTemperatureCelsiusBonus();

            ___m_CurrentTemperatureWithoutHeatSources = ___m_CurrentTemperature;
            ___m_CurrentTemperature += GameManager.GetHeatSourceManagerComponent().GetTemperatureIncrease();



            if (___m_LockedAirTemperature > -1000f)
            {
                ___m_CurrentTemperature = ___m_LockedAirTemperature;
            }
            return false;
        }
    }

    /*
        public void Update()  ---HUDManager
        {
            if (CameraFade.GetFadeAlpha() > 0f)
            {
                InterfaceManager.m_Panel_HUD.Enable(true);
            }
            else if (InterfaceManager.m_Panel_MainMenu.IsEnabled() || !this.CanEnableHud())
            {
                InterfaceManager.m_Panel_HUD.Enable(false);
            }
            else
            {
                InterfaceManager.m_Panel_HUD.Enable(true);
            }
            if (!this.CanEnableHud())
            {
                return;
            }
            InterfaceManager.m_Panel_HUD.SetHudDisplayMode(HUDManager.m_HudDisplayMode);
            this.UpdateDebugLines();
            this.UpdateCrosshair();
            this.MaybeShowLocationReveal();
        }

    */

    [HarmonyPatch(typeof(Weather), "GetDebugWeatherText")]
    internal class Weather_GetDebugWeatherText
    {
        private static bool Prefix(ref string __result)//, float ___m_TempLow, float ___m_TempHigh)
        {
            TODBlendState todblendState = GameManager.GetUniStorm().GetTODBlendState();
            if ((Time.frameCount % 1600) == 0) Debug.Log(" ");
            float num = GameManager.GetUniStorm().GetTODBlendPercent(todblendState) * 100f;
            string text = uConsoleLog.GetLine(uConsoleLog.GetNumLines() - 3);
            text += "\n" + uConsoleLog.GetLine(uConsoleLog.GetNumLines() - 2);
            text += "\n" + uConsoleLog.GetLine(uConsoleLog.GetNumLines() - 1);



            string nameForScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string nameForScene2 = Utils.GetHardcodedRegionForLocation(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

            text += "\n\n" + nameForScene + " (" + nameForScene2+")";
            text += "\n" + Enum.GetName(typeof(TODBlendState), todblendState) + "(" + num.ToString("F2") + "%)";

            text += GameManager.GetWeatherTransitionComponent().GetDebugString();

            Wind windComponent = GameManager.GetWindComponent();
            text += "\nWind Actual Speed: " + windComponent.GetSpeedMPH().ToString("F1");
            /* string text2 = text;
             text = string.Concat(new string[]
             {
         text2,
         "\nWind Speed Base: ",
         windComponent.GetSpeedMPH_Base().ToString("F1"),
         " MPH. Target Wind Speed: ",
         windComponent.GetTargetSpeedMPH().ToString("F1"),
         " MPH. Angle Base: ",
         windComponent.GetWindAngle_Base().ToString("F1")
             });
             text2 = text;
             text = string.Concat(new string[]
             {
         text2,
         "\nWind Actual Speed: ",
         windComponent.GetSpeedMPH().ToString("F1"),
         " MPH. Actual Angle: ",
         windComponent.GetWindAngle().ToString("F1")
             });
             text = text + "\nPlayer Speed: " + GameManager.GetVpFPSPlayer().Controller.Velocity.magnitude;
             text = text + "\nPlayer Wind Angle: " + windComponent.GetWindAngleRelativeToPlayer().ToString("F1");
             text = text + "\nWwise WindIntensity: " + windComponent.m_LastWindIntensityBlendSentToWise.ToString("F0");
             text = text + "\nWwise GustStrength: " + windComponent.m_LastWindGustStrengthSentToWise.ToString("F0");
             */
            text = text + "\nLocal snow depth: " + string.Format("{0:0.09}", GameManager.GetSnowPatchManager().GetLocalSnowDepth());
            //text = text + "\nAurora alpha: " + GameManager.GetAuroraManager().GetNormalizedAlpha();
            if (GameManager.GetAuroraManager().IsFullyActive())
            {
                text = text + "\nAurora fully active. electrolyzer: " + GameManager.GetAuroraManager().GetAuroraElectrolyzerFadeRatio();
            }
            if (WeatherTransition.m_SuppressBlizzards)
            {
                text += "\nBLIZZARDS ARE SUPPRESSED";
            }
            /*if (FlyOver.GetCurrentFormation())
            {
                float y = FlyOver.GetCurrentFormation().transform.position.y;
                float num2 = Vector3.Magnitude(FlyOver.GetCurrentFormation().transform.position - GameManager.GetPlayerTransform().position);
                text2 = text;
                text = string.Concat(new string[]
                {
            text2,
            "\nFlyOver: height ",
            y.ToString("F0"),
            " distance ",
            num2.ToString("F0"),
            " angle ",
            FlyOver.m_DebugAngle.ToString("F2")
                });
            }
            */

            float tempRamp = Solstice_RV.TemperatureOffset;

            Weather theweather = GameManager.GetWeatherComponent();
            float indoor = 0;


            float outs = Solstice_RV.outsideTemp;
            float gnd = Solstice_RV.groundTemp;
            float fire = GameManager.GetHeatSourceManagerComponent().GetTemperatureIncrease();
            float height = GameManager.GetPlayerTransform().position.y + Solstice_RV.GetBaseHeight(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            float alt = height / -100f;
            float curTemp = theweather.GetCurrentTemperature();
            float inside = -1000.0f;
            float indoor_tempfinal = -1000;
            float final_outside = outs + alt + tempRamp;
            string instr = "";

            float missing = curTemp - (final_outside + indoor + Solstice_RV.playerSunBuff() + fire);
            if (theweather.IsIndoorEnvironment() && !(GameManager.GetPlayerManagerComponent().m_IndoorSpaceTrigger && GameManager.GetPlayerManagerComponent().m_IndoorSpaceTrigger.m_UseOutdoorTemperature))
            {
                inside = theweather.m_IndoorTemperatureCelsius;
                final_outside += Solstice_RV.lastoutsideAltitudeAdjustment;
                indoor_tempfinal = (final_outside + inside + gnd) / 3;
                instr = " Ins :" + string.Format("{0:0.0}", inside) + " Fin:" + string.Format("{0:0.0}", indoor_tempfinal);
                missing = curTemp - (indoor_tempfinal + fire);
            }
            else if (GameManager.GetPlayerManagerComponent().m_IndoorSpaceTrigger)
            {
                indoor = GameManager.GetPlayerManagerComponent().m_IndoorSpaceTrigger.m_TemperatureDeltaCelsius;
                indoor_tempfinal = ((final_outside+alt) + gnd * 2) / 3;
                instr = " Fin:" + string.Format("{0:0.0}", indoor_tempfinal);
                missing = curTemp - (indoor_tempfinal + fire);
            }


            //float blizdrop = (float)AccessTools.Field(typeof(Weather), "m_CurrentBlizzardDegreesDrop").GetValue(theweather);

            text += "\nGnd:" + string.Format("{0:0.0}", gnd);
            text += " Air:" + string.Format("{0:0.0}", outs);
            text += " Alt:" + string.Format("{0:0.0}", alt);
            text += " Ramp:" + string.Format("{0:0.0}", tempRamp);
            text += " Outs:" + string.Format("{0:0.0}", final_outside);
            text += instr;

            text += "\nMss:" + string.Format("{0:0.00}", Solstice_RV.airmassChangePerMin);
            text += " SunB:" + string.Format("{0:0.0}", Solstice_RV.playerSunBuff());
            text += " Fire" + string.Format("{0:0.0}", fire);

            float TempNoFire = GameManager.GetWeatherComponent().GetCurrentTemperatureWithoutHeatSources();
            text += "\nTNoF: " + string.Format("{0:0.0}",TempNoFire);
            text += " Miss:" + string.Format("{0:0.0}",(curTemp -( TempNoFire +fire) ));
            text += "\nTemp:" + string.Format("{0:0.0}", curTemp);

            text += " Miss:" + string.Format("{0:0.0}", missing);
            text += "\n";

            __result = text;

            return false;
        }
    }

}

        /*
        [HarmonyPatch(typeof(Panel_Inventory_Examine), "MaybeAbortReadingWithHUDMessage")]
        internal class Panel_Inventory_Examine_MaybeAbortReadingWithHUDMessage
        {
            private static bool Prefix(Weather __instance, ref bool __result)
            {
                Debug.Log("gothere:"+GameManager.GetWeatherComponent().IsTooDarkForAction(ActionsToBlock.Reading));
                if (GameManager.GetWeatherComponent().IsTooDarkForAction(ActionsToBlock.Reading))
                {
                    Debug.Log("gotheretoo");
                    HUDMessage.AddMessage(Localization.Get("GAMEPLAY_TooDarkToRead"), false);
                    __result = true;
                    return false;
                }
                if (GameManager.GetFatigueComponent().IsExhausted())
                {
                    HUDMessage.AddMessage(Localization.Get("GAMEPLAY_TooTiredToRead"), false);
                    __result = true;
                    return false;
                }
                if (GameManager.GetFreezingComponent().IsFreezing())
                {
                    HUDMessage.AddMessage(Localization.Get("GAMEPLAY_TooColdToRead"), false);
                    __result = true;
                    return false;
                }
                if (GameManager.GetHungerComponent().IsStarving())
                {
                    HUDMessage.AddMessage(Localization.Get("GAMEPLAY_TooHungryToRead"), false);
                    __result = true;
                    return false;
                }
                if (GameManager.GetThirstComponent().IsDehydrated())
                {
                    HUDMessage.AddMessage(Localization.Get("GAMEPLAY_TooThirstyToRead"), false);
                    __result = true;
                    return false;
                }
                if (GameManager.GetConditionComponent().GetNormalizedCondition() < 0.1f)
                {
                    HUDMessage.AddMessage(Localization.Get("GAMEPLAY_TooWoundedToRead"), false);
                    __result = true;
                    return false;
                }
                if (GameManager.GetConditionComponent().HasNonRiskAffliction())
                {
                    HUDMessage.AddMessage(Localization.Get("GAMEPLAY_CannotReadWithAfflictions"), false);
                    __result = true;
                    return false;
                }
                Debug.Log("eek returning false");
                __result = false;
                return false;
            }

        }

        */
    



