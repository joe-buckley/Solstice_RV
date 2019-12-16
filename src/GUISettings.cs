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

        [Name("Latitude")]
        [Description("How Far North have you crashed.\nIncreases seasonal effects on sun strength and daylight hours")]
        [Choice("46 degrees", "53 degrees", "60 degrees", "65 degrees")]
        public Location Location = Location.FiftyThree;

        [Name("Starting Month")]
        [Description("Winter days are short and colder, summer days are long and warmer")]
        public StartOffset StartMonth = StartOffset.May;

        [Name("Days in Month")]
        [Description("This will change the duration of the seasons.")]
        [Slider(1f, 30, 30)]
        public float DaysInMonth = 3;

        [Name("Starting Temperature Buff")]
        [Description("Starting temperature buff.\n")]
        [Slider(-10f, 10f, 21)]
        public float tempRampStart = 0f;

        [Name("Final Temperature Buff")]
        [Description("Final temperature buff.\n")]
        [Slider(-50f, 10f, 61)]
        public float tempRampEnd = -20f;

        [Name("Temperature Ramp Days")]
        [Description("Days to reach final temperature buff\n")]
        [Slider(0f, 500f, 101)]
        public float tempRampDays = 100f;




        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            if (field.Name == "Enabled")
            {
                bool visible = (bool)newValue;
                this.SetFieldVisible(typeof(GUISettings).GetField("DaysInMonth"), visible);
                this.SetFieldVisible(typeof(GUISettings).GetField("StartMonth"), visible);
                this.SetFieldVisible(typeof(GUISettings).GetField("Location"), visible);
                this.SetFieldVisible(typeof(GUISettings).GetField("tempRampStart"), visible);
                this.SetFieldVisible(typeof(GUISettings).GetField("tempRampEnd"), visible);
                this.SetFieldVisible(typeof(GUISettings).GetField("tempRampDays"), visible);
            }
            if (field.Name == "Location")
            {
                switch (Location)
                {
                    case Location.FortySix:
                        tempRampStart = 5;
                        tempRampEnd = -5;
                        tempRampDays = 50;
                        break;
                    case Location.FiftyThree:
                        tempRampStart = 5;
                        tempRampEnd = -5;
                        tempRampDays = 50;
                        break;

                    case Location.Sixty:
                        tempRampStart = 5;
                        tempRampEnd = -5;
                        tempRampDays = 50;
                        break;
                    case Location.SixtyFive:
                        tempRampStart = 5;
                        tempRampEnd = -5 ;
                        tempRampDays = 50;
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