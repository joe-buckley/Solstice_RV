using Harmony;
using System.Reflection;
using UnityEngine;

=

namespace Solstice
{
    internal class Harvestables
    {
        public enum HarvestableType
        {
            Unknown,
            Cattail,
            Rosehip,
            Reishi,
            OldMansBeard,
            BirchSapling,
            MapleSapling
        }
        public static HarvestableType GetHarvestableFromString(string regionString)
        {
            HarvestableType result = HarvestableType.Unknown;
            switch (regionString)
            {
                case "GAMEPLAY_CattailPlant":
                    result = HarvestableType.Cattail; break;
                case "GAMEPLAY_ReishiMushroom":
                    result = HarvestableType.Reishi; break;
                case "GAMEPLAY_RoseHips":
                    result = HarvestableType.Rosehip; break;
                case "GAMEPLAY_OldMansBeard":
                    result = HarvestableType.OldMansBeard; break;
                case "GAMEPLAY_BirchSapling":
                    result = HarvestableType.BirchSapling; break;
                case "GAMEPLAY_MapleSapling":
                    result = HarvestableType.MapleSapling; break;
                default: throw new System.ArgumentException("Parameter cannot be found", "regionString");
            }
            return result;
        }
        public static float UpdateNextHarvestTime(float harvestTime, string harvestableName, GameRegion regionID)
        {
            float growthSeasonStart = 3.0f / 12f;
            float growthSeasonEnd = 5.0f / 12f;
            float aliveTime = 6.0f / 12f;
            bool growthFailed = harvestTime < 0;
            float curTime = UniTime;

            if (curTime > Mathf.Abs(harvestTime) + aliveTime) // time to reroll
            {
                //will we spawn

                harvestTime = this_Year_Start + Random.Range(growthSeasonStart, growthSeasonEnd);
                bool spawn = Random.Range(0f, 100f) < Mathf.Clamp(spawnChance(GetHarvestableFromString(harvestableName), regionID), 0f, 100f));
                return spawn ? harvestTime : harvestTime * -1f;

            }
            else
                Harvestable.
            {
                return harvestTime; //no change
            }
        }


        public static bool isAlive(float harvestTime, string harvestableName, GameRegion regionID)
        {

            float curTime = UniTime;
            return (curTime > harvestTime && curTime < (harvestTime + harvestable_Lifetime[GetHarvestableFromString(harvestableName), regionID])); // plant is alive

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




}