using System;
using UnityEngine;

namespace Route24.GameOff
{
    public static class AngleVector 
    {
        public static float GetAngleFromVector(Vector2 direction)
        {
            double radians = Math.Atan2(direction.y, direction.x);

            double degrees = radians * (180.0 / Math.PI);

            return (float)degrees;
        }

        public static Vector2 GetVectorFromAngle(float angle)
        {
            double angleInRadians = angle * (Math.PI / 180.0);

            double x = Math.Cos(angleInRadians);
            double y = Math.Sin(angleInRadians);

            return new Vector2((float)x, (float)y);
        }

        public static float GetAngleBetween(float angle1, float angle2)
        {
            float dif = (angle1 - angle2 + 540) % 360 - 180;
            return Math.Abs(dif);
        }

        public static float GetAngleBetween(Vector2 direction, float angle)
        {
            return GetAngleBetween(GetAngleFromVector(direction), angle);
        }
    }
}