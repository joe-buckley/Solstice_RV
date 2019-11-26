using Harmony;
using UnityEngine;

namespace Solstice_RV
{
    internal class TimeWidgetUpdater
    {
        private static readonly Interpolator moonInterpolator = new Interpolator();
        private static readonly Interpolator sunInterpolator = new Interpolator();
        private static float moonRadius;
        private static float sunRadius;

        internal static void Initialize(TimeWidget timeWidget)
        {
            sunRadius = Traverse.Create(timeWidget).Field("m_SunRadius").GetValue<float>();
            moonRadius = Traverse.Create(timeWidget).Field("m_MoonRadius").GetValue<float>();
        }

        internal static void SetTimes(float sunrise,float noon, float sunset)
        {
            sunInterpolator.Clear();
            sunInterpolator.Set(0, -180);
            sunInterpolator.Set(sunrise / 24f, -100);
            sunInterpolator.Set(noon/24f, 0);
            sunInterpolator.Set(sunset / 24f, 100);
            sunInterpolator.Set(1, 180);

            moonInterpolator.Clear();
            moonInterpolator.Set(0, 0);
            moonInterpolator.Set(sunrise / 24f, 100);
            moonInterpolator.Set(noon/24f, 180);
            moonInterpolator.Set(sunset / 24f, 260);
            moonInterpolator.Set(1, 360);
        }

        internal static void Update(TimeWidget timeWidget, float angleDegrees)
        {
            float normalizedTime = GameManager.GetUniStorm().m_NormalizedTime;
            float sunAngle = sunInterpolator.GetValue(normalizedTime);
            timeWidget.m_SunSprite.transform.position = timeWidget.m_ArrowSprite.transform.position + GetPositionOnCircle(sunRadius, sunAngle);
            float moonAngle = moonInterpolator.GetValue(normalizedTime);
            timeWidget.m_MoonSprite.transform.position = timeWidget.m_ArrowSprite.transform.position + GetPositionOnCircle(moonRadius, moonAngle);
            //Debug.Log("[Solstice] " + "NormalizedTime: " + normalizedTime + " In hours " + normalizedTime*24f);

        }

        private static Vector3 GetPositionOnCircle(float radius, float angleDegrees)
        {
            float xpos = radius * Mathf.Sin(angleDegrees * Mathf.Deg2Rad);
            float ypos = radius * Mathf.Cos(angleDegrees * Mathf.Deg2Rad);
            //Debug.Log("[Solstice] " + "Radius: " + radius + " angle: " + angleDegrees + "xpos: " +  xpos + "ypos: " + ypos );
            return new Vector3(xpos,ypos, 0.0f);
        }
    }
}