using Harmony;
using System.Reflection;
using UnityEngine;
using System.IO;

//Left to do:
//Fix precession so it uses rotation around an axis
//Test more extreme settings

namespace Solstice_RV
{
    internal class Solstice_RV
    {
        private const string SAVE_FILE_NAME = "solstice-settings";

        private static readonly GUISettings guiSettings = new GUISettings();

        private static readonly Settings settings = new Settings();

        private static string[] months = { "January", "February", "March", "April","May","June","July","August","September","October","November","December" };

        private static float[] originalKeyframeTimes;

        private static float   originalMasterTimeKeyOffset;

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

        internal static bool Enabled
        {
            get => settings.enabled;
            private set => settings.enabled = value;
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

            guiSettings.AddToCustomModeMenu(ModSettings.Position.BelowEnvironment);

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

            settings.cycleLength = 12 * Mathf.FloorToInt(guiSettings.DaysInMonth);

            if ((int)guiSettings.StartMonth == 0) {
                settings.cycleOffset = (int)(Random.value * CycleLength); }
            else
            {
                settings.cycleOffset = ((int)guiSettings.StartMonth - 1) * (int)CycleLength / 12;
            }
 
            settings.Latitude = (int)guiSettings.Location;
            initLatitude(settings.Latitude);

            settings.summerTemperature   = guiSettings.SummerTemperature;
            settings.winterTemperature   = guiSettings.WinterTemperature;

            Enabled = settings.enabled;

            if (!Enabled)
            {
                RestoreKeyframeTimes(GameManager.GetUniStorm());
            } 

            Update();
        }

        internal static void Disable()
        {
            if (Enabled)
            {
                Enabled = false;
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

        internal static void LoadData(string name)
        {
            string data = SaveGameSlots.LoadDataFromSlot(name, SAVE_FILE_NAME);
            StringArray stringArray = Utils.DeserializeObject<StringArray>(data);
            Solstice_RV.SetSettingsData(stringArray.strings[0]);       
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
                    Debug.Log("mytime:" + mytime + "oldy:" + oldy + "newearthtosun:" + newearthtosun);
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
            stringArray.strings = new string[1];
            stringArray.strings[0] =  Utils.SerializeObject(settings);
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
            Update();
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
            if (Enabled)
            {

                Debug.Log(gggtime(unitime()) + " update called from elapsed hours time: " + gggtime(GameManager.GetUniStorm().GetElapsedHours() / 24f));

                TemperatureOffset = (settings.summerTemperature + settings.winterTemperature) / 2 + (settings.summerTemperature - settings.winterTemperature) * getDaysTilt(unitime()) / 2;

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
        public int cycleLength=24;
        public int cycleOffset=3;
        public bool enabled=true;
        public float summerTemperature=0;
        public float winterTemperature=-20;
        public int Latitude=65;
       

    }


}