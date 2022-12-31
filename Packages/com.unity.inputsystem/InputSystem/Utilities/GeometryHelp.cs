using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine.InputSystem.Utilities
{
    public class GeometryHelp
    {
        public enum CircleMethod { MouseFurthestPoints, MouseThreePoints, Gamepad }

        public class Circle{
            public Vector2 Center;
            public float Radius;

            public Circle(Vector2 center, float radius)
            {
                Center = center;
                Radius = radius;
            }
        }

        /// <summary>
        /// Take 3 points among the list and get the circle that is passing by these 3 points.
        /// </summary>
        /// <remarks>Will work only with the mouse. For better result, considere ussing <see cref="GetCircleFurthestPoints(List{Vector2})"/> instead</remarks>
        /// <param name="points"></param>
        /// <returns></returns>
        private static Circle GetCircle3Points(List<Vector2> points)
        {
            if(points==null || points.Count < 3)
            {
                //not enough points !
                return null;
            }

            int indexA, indexB, indexC;
            indexA = 0;
            indexB = (int)(points.Count / 3);
            indexC = indexB + (int)(points.Count / 3);

            return GetCircle3Points(points[indexA], points[indexB], points[indexC]);
        }

        /// <summary>
        /// Get the circle that is passing by all 3 points given in parameter
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <returns></returns>
        private static Circle GetCircle3Points(Vector2 point1, Vector2 point2, Vector2 point3)
        {
            var a = point1.x * (point2.y - point3.y) - point1.y * (point2.x - point3.x) + point2.x * point3.y - point3.x * point2.y;

            var b = (point1.x * point1.x + point1.y * point1.y) * (point3.y - point2.y)
                  + (point2.x * point2.x + point2.y * point2.y) * (point1.y - point3.y)
                  + (point3.x * point3.x + point3.y * point3.y) * (point2.y - point1.y);

            var c = (point1.x * point1.x + point1.y * point1.y) * (point2.x - point3.x)
                  + (point2.x * point2.x + point2.y * point2.y) * (point3.x - point1.x)
                  + (point3.x * point3.x + point3.y * point3.y) * (point1.x - point2.x);

            Vector2 center = new Vector2(-b / (2 * a), -c / (2 * a));
            float radius = Mathf.Sqrt(Mathf.Pow((center.x - point1.x),2) + Mathf.Pow((center.y - point1.y),2));

            return new Circle(center, radius);
        }

        /// <summary>
        /// Get the circle define by the distance of the 2 furthest point as diameter and the middle of these 2 points as center
        /// </summary>
        /// <remarks>Will work only for the mouse</remarks>
        /// <param name="points"></param>
        /// <returns></returns>
        private static Circle GetCircleFurthestPoints(List<Vector2> points)
        {
            Vector2[] furthestPoints = FindFurthestPoints(points);

            Vector2 center = new Vector2((furthestPoints[0].x + furthestPoints[1].x) / 2, (furthestPoints[0].y + furthestPoints[1].y) / 2);
            float radius = Vector2.Distance(furthestPoints[0], furthestPoints[1])/2;
            return new Circle(center, radius);
        }

        /// <summary>
        /// For a given list of points, find the 2 points with the longest distance between
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private static Vector2[] FindFurthestPoints(List<Vector2> points)
        {
            Vector2[] furthestPoints = new Vector2[2];
            float longestDistance = 0;
            float currentDistance = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                for (int j = i + 1; j < points.Count; ++j)
                {
                    currentDistance = Vector2.Distance(points[i], points[j]);
                    if (currentDistance > longestDistance)
                    {
                        furthestPoints[0] = points[i];
                        furthestPoints[1] = points[j];
                        longestDistance = currentDistance;
                    }
                }
            }

            return furthestPoints;
        }

        /// <summary>
        /// Get the circle that contains all the possible values returned by the gamepad stick
        /// </summary>
        /// <remarks>Will wok only for the gamepad stick</remarks>
        /// <returns></returns>
        private static Circle GetCircleGamepadValue()
        {
            return new Circle(new Vector2(0, 0), 1);
        }

        public static Circle GetCircle(List<Vector2> points, CircleMethod method)
        {
            switch (method)
            {
                case CircleMethod.MouseFurthestPoints:
                    return GetCircleFurthestPoints(points);
                case CircleMethod.MouseThreePoints:
                    return GetCircle3Points(points);
                case CircleMethod.Gamepad:
                    return GetCircleGamepadValue();
            }

            return null;
        }

        /// <summary>
        /// For <see cref="CircleMethod.MouseFurthestPoints"/> and <see cref="CircleMethod.MouseThreePoints"/>, Check if all the points are in between 2 limit circles. 
        /// A first circle is calculated depending on the chosen method then the 2 limit circles are define from this first circle and the accuracyPercent. 
        /// For <see cref="CircleMethod.Gamepad"/>, check if most of the points are on the Gamepad value circle and are evenly spread.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="accuracyPercent"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static bool IsCircle(List<Vector2> points, float accuracyPercent, CircleMethod method)
        {
            if (points.Count <= 2)
            {
                //Not enough points to calculate a circle
                return false;
            }

            //Modelize a base circle that will enclosed most of the points using the chosen method
            Circle originalCircle = GetCircle(points,method);
            if (originalCircle == null)
            {
                return false;
            }

            //Calculate accuracy offset based on the radius of the circle
            float accuracyOffset = originalCircle.Radius * (100 - accuracyPercent) / 100;

            if (method == CircleMethod.Gamepad)
            {
                int countPointsOnCircle = 0;
                Vector2 AveragePoint = new Vector2(0, 0);
                for(int i = 0; i < points.Count; ++i)
                {
                    //check if the point is on the circle
                    float circleEquation = Mathf.Pow((points[i].x - originalCircle.Center.x), 2) + Mathf.Pow((points[i].y - originalCircle.Center.y), 2) - Mathf.Pow(originalCircle.Radius, 2);
                    //Handling float results in some inacuracies, therefore the check cannot be with ==0.
                    bool isOnCircle = (Mathf.Abs(circleEquation) < 0.0001);
                    
                    if (isOnCircle)
                    {
                        ++countPointsOnCircle;
                        AveragePoint.x += points[i].x;
                        AveragePoint.y += points[i].y;
                    }
                }

                //check if enough points acquired are on the circle
                if ((countPointsOnCircle * 100 / points.Count) < accuracyPercent)
                {
                    return false;
                }

                AveragePoint.x = AveragePoint.x / countPointsOnCircle;
                AveragePoint.y = AveragePoint.y / countPointsOnCircle;

                //Check how close the true middle of all the points that stands on the circle is in comparison to the center of the circle 
                //The closer the true middle and the center are, the more evenly reparted the points are over the circle
                return Vector2.Distance(AveragePoint, originalCircle.Center) <= accuracyOffset;
            }
            else
            {
                //Check the distance between the first and last point
                if (Vector2.Distance(points[0], points[points.Count - 1]) > accuracyOffset)
                {
                    //The first and last points are too far away to be form a complete circle
                    return false;
                }

                Circle smallCircle = new Circle(originalCircle.Center, originalCircle.Radius - (accuracyOffset));
                Circle bigCircle = new Circle(originalCircle.Center, originalCircle.Radius + (accuracyOffset));

                bool isInsideBigCircle;
                bool isOutsideSmallCircle;

                for (int i = 0; i < points.Count; ++i)
                {
                    isInsideBigCircle = Mathf.Pow((points[i].x - bigCircle.Center.x), 2) + Mathf.Pow((points[i].y - bigCircle.Center.y), 2) - Mathf.Pow(bigCircle.Radius, 2) <= 0;
                    if (!isInsideBigCircle)
                        return false;

                    isOutsideSmallCircle = Mathf.Pow((points[i].x - smallCircle.Center.x), 2) + Mathf.Pow((points[i].y - smallCircle.Center.y), 2) - Mathf.Pow(smallCircle.Radius, 2) >= 0;
                    if (!isOutsideSmallCircle)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Return the list of all points that does not stand out between the 2 limit circles
        /// This method is used for debug purposes in order to visually represent which points caused the recognition algorithm to fail
        /// </summary>
        /// <param name="points"></param>
        /// <param name="accuracyPercent"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        [Obsolete]
        public static List<Vector2> GetIncorrectPointsCircle_DEBUG(List<Vector2> points, float accuracyPercent, CircleMethod method)
        {
            List<Vector2> incorrectPoints = new List<Vector2>();

            Circle originalCircle = GetCircle(points, method);

            if (originalCircle == null)
            {
                return incorrectPoints;
            }

            float accuracyOffset = originalCircle.Radius * 2 * (100 - accuracyPercent) / 100;
            Circle smallCircle = new Circle(originalCircle.Center, originalCircle.Radius - (accuracyOffset/2));
            Circle bigCircle = new Circle(originalCircle.Center, originalCircle.Radius + (accuracyOffset / 2));

            bool isInsideBigCircle = false;
            bool isOutsideSmallCircle = false;
            for (int i = 0; i < points.Count; ++i)
            {
                isInsideBigCircle = Mathf.Pow((points[i].x - bigCircle.Center.x), 2) + Mathf.Pow((points[i].y - bigCircle.Center.y), 2) - Mathf.Pow(bigCircle.Radius, 2) <= 0;
                if (!isInsideBigCircle)
                {
                    incorrectPoints.Add(points[i]);
                    continue;
                }

                isOutsideSmallCircle = Mathf.Pow((points[i].x - smallCircle.Center.x), 2) + Mathf.Pow((points[i].y - smallCircle.Center.y), 2) - Mathf.Pow(smallCircle.Radius, 2) >= 0;
                if (!isOutsideSmallCircle)
                {
                    incorrectPoints.Add(points[i]);
                    continue;
                }
            }

            return incorrectPoints;
        }
    }
}
