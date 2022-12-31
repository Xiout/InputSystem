using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine.InputSystem.Utilities
{
    public class GeometryHelp
    {
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
        /// Take 3 points among the list and get the circle that is passing by all 3 points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Circle Get3PointsCircle(List<Vector2> points)
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

            return Get3PointsCircle(points[indexA], points[indexB], points[indexC]);
        }

        /// <summary>
        /// Get the circle that is passing by all 3 points given in parameter
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <returns></returns>
        public static Circle Get3PointsCircle(Vector2 point1, Vector2 point2, Vector2 point3)
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
        /// <param name="points"></param>
        /// <returns></returns>
        public static Circle GetCircleFurthestPoints(List<Vector2> points)
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
        public static Vector2[] FindFurthestPoints(List<Vector2> points)
        {
            Vector2[] furthestPoints = new Vector2[2];
            float longestDistance = 0;
            float currentDistance = 0;
            for(int i=0; i<points.Count; ++i)
            {
                for (int j = i+1; j < points.Count; ++j)
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

        public enum CircleMethod { FurthestPoints, ThreePoints }

        /// <summary>
        /// Check if all the points are in between 2 limit circles. 
        /// A first circle is calculated depending on the chosen method then the 2 limit circles are define from this first circle and the accuracyPercent.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="accuracyPercent"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static bool IsCircle(List<Vector2> points, float accuracyPercent, CircleMethod method)
        {
            Circle originalCircle = null;
            switch (method)
            {
                case CircleMethod.FurthestPoints:
                    originalCircle = GetCircleFurthestPoints(points);
                    break;
                case CircleMethod.ThreePoints:
                    originalCircle = Get3PointsCircle(points);
                    break;
            }

            if (originalCircle == null)
            {
                return false;
            }

            float accuracyOffset = originalCircle.Radius * 2 * (100 - accuracyPercent) / 100;
            Circle smallCircle = new Circle(originalCircle.Center, originalCircle.Radius - (accuracyOffset / 2));
            Circle bigCircle = new Circle(originalCircle.Center, originalCircle.Radius + (accuracyOffset / 2));

            bool isInsideBigCircle = false;
            bool isOutsideSmallCircle = false;

            for(int i=0; i<points.Count; ++i)
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

        /// <summary>
        /// Return the list of all points that does not stand out between the 2 limit circles
        /// </summary>
        /// <param name="points"></param>
        /// <param name="accuracyPercent"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static List<Vector2> GetIncorrectPointsCircle(List<Vector2> points, float accuracyPercent, CircleMethod method)
        {
            List<Vector2> incorrectPoints = new List<Vector2>();

            Circle originalCircle = null;
            switch (method)
            {
                case CircleMethod.FurthestPoints:
                    originalCircle = GetCircleFurthestPoints(points);
                    break;
                case CircleMethod.ThreePoints:
                    originalCircle = Get3PointsCircle(points);
                    break;
            }

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

        /// <summary>
        /// Find the 2 object that have the longest distance between and change their scale. 
        /// </summary>
        /// <param name="GOs"></param>
        public static void FindFurthestGameObject_DEBUG(List<GameObject> GOs)
        {
            GameObject go1 = null;
            GameObject go2 = null;

            float longestDistance = 0;
            float currentDistance = 0;
            for (int i = 0; i < GOs.Count; ++i)
            {
                for (int j = i + 1; j < GOs.Count; ++j)
                {
                    currentDistance = Vector2.Distance(GOs[i].transform.position, GOs[j].transform.position);
                    if (currentDistance > longestDistance)
                    {
                        go1 = GOs[i];
                        go2 = GOs[j];
                        longestDistance = currentDistance;
                    }
                }
            }

            go1.transform.localScale = new Vector3(0.05f, 0.05f, 1);
            go2.transform.localScale = new Vector3(0.05f, 0.05f, 1);
        }
    }
}
