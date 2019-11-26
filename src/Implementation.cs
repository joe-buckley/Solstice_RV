using Harmony;
using System.Reflection;
using UnityEngine;

/*
 * Todo:
 * 
 * Cat Tail Distribution Mod 
 * cattail plant spawn chance, globalY, regional, seasonal variation
 * every year each cat tail grows or not based on local probability p
 * so each year there will be a harvestable yield of np cattails for each region
 * which reduces to a daily yield of np/year length
 * we can add a population decline rate as well 
 * p needs to be corrected for yearlength
 * 
 * link up the back of timberwolf to coastal
 * 
 * link up the back of railroad to hushed
 *
 * * cattail plant decay over months. 
 * 
 * cattail and cattail head decay once harvested. 
 * allow to see amount of meat on frozen carcass
 * fix fire brings air temperature above 0 to harvest carcass
 * 
 * Bunnies Have Homes
 * Deer and Wolves wander
 * 
 * hides have different thicknesses
 * 
 * mess with starting deer carcasses
 * 
 * 
 * GameManager.GetPlayerTransform().position
 * 
 * 
 * 
 * 
 */

namespace Solstice
{
    internal class Solstice
    {
        private const string SAVE_FILE_NAME = "solstice-settings";
        private static readonly GUISettings guiSettings = new GUISettings();
        private static readonly Settings settings = new Settings();

        private static string[] seasons = { "Winter", "Spring", "Summer", "Fall" };
        private static string[] months = { "January", "February", "March", "April","May","June","July","August","September","October","November","December" };

        private static float[] originalKeyframeTimes;
        private static float   originalMasterTimeKeyOffset;

        private static Traverse traverseKeyframeTimes;

        private static InterpolatedValue SunAngle;

        private static GameObject mySeasonLabel;

        private static float[,] dayData = new float[4,5];


        internal static float BrightnessMultiplier
        {
            get; private set;
        }

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
        internal static float Strength
        {
            get; private set;
        }
        internal static float Latitude
        {
            get; private set;
        }
        internal static float UniSeason
        {
            get; private set;
        }

        public static void OnLoad()
        {
            Log("Version {0}", Assembly.GetExecutingAssembly().GetName().Version);

            guiSettings.AddToCustomModeMenu(ModSettings.Position.BelowEnvironment);

            uConsole.RegisterCommand("solstice-log", SolsticeLog);
        }

        private static float LerpFromArray(float[,] array, float x)
        {
            if (x < array[0, 0])
                return array[0, 1];

            int rows = array.GetLength(0);
            for (int i = 1; i < rows; ++i)
            {
                if (x < array[i, 0])
                {
                    float t = Mathf.InverseLerp(array[i - 1, 0], array[i, 0], x);
                    return Mathf.Lerp(array[i - 1, 1], array[i, 1], t);
                }
            }
            return array[rows - 1, 1];
        }

        private static float CircLerpFromArray1D(float[] array, float x)
        {
            int StartRow = Mathf.FloorToInt(x) + 1;
            int rows = array.GetLength(0);

            if (StartRow < 1) throw new System.ArgumentException("x <0 fail case", "original"); ;
            if (StartRow>rows) throw new System.ArgumentException("x> arraylength fail case", "original"); ;
            if (StartRow == rows) return Mathf.Lerp(array[StartRow -1], array[0], x % 1);
            return Mathf.Lerp(array[StartRow-1], array[StartRow], x%1);
      
      
        }
        private static float CircLerpFromArray2D(float[,] array,int col, float x)
        {
            int rows = array.GetLength(0);
            float[] temp=new float[rows];
            //string logstring = "";
            for (int i = 0; i < rows; i++)
            {
                temp[i] = array[i, col];
                //logstring += " " + temp[i].ToString("0.00");

            }
        //Log("2DLerp ["+logstring+"] x:"+x+", res:" + CircLerpFromArray1D(temp, x));
        return CircLerpFromArray1D(temp,x);
        }
        internal static void initLatitude(int LatitudeChoice)
        {
            string[,] dayDataIn;

            switch ((Location)LatitudeChoice)
            {
                case Location.FortySix:
                    dayDataIn = new string[4, 5] {  { "07:13", "07:47", "12:07", "16:27", "17:01" },
                                                    { "06:41", "07:10", "13:16", "19:23", "19:53" },
                                                    { "04:40", "05:18", "13:11", "21:04", "21:42" },
                                                    { "06:25", "06:54", "13:02", "19:09", "19:39" }
                                                  };
                    Latitude = 46f;
                    SunAngle = new InterpolatedValue(90 - Latitude, 23f);
                    break;
                case Location.FiftyThree:
                    dayDataIn = new string[4, 5] {  { "07:38", "8:20", "12:07", "15:55", "16:36" },
                                                    { "06:35", "07:09", "13:16", "19:24", "19:58" },
                                                    { "03:51", "04:42", "13:11", "21:39", "22:30" },
                                                    { "06:18", "06:52", "13:02", "19:11", "19:45" }
                                                  };
                    Latitude = 53f;
                    SunAngle = new InterpolatedValue(90 - Latitude, 23f);
                    break;
                case Location.Sixty:
                    dayDataIn = new string[4, 5] {  { "08:13", "9:10", "12:07", "15:04", "16:01" },
                                                    { "06:27", "07:08", "13:16", "19:26", "20:07" },
                                                    { "01:58", "03:44", "13:11", "22:38", "12:24" },
                                                    { "06:09", "06:50", "13:02", "19:13", "19:54" }
                                                  };
                    Latitude = 60f;
                    SunAngle = new InterpolatedValue(90 - Latitude, 23f);
                    break;
                case Location.SixtyFive:
                    dayDataIn = new string[4, 5] {  { "08:52", "10:18", "12:07", "13:56", "15:22" },
                                                    { "06:17", "07:06", "13:16", "19:28", "20:17" },
                                                    { "23:56", "02:25", "13:11", "23:57", "02:24" },
                                                    { "05:58", "06:48", "13:02", "19:15", "20:04" }
                                                  };
                    Latitude = 65f;
                    SunAngle = new InterpolatedValue(90 - Latitude, 23f);
                    break;

                default:
                    dayDataIn = new string[4, 5] {  { "07:13", "07:47", "12:07", "16:27", "17:01" },
                                                    { "06:41", "07:10", "13:16", "19:23", "19:53" },
                                                    { "04:40", "05:18", "13:11", "21:04", "21:42" },
                                                    { "06:25", "06:54", "13:02", "19:09", "19:39" }
                                                  };
                    Latitude = 46f;
                    SunAngle = new InterpolatedValue(90 - Latitude, 23f);
                    break;

            }
            for (int i = 0; i < 4; i++){
                for (int j = 0; j < 5; j++)    {
                    dayData[i, j] = float.Parse(dayDataIn[i, j].Split(':')[0]) + float.Parse(dayDataIn[i, j].Split(':')[1]) / 60;
                }
            }
            //correct middays and dawns
            string logtext = "";
            for (int i = 0; i < 4; i++){
                if (dayData[i, 0] > dayData[i, 2]) {  dayData[i, 0] -= 24f; } //weird edge case dawn before midnight
                if (dayData[i, 4] < dayData[i, 2]) { dayData[i, 4] += 24f; }//weird edge case dusk after midnight

                float oldnoon = dayData[i, 2];
                logtext += "[";
                for (int j = 0; j < 5; j++)
                {
                    dayData[i, j] = dayData[i, j] - oldnoon + 12f;
                    logtext=logtext+ ggtime(dayData[i,j])+",";
                }
                Debug.Log(logtext+"]");
                logtext ="";
            }
        }
    


        internal static void ApplySettings()
        {
            settings.enabled = guiSettings.Enabled;

            settings.cycleLength = 12 *  Mathf.FloorToInt(guiSettings.DaysInMonth);          

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
            
            RestoreKeyframeTimes(GameManager.GetUniStorm());
            Update(GameManager.GetUniStorm());
        }

        internal static void Disable()
        {
            if (Enabled)
            {
                Enabled = false;
                RestoreKeyframeTimes(GameManager.GetUniStorm());
            }
        }

        internal static void Init(UniStormWeatherSystem uniStormWeatherSystem)
        {
            traverseKeyframeTimes = Traverse.Create(uniStormWeatherSystem).Field("m_TODKeyframeTimes");

            originalMasterTimeKeyOffset = uniStormWeatherSystem.m_MasterTimeKeyOffset;
            originalKeyframeTimes = (float[])traverseKeyframeTimes.GetValue<float[]>().Clone();
            Log("Original mtko"+ originalMasterTimeKeyOffset + " times   [0]:" + originalKeyframeTimes[0] + "; [1]: " + originalKeyframeTimes[1] + "[2]:" + originalKeyframeTimes[2] + "; [3]: " + originalKeyframeTimes[3] + "; [4]: " + originalKeyframeTimes[4] + "; [5]: " + originalKeyframeTimes[5] + "; [6]: " + originalKeyframeTimes[6]);

        }

        internal static void LoadData(string name)
        {
            string data = SaveGameSlots.LoadDataFromSlot(name, SAVE_FILE_NAME);
            Implementation.SetSettingsData(data);
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

        internal static void SaveData(SaveSlotType gameMode, string name)
        {
            string data = Utils.SerializeObject(settings);
            SaveGameSlots.SaveDataToSlot(gameMode, SaveGameSystem.m_CurrentEpisode, SaveGameSystem.m_CurrentGameId, name, SAVE_FILE_NAME, data);
        }

        internal static string ggtime(float intime)
        {
            return string.Format("{0:00}:{1:00}", Mathf.FloorToInt(intime), Mathf.FloorToInt(((intime) % 1) * 60));
        }

        internal static float uniSeason(int dayCounter)
        {
            Log("DayCounter:" + dayCounter + "CyCleOffset" + CycleOffset + "CycleLength" + CycleLength+"uniSeason"+ (float)((dayCounter + CycleOffset) % CycleLength) / (float)(CycleLength - 1));
            return (float)((dayCounter + CycleOffset-1) % CycleLength)/(float)(CycleLength-1);
        }

        //Strength = -1.0f*Mathf.Cos(Mathf.PI* 2f * UniSeason);


        public static string getSeasonInfo()
        {
            return seasons[Mathf.FloorToInt(UniSeason * 4)] + ": " + Mathf.FloorToInt(400f*(UniSeason % 0.25f))+ "% done";
        }

        public static int getMonth()
        {
            return Mathf.FloorToInt(UniSeason * 12) ;
        }

        internal static void SolsticeLog()
        {
  
            Log(ggtime(GameManager.GetUniStorm().m_NormalizedTime * 24f) +" hrs : "+getSeasonInfo() + " Zenith: " + string.Format("{0:0.0}", SunAngle.Calculate(Strength))+ "° Bright: " + string.Format("{0:0.00}", BrightnessMultiplier*100f) + "% Temp Offset: " + string.Format("{0:0.00}", TemperatureOffset)+ "°C");

            float[] keyframeTimes = traverseKeyframeTimes.GetValue<float[]>();

            Log("[0NE]: " + ggtime(keyframeTimes[0]) + " [1DN]:" + ggtime(keyframeTimes[1]) + " [2Mo]:" + ggtime(keyframeTimes[2]) + " [3Md]:" + ggtime(keyframeTimes[3]) + " [4Af]:" + ggtime(keyframeTimes[4]) + " [5Du]:" + ggtime(keyframeTimes[5]) + "; [6Ns]: " + ggtime(keyframeTimes[6]));



            //}
        }

        internal static void attachSeasonLabel(GameObject labelObject)
        {
            mySeasonLabel = labelObject;
        }

        internal static void Update(UniStormWeatherSystem uniStormWeatherSystem)
        {
            if (!Enabled)
            {
                return;
            }

            UniSeason = uniSeason(uniStormWeatherSystem.m_DayCounter);
            Log("Uniseason" + UniSeason);
            Strength = -1.0f*Mathf.Cos(Mathf.PI * 2f * UniSeason);

            BrightnessMultiplier = Mathf.Clamp(0.5f + SunAngle.Calculate(Strength)/60f, 0, 1);

            TemperatureOffset = (settings.summerTemperature+settings.winterTemperature)/2 +  (settings.summerTemperature - settings.winterTemperature) * Strength/2;
            //Log(ggtime(GameManager.GetUniStorm().m_NormalizedTime * 24f) + " hrs : " + getSeasonInfo() + " Zenith: " + string.Format("{0:0.0}", SunAngle.Calculate(Strength)) + "° Bright: " + string.Format("{0:0.00}", BrightnessMultiplier * 100f) + "% Temp Offset: " + string.Format("{0:0.00}", TemperatureOffset) + "°C");
            ConfigureKeyframeTimes(uniStormWeatherSystem);
            UILabel label = mySeasonLabel.GetComponent<UILabel>();
            label.text = months[getMonth()];
/*
            if (newMonth)
            {
                //do housekeeping
                for (int i = 0; i < HarvestableManager.m_Harvestables.Count; i++)
                {
                    if (HarvestableManager.m_Harvestables[i])
                    {
                        //
                        HarvestableManager.m_Harvestables[i].Serialize();
                    }
                }
            }
            */

        }

        private static float tilt()
        {
            if (UniSeason < 0.5) return (-1 * Mathf.Cos(UniSeason * Mathf.PI * 2) + 1);
            else return (3 + Mathf.Cos(UniSeason * Mathf.PI * 2));
        }
        private static void ConfigureKeyframeTimes(UniStormWeatherSystem uniStormWeatherSystem)
        {
            uniStormWeatherSystem.m_MasterTimeKeyOffset = 0;
            float[] keyframeTimes = traverseKeyframeTimes.GetValue<float[]>();
            Log("UniSeason*4:"+UniSeason*4+", Tilt:" + tilt());
            float cdawn    = Mathf.Clamp(CircLerpFromArray2D(dayData,0, tilt()), 0.0f, 23.999999f);
            float csunrise = CircLerpFromArray2D(dayData,1, tilt());
            float cnoon = CircLerpFromArray2D(dayData,2, tilt());
            float csunset = CircLerpFromArray2D(dayData,3, tilt());
            float cdusk =    Mathf.Clamp(CircLerpFromArray2D(dayData,4, (UniSeason)*4), 0.0f, 23.999999f);



            Log("dawn: " + ggtime(cdawn) + " sunrise:" + ggtime(csunrise) + " noon:" + ggtime(cnoon) + " sunset:" + ggtime(csunset) + " dusk:" + ggtime(cdusk) );
            
            //		TODKeyframeTimes [0]:NightEnd [1]:Dawn [2]:Morning [3]:Midday [4]:Afternoon [5]:Dusk [6]:NightStart
            
            keyframeTimes[0] = Mathf.Clamp(cdawn , 0.0f, 23.999999f);//dawn start
            keyframeTimes[1] = Mathf.Clamp(cdawn * 0.5f + csunrise * .5f,  0.0f, 23.999999f);//dawn start
            keyframeTimes[2] = csunrise; //morning start
            keyframeTimes[3] = cnoon; //noon start
            keyframeTimes[4] = csunset;// afternoon start
            keyframeTimes[5] = Mathf.Clamp(cdusk * 0.5f + csunset * .5f, 0.0f, 23.999999f); 
            keyframeTimes[6] = Mathf.Clamp(cdusk, 0.0f, 23.999999f);// night start

            uniStormWeatherSystem.m_SunAngle = SunAngle.Calculate(Strength);
            //Log("[0NE]: " + ggtime(keyframeTimes[0]) + " [1DN]:" + ggtime(keyframeTimes[1]) + " [2Mo]:" + ggtime(keyframeTimes[2]) + " [3Md]:" + ggtime(keyframeTimes[3]) + " [4Af]:" + ggtime(keyframeTimes[4]) + " [5Du]:" + ggtime(keyframeTimes[5]) + "; [6Ns]: " + ggtime(keyframeTimes[6]));
            
            TimeWidgetUpdater.SetTimes(keyframeTimes[1], cnoon, keyframeTimes[5]);
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
                Update(GameManager.GetUniStorm());
            }

         
        }
    }

    internal class Settings
    {
        public int cycleLength;
        public int cycleOffset;
        public bool enabled;
        public float summerTemperature;
        public float winterTemperature;
        public int Latitude;

    }


}