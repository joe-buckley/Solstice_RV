using Harmony;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

//Left to do:
//Fix precession so it uses rotation around an axis
//Test more extreme settings

namespace Solstice_RV
{
    public static class Solstice_RV
    {
        private const string SAVE_FILE_NAME = "solstice-settings";

        private static readonly GUISettings guiSettings = new GUISettings();

        private static readonly Settings settings = new Settings();

        private static string[] months = { "January", "February", "March", "April","May","June","July","August","September","October","November","December" };


        private static Dictionary<string, float> baseHeights = new Dictionary<string, float> {
{"CoastalRegion",-23.1f},
{"CrashMountainRegion",250},
{"CrossroadsRegion",0},
{"Dam",0},
{"DamCaveTransitionZone",0},
{"DamRiverTransitionZoneB",0},
{"DamTransitionZone",0},
{"HighwayTransitionZone",-46.1f},
{"LakeRegion",144},
{"MarshRegion",313.1f},
{"MountainTownRegion",300f},
{"RavineTransitionZone",47.5f},
{"RiverValleyRegion",600f},
{"RuralRegion",200f},
{"TracksRegion",-52.7f},
{"TransitionCHtoDP",0},
{"TransitionCHtoPV",0},
{"TransitionMLtoCH",0},
{"TransitionMLtoPV",0},
{"WhalingStationRegion",-15.8f},
        };

        private static float[] originalKeyframeTimes;

        private static float   originalMasterTimeKeyOffset;

        public static bool isEnabled=true;

        public static float airmassChangePerMin = 0;
        public static float lastBlizDrop = 0;
        public static float outsideTemp=-10f;
        public static float lastoutsideAltitudeAdjustment = 0;
        public static float groundTemp=-10f;
        public static bool scene_loading = true;


        public static float lastTemperatureUpdate = -100;

        public static readonly float dawndusk_angle = -12;
        public static readonly float riseset_angle = 0;
        public static readonly float mornaft_angle = 12;
        
        private static Traverse traverseKeyframeTimes;

        private static float precession = 23f;

        private static GameObject mySeasonLabel;

        internal static int CycleLength
        {
            get => settings.cycleLength;
            private set => settings.cycleLength = value;
        }

        internal static int CycleOffset
        {
            get => settings.cycleOffset;
            private set => settings.cycleOffset = value;
        }

        //internal static bool Enabled
        //    {
        //        get => settings.enabled;
        //         private set => settings.enabled = value;
        //       }

        internal static float playerSunBuff()
        {
            bool indoor_test = GameManager.GetWeatherComponent().IsIndoorScene(); //(!GameManager.GetPlayerManagerComponent().m_IndoorSpaceTrigger || !GameManager.GetPlayerManagerComponent().m_IndoorSpaceTrigger.m_UseOutdoorTemperature);
            if (indoor_test) return 0;

            Transform transform = GameManager.GetUniStorm().m_SunLight.transform;
            //test if in sunlight.
            int layerMask = 1 << 8;
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(GameManager.GetPlayerObject().transform.position, transform.TransformDirection(Vector3.back), out hit, Mathf.Infinity, layerMask))
            {
                //Debug.DrawRay(GameManager.GetPlayerObject().transform.position, transform.TransformDirection(Vector3.back) * hit.distance, Color.yellow);
                return 0;
            }
            else
            {
                // Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
                //Debug.Log("Sunlight_transform y:"+ GameManager.GetUniStorm().m_SunLight.transform.forward.y);
                return getDirectSunWarmth();
            }
        }

        internal static float getDirectSunWarmth()
        {
            float sunangle = GetCurrentNormSunIncidence();
            float sunstrength;
            Weather theWeather = GameManager.GetWeatherComponent();
            switch (theWeather.GetWeatherStage())
            {
                case WeatherStage.Clear:
                    sunstrength = 6;
                    break;
                case WeatherStage.PartlyCloudy:
                    sunstrength = 3f;
                    break;
                case WeatherStage.Cloudy:
                    sunstrength = 0.1f;
                    break;
                case WeatherStage.LightFog:
                    sunstrength = 0.3f;
                    break;
                default:
                    sunstrength = 0;
                    break;
            }
            return sunangle * sunstrength;// the sun forward vector point down towards the player
        }


        internal static float GetBaseHeight(string regname)
        {
            if (!baseHeights.ContainsKey(regname))
            {
                Debug.LogWarningFormat("Scene \"{0}\" has not been associated with a height in baseHeights ID",regname);
                return 0;
            }
            //Debug.Log("Returning " + baseHeights[regname] + " for:" + regname);
            return baseHeights[regname];
        }


internal static float InsulationFactor(WeatherStage curStage)
        {
            float insulation;
            switch (curStage)
            {
                case WeatherStage.Clear:
                    insulation = 0;
                    break;
                case WeatherStage.ClearAurora:
                    insulation = 0;
                    break;
                case WeatherStage.PartlyCloudy:
                    insulation = 0.2f;
                    break;
                case WeatherStage.Cloudy:
                    insulation = 0.9f;
                    break;
                case WeatherStage.LightFog:
                    insulation = 0.2f;
                    break;
                case WeatherStage.DenseFog:
                    insulation = 0.9f;
                    break;
                case WeatherStage.LightSnow:
                    insulation = 0.9f;
                    break;
                case WeatherStage.HeavySnow:
                    insulation = 0.9f;
                    break;
                case WeatherStage.Blizzard:
                    insulation = 0.9f;
                    break;
                default:
                    Debug.Log("Unrecognised Weather State:"+curStage);
                    insulation = 0.5f;
                    break;
            }
            return insulation*5/9; //the models work best when the clouds aren't a perfect blanket set max to 0.5;
        }

        internal static float GetCurrentNormSunIncidence()
        {
            return Mathf.Asin(Mathf.Max(GameManager.GetUniStorm().m_SunLight.transform.forward.y * -1f, 0)) * Mathf.Rad2Deg/90;
        }
  

        internal static float TemperatureOffset
        {
            get; private set;
        }

        internal static float Latitude
        {
            get; private set;
        }

        internal static bool better_night_sky_installed = false;


        public static void OnLoad()
        {
            Log("Version {0}", Assembly.GetExecutingAssembly().GetName().Version);

            guiSettings.AddToCustomModeMenu(ModSettings.Position.AboveAll);

            uConsole.RegisterCommand("solstice-log", SolsticeLog);
        }

        internal static void initLatitude(int LatitudeChoice)
        {
            switch ((Location)LatitudeChoice)
            {
                case Location.FortySix:
                    Latitude = 46;
                    break;
                case Location.FiftyThree:                  
                    Latitude = 53f;
                    break;
                case Location.Sixty:            
                    Latitude = 60f;
                    break;
                case Location.SixtyFive:               
                    Latitude = 65f;
                    break;

                default:             
                    Latitude = 46f;
                    break;    
            }
        }


        internal static void ApplySettings()
        {
            settings.enabled = guiSettings.Enabled;
            isEnabled = settings.enabled;

            settings.cycleLength = 12 * Mathf.FloorToInt(guiSettings.DaysInMonth);

            if ((int)guiSettings.StartMonth == 0) {
                settings.cycleOffset = (int)(Random.value * CycleLength); }
            else
            {
                settings.cycleOffset = ((int)guiSettings.StartMonth - 1) * (int)CycleLength / 12;
            }
 
            settings.Latitude = (int)guiSettings.Location;
            initLatitude(settings.Latitude);

            settings.tempRampStart   = guiSettings.tempRampStart;
            settings.tempRampEnd   = guiSettings.tempRampEnd;
            settings.tempRampDays = guiSettings.tempRampDays;


            lastTemperatureUpdate = -100;

            string outputstring = Solstice_RV.BootstrapTemps();

            Debug.Log(outputstring);

            Debug.Log("Initial Temps:" + Solstice_RV.groundTemp + ":" + Solstice_RV.outsideTemp);

            if (!isEnabled)
            {
                RestoreKeyframeTimes(GameManager.GetUniStorm());
            } 

            Update();
        }

        internal static void Disable()
        {
            if (isEnabled)
            {
                isEnabled = false;
                settings.enabled = false;
                RestoreKeyframeTimes(GameManager.GetUniStorm());
            }
        }
        internal static float getDaysTilt(float unitime)
        {
          //  Debug.Log("GetTilt called with:"+unitime+"returning" + -1.0f * Mathf.Cos(Mathf.PI * 2f * UniSeason(unitime)));
            return  -1.0f * Mathf.Cos(Mathf.PI * 2f * UniSeason(unitime));
        }

 
        internal static void Init(UniStormWeatherSystem uniStormWeatherSystem)
        {
            //used for main menu
            Debug.Log("Init called");
            traverseKeyframeTimes = Traverse.Create(uniStormWeatherSystem).Field("m_TODKeyframeTimes");
            originalMasterTimeKeyOffset = uniStormWeatherSystem.m_MasterTimeKeyOffset;
            originalKeyframeTimes = (float[])traverseKeyframeTimes.GetValue<float[]>().Clone();
            Log("Original mtko"+ originalMasterTimeKeyOffset + " times   [0]:" + originalKeyframeTimes[0] + "; [1]: " + originalKeyframeTimes[1] + "[2]:" + originalKeyframeTimes[2] + "; [3]: " + originalKeyframeTimes[3] + "; [4]: " + originalKeyframeTimes[4] + "; [5]: " + originalKeyframeTimes[5] + "; [6]: " + originalKeyframeTimes[6]);
            Latitude = 45f;
            settings.Latitude = 0;

            string BNSPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Better-Night-Sky.dll");
            better_night_sky_installed = File.Exists(BNSPath);


            settings.cycleOffset = (int)(Random.value * CycleLength);
            GameManager.GetUniStorm().m_SunAngle = getSunAngleAtUnitime(1.5f); 
            GameManager.GetUniStorm().UpdateSunTransform();
            //SolsticeLog();
         
        }

        internal static void updateTemps(float time,float elapsedmins,float airmasschange, float ins, ref float myground,ref float myoutside)
        {

            float sun = settings.sunStrength * Mathf.Max(getSunAngleAtUnitime(time), 0) / 90;
            float abszero = -273f;
            float rad = Mathf.Pow((myoutside - abszero), 4) * (1 / Mathf.Pow(0 - abszero, 4));
            float airgroundexchange = (myoutside - myground) * settings.epsilon;
            float coreTemp = 1f; //not deep core, just somewhere deeper underground
            float coregroundexchange = (coreTemp - myground) * settings.gamma;
           // Debug.Log("sun:" + sun+ " rad:"+ rad+" airground:"+airgroundexchange + " coreground:"+coregroundexchange+" airmassexchage:"+airmasschange+" ins:"+ins);
         //   Debug.Log("myground:" + myground + " myoutside" + myoutside);
            myoutside += ((sun - rad) * (1 - ins) * settings.beta - airgroundexchange + airmasschange) * elapsedmins;
            myground += (coregroundexchange + airgroundexchange / settings.theta) * elapsedmins;
         //   Debug.Log("myground:" + myground + " myoutside" + myoutside);
        }
        internal static string BootstrapTemps()
        {

            float mytemp = outsideTemp;
            float groundtemp = groundTemp;
            float step = 1f / (24f * 4f);
            float simUniTime;
            float winterDayTemp = Mathf.NegativeInfinity;
            float summerDayTemp = Mathf.NegativeInfinity;
            float winterNightTemp = Mathf.NegativeInfinity;
            float summerNightTemp = Mathf.NegativeInfinity;
            float summerNightCount = 0;
            float summerDayCount = 0;
            float winterNightCount = 0;
            float winterDayCount = 0;

            bool clocking;
            bool isWinter;
            bool isSummer;
            bool isDay;

            float curtime;

            for (simUniTime = 1; simUniTime < CycleLength * 20; simUniTime += step)
            {
                float mins = step * 24 * 60;

                updateTemps(simUniTime, mins,0, 0.25f, ref groundtemp, ref mytemp);
                clocking = simUniTime > CycleLength * 19f;
                
                float mySeason = UniSeason(simUniTime);
                isWinter = mySeason>0.875f || mySeason <0.125f;
                isSummer = Mathf.Abs(mySeason - 0.5f) < 0.125f;
                isDay = getSunAngleAtUnitime(simUniTime) > riseset_angle;



                if (clocking) Debug.Log(UniYear(simUniTime) + "\t" + gggtime(simUniTime) + "\t" +isDay+ "\t" + mySeason + "\t" + groundtemp + "\t" + mytemp);

                if (clocking && isWinter && isDay)
                {
                    winterDayTemp = (winterDayTemp == Mathf.NegativeInfinity) ? mytemp : winterDayTemp + mytemp;
                    winterDayCount++;
                }

                if (clocking && isWinter && !isDay)
                {
                    winterNightTemp = (winterNightTemp == Mathf.NegativeInfinity) ? mytemp : winterNightTemp + mytemp;
                    winterNightCount++;
                }
                if (clocking && isSummer && isDay)
                {
                    summerDayTemp = (summerDayTemp == Mathf.NegativeInfinity) ? mytemp : summerDayTemp + mytemp;
                    summerDayCount++;
                }
                if (clocking && isSummer && !isDay)
                {
                    summerNightTemp = (summerNightTemp == Mathf.NegativeInfinity) ? mytemp : summerNightTemp + mytemp;
                    summerNightCount++;
                }



            }

            outsideTemp = mytemp;
            groundTemp = groundtemp;

            winterDayTemp /= winterDayCount;
            winterNightTemp /= winterNightCount;
            summerDayTemp /= summerDayCount;
            summerNightTemp /= summerNightCount;

            Debug.Log("Bootstrapping Temps, sunStrength:"+ settings.sunStrength +" beta:"+ settings.beta +" theta:"+ settings.theta +" epsilon:"+ settings.epsilon);
            Debug.Log("UniYear at end of sim:" + UniYear(simUniTime)+ "Air Temp:"+outsideTemp+"Ground Temp"+groundTemp);
            return "Winter Day:"+winterDayTemp+" Night:"+winterNightTemp+"   Summer Day:"+summerDayTemp+" Night:"+summerNightTemp;
        }
/*
        internal static void BootstrapTemps()
        {
            string times = "";
            string temps = "";
            string suns = "";
            string grnds = "";
            float mytemp = -5f;
            float groundtemp = -10f;
            float logtime = 1;//CycleLength*8;
            float step = 1f / (24f);
            //bool done = false;
            float simUniTime;
            float[] mylats = { 46, 53, 60, 65 };
            for (int mylat = 0; mylat < 4; mylat++)
            {
                Latitude = mylats[mylat];

                for (simUniTime = 1; simUniTime < CycleLength * 2; simUniTime += step)
                {

                    sunStrength = 5.5f;
                    float beta = 0.07f;
                    float theta = 16f; //the 
                    float epsilon = .003f;
                    float corebuff = 0.0000f;// 01f;

                    float sun = sunStrength * Mathf.Max(getSunAngleAtUnitime(simUniTime), 0) / 90;

                    float numDays = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused() / 24f;
                    float tempRamp = GameManager.GetExperienceModeManagerComponent().GetOutdoorTempDropCelcius(numDays);
                    float abszero = -273f;// Solstice_RV.TemperatureOffset- tempRamp;


                    float rad = Mathf.Pow((mytemp - abszero), 4) * (1 / Mathf.Pow(0 - abszero, 4));
                    float ins = 0.5f;
                    float mins = step * 24 * 60;


                    float airgroundexchange = (mytemp - groundtemp) * epsilon;

                    mytemp += ((sun - rad) * (1 - ins) * beta - airgroundexchange) * mins;

                    groundtemp += (corebuff + airgroundexchange / theta) * mins;

                    if (simUniTime >= logtime)
                    {
                        Debug.Log((simUniTime + CycleOffset) / (float)CycleLength + "\t" + sun);
                        times += "\t" + (simUniTime + CycleOffset) / (float)CycleLength;
                        grnds += "\t" + groundtemp;
                        temps += "\t" + mytemp;
                        suns += "\t" + sun;

                        //logtime += 0.125f/2;
                    }
                }
            }
            Debug.Log("Times:" + times);
            Debug.Log("Gnds:" + grnds);
            Debug.Log("Temps:" + temps);
            Debug.Log("Suns:" + suns);
            //ebug.Log("UniYear at end of sim:" + UniYear(simUniTime));


        }
        */



        internal static void LoadData(string name)
        {
            string data = SaveGameSlots.LoadDataFromSlot(name, SAVE_FILE_NAME);
            StringArray stringArray = Utils.DeserializeObject<StringArray>(data);
            Solstice_RV.SetSettingsData(stringArray.strings[0]);
            Solstice_RV.lastTemperatureUpdate = Utils.DeserializeObject<float>(stringArray.strings[1]);
            Solstice_RV.outsideTemp = Utils.DeserializeObject<float>(stringArray.strings[2]);
            Update();

        }

        internal static void Log(string message)
        {
            Debug.Log("[Solstice] " + message);
        }

        internal static void Log(string message, params object[] parameters)
        {
            string preformattedMessage = string.Format(message, parameters);
            Log(preformattedMessage);
        }


        internal static float getSunAngleAtUnitime(float unitime)
        {

            float sunAngle = 90 - Latitude + precession * getDaysTilt(unitime);

            Vector3 suntoearth = new Vector3(0, 0, 1f);
            float zenith = sunAngle;

            suntoearth = Quaternion.Euler(zenith, 0, 0) * suntoearth;

            Vector3 earth_axis = new Vector3(0, 0, 1);

            earth_axis = Quaternion.Euler(-Latitude, 0, 0) * earth_axis;

            float newangle = 360*unitime- 180f;

            float newearthtosun = Mathf.Asin((Quaternion.AngleAxis(newangle, earth_axis) * suntoearth * -1f).y) * Mathf.Rad2Deg;
            return newearthtosun;           
        }

        internal static float[] calculatesTimes(float unitime) {
                
            UniStormWeatherSystem __instance = GameManager.GetUniStorm();
            
            float newearthtosun;
            bool donemid = false;
            float[] keytimes = { 0.00f, -999, 12, 12, 12, -999, 23.9999999f };
            float oldy = getSunAngleAtUnitime(Mathf.FloorToInt(unitime));
            float[] keyheights = {dawndusk_angle, riseset_angle, mornaft_angle, 666f, mornaft_angle, riseset_angle, dawndusk_angle };
            for (float mytime = 0; mytime < 1; mytime+=0.00001f)
            {

                newearthtosun = getSunAngleAtUnitime(Mathf.FloorToInt(unitime)+mytime);
                
                  //Debug.Log(mytime + ":" + newearthtosun);

                if (newearthtosun > keyheights[0] && oldy < keyheights[0]) { keytimes[0] = mytime * 24; }
                if (newearthtosun > keyheights[1] && oldy < keyheights[1]) { keytimes[1] = mytime*24; }
                if (newearthtosun > keyheights[2] && oldy < keyheights[2]) { keytimes[2] = mytime * 24; }
                if (newearthtosun < oldy && !donemid)
                {
                    //Debug.Log("mytime:" + mytime + "oldy:" + oldy + "newearthtosun:" + newearthtosun);
                    donemid = true;
                    keytimes[3] = 12;// mytime * 24; }
                }
                if (newearthtosun < keyheights[4] && oldy > keyheights[4]) { keytimes[4] = mytime * 24; }
                if (newearthtosun < keyheights[5] && oldy > keyheights[5]) { keytimes[5] = mytime * 24; }
                if (newearthtosun < keyheights[6] && oldy > keyheights[6]) { keytimes[6] = mytime * 24; }
                oldy = newearthtosun;
               
            }
            if (keytimes[1]==-999 || keytimes[5] == -999) { throw new System.ArgumentException("Sunrise/sunset undefined" , Utils.SerializeObject(keytimes)); }
            return  keytimes;

        }

        internal static void SaveData(SaveSlotType gameMode, string name)
        {
            StringArray stringArray = new StringArray();
            stringArray.strings = new string[3];
            stringArray.strings[0] =  Utils.SerializeObject(settings);
            stringArray.strings[1] = Utils.SerializeObject(lastTemperatureUpdate);
            stringArray.strings[2] = Utils.SerializeObject(outsideTemp);
            SaveGameSlots.SaveDataToSlot(gameMode, SaveGameSystem.m_CurrentEpisode, SaveGameSystem.m_CurrentGameId, name, SAVE_FILE_NAME, Utils.SerializeObject(stringArray));
        }

        internal static string ggtime(float intime)
        {
            return string.Format("{0:00}:{1:00}:{2:00}", Mathf.FloorToInt(intime), Mathf.FloorToInt(((intime) % 1) * 60), Mathf.FloorToInt((((intime) % 1) * 3600)%60));
        }

        internal static string gggtime(float intime)
        {

            return string.Format("{0:00}:{1:00}:{2:00}", Mathf.FloorToInt(intime), Mathf.FloorToInt(((intime) % 1) * 24), Mathf.FloorToInt(((intime) % (1f / 24f)) * 24 * 60));
        }


        public static float UniYear(float unitime)
        {
            //returns the day of the year so 1.5 for cyclelengh 36 is the 19th day of the first year
             return (float)((Mathf.FloorToInt(unitime)-1) + CycleLength + CycleOffset)/(float)(CycleLength);
        }

        public static float UniSeason(float unitime)
        {
            return UniYear(unitime)%1;
        }

    
        public static int getMonth(int day)
        {
            return Mathf.FloorToInt((UniYear(day)%1) * 12) ;
        }

        public static float unitime()
        {
            //maybe want to make this proofed against weird daycounter changes on scene loading...
            return GameManager.GetUniStorm().m_DayCounter + GameManager.GetUniStorm().m_NormalizedTime;
        }

        internal static void SolsticeLog()
        {
            float[] keyframeTimes = traverseKeyframeTimes.GetValue<float[]>();
           
            Log("Unitime:" + gggtime(unitime()) + ", " + months[getMonth(Mathf.FloorToInt(unitime()))] + ", UniYear:" + UniYear(Mathf.FloorToInt(unitime())) + " Zenith: " + string.Format("{0:0.0}", getSunAngleAtUnitime(Mathf.Floor(unitime())+0.5f ))+ "°  Temp Offset: " + string.Format("{0:0.00}", TemperatureOffset) + "°C");

            keyframeTimes = traverseKeyframeTimes.GetValue<float[]>();

            Log("[0NE]: " + ggtime(keyframeTimes[0]) + " [1DN]:" + ggtime(keyframeTimes[1]) + " [2Mo]:" + ggtime(keyframeTimes[2]) + " [3Md]:" + ggtime(keyframeTimes[3]) + " [4Af]:" + ggtime(keyframeTimes[4]) + " [5Du]:" + ggtime(keyframeTimes[5]) + "; [6Ns]: " + ggtime(keyframeTimes[6]));
            Log("Delete this when you see this is right BNS+" + better_night_sky_installed);


            SceneNameMapping mynamemap = GameManager.GetInterfaceManager().m_SceneNameMappingAsset;
            Dictionary<string, string> wow = (Dictionary<string, string>)AccessTools.Field(typeof(SceneNameMapping), "m_SceneNameMapping").GetValue(mynamemap);
            Debug.Log(Utils.SerializeObject(wow));

            wow = (Dictionary<string, string>)AccessTools.Field(typeof(SceneNameMapping), "m_SceneRegionMapping").GetValue(mynamemap);
            Debug.Log(Utils.SerializeObject(wow));



        }

        internal static TODStateConfig createNewMidPoint(TODStateConfig upbefore, TODStateConfig upafter, TODStateConfig downbefore, TODStateConfig downafter, float upcnt)
        {
            TODStateConfig midup = new TODStateConfig();
            TODStateConfig middown = new TODStateConfig();
            TODStateConfig midblend = new TODStateConfig();

            midup.SetBlended(upbefore, upafter, upcnt, upcnt, 0);
            middown.SetBlended(downbefore, downafter, upcnt, upcnt, 0);
            midblend.SetBlended(midup, middown, 0.5f, 0.5f, 0);
            return midblend;
        }
      

        internal static void attachSeasonLabel(GameObject labelObject)
        {
            mySeasonLabel = labelObject;
        }

        internal static void Update()
        {
            if (isEnabled)
            {

                Debug.Log(gggtime(unitime()) + " update called from elapsed hours time: " + gggtime(GameManager.GetUniStorm().GetElapsedHours() / 24f));

                TemperatureOffset = settings.tempRampStart + (settings.tempRampEnd - settings.tempRampStart) * Mathf.Min(1, (GameManager.GetUniStorm().m_DayCounter - 1)/settings.tempRampDays);

                GameManager.GetUniStorm().m_SunAngle = getSunAngleAtUnitime(Mathf.Floor(unitime()) + 0.5f);

                GameManager.GetUniStorm().m_MasterTimeKeyOffset = 0;  //what does this do!
                float[] keyframeTimes;

                keyframeTimes = calculatesTimes(unitime());

                traverseKeyframeTimes.SetValue(keyframeTimes);
                TimeWidgetUpdater.SetTimes(keyframeTimes[1], keyframeTimes[3], keyframeTimes[5]);

                UILabel label = mySeasonLabel.GetComponent<UILabel>();
                label.text = months[getMonth(GameManager.GetUniStorm().m_DayCounter)];

            }
        }

    
        private static void RestoreKeyframeTimes(UniStormWeatherSystem uniStormWeatherSystem)
        {
            float[] keyframeTimes = traverseKeyframeTimes.GetValue<float[]>();
            for (int i = 0; i < keyframeTimes.Length; i++)
            {
                keyframeTimes[i] = originalKeyframeTimes[i];
            }

            uniStormWeatherSystem.m_MasterTimeKeyOffset = originalMasterTimeKeyOffset;
        }


        private static void SetSettingsData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                Disable();
            }
            else
            {
                JsonUtility.FromJsonOverwrite(data, settings);
                initLatitude(settings.Latitude);
            }         
        }
    }

    internal class Settings
    {
        public int Latitude = 65;
        public int cycleLength=24;
        public int cycleOffset=3;
        public bool enabled=true;
        public float tempRampStart =0;
        public float tempRampEnd = 0;
        public float tempRampDays = 0;
        public float sunStrength = 3f;
        public float beta = 0.25f;
        public float theta = 40;
        public float epsilon = 0.01f;
        public float gamma = 0.0002f;


    }


}