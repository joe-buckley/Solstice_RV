using ModSettings;
using System.Reflection;

namespace Solstice_RV
{
    public class GUISettings : ModSettingsBase
    {
        [Section("Solstice")]

        [Name("Enabled")]
        [Description("If enabled the length of the day will cycle from short to long and back to short.")]
        public bool Enabled = true;

        [Name("Days in Month")]
        [Description("This will change the duration of the seasons.")]
        [Slider(1f, 30, 30)]
        public float DaysInMonth = 4;

        [Name("Starting Month")]
        [Description("Winter days are short and colder, summer days are long and warmer")]
        public StartOffset StartMonth = StartOffset.Random;

        [Name("Latitude")]
        [Description("How Far North have you crashed.\nIncreases seasonal effects on daylight hours")]
        [Choice("46 degrees", "53 degrees", "60 degrees","65 degrees")]
        public Location Location = 0;

        [Name("Summer Temperature")]
        [Description("The Base Temperature in Summer.\n Recommended Settings (Easier:+5 Med:0 Hard:-5)\n")]
        [Slider(-50f, 10f, 61)]
        public float SummerTemperature = 5f;

        [Name("Winter Temperature")]
        [Description("The Base Temperature in Winter.\n Recommended Settings (Easier:-10 Med:-20 Hard:-30)")]
        [Slider(-80f, 0f, 81)]
        public float WinterTemperature = -10f;




        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            if (field.Name == "Enabled")
            {
                bool visible = (bool)newValue;
                this.SetFieldVisible(typeof(GUISettings).GetField("DaysInMonth"), visible);
                this.SetFieldVisible(typeof(GUISettings).GetField("StartMonth"), visible);
                this.SetFieldVisible(typeof(GUISettings).GetField("Location"), visible);
                this.SetFieldVisible(typeof(GUISettings).GetField("SummerTemperature"), visible);
                this.SetFieldVisible(typeof(GUISettings).GetField("WinterTemperature"), visible);
            }
            if (field.Name == "Location")
            {
                switch (Location)
                {
                    case Location.FortySix:
                        SummerTemperature = 5;
                        WinterTemperature = -10;
                        break;
                    case Location.FiftyThree:
                        SummerTemperature = 0;
                        WinterTemperature = -20;
                        break;

                    case Location.Sixty:
                        SummerTemperature = -5;
                        WinterTemperature = -30;
                        break;
                    case Location.SixtyFive:
                        SummerTemperature = -10;
                        WinterTemperature = -40;
                        break;
                }
                RefreshGUI();

            }
        }

        protected override void OnConfirm()
        {
            Solstice_RV.ApplySettings();
        }
    }

    public enum StartOffset
    {
        Random = 0,
        January = 1,
        February = 2,
        March = 3,
        April = 4,
        May = 5,
        June = 6,
        July = 7,
        August = 8,
        September = 9,
        October= 10,
        November =11,
        December = 12


    }
    public enum Location
    {
        FortySix = 0,
        FiftyThree = 1,
        Sixty = 2,
        SixtyFive =3,
    }
}