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

        public static Circle GetCircleFurthestPoints(List<Vector2> points)
        {
            Vector2[] furthestPoints = FindFurthestPoints(points);

            Vector2 center = new Vector2((furthestPoints[0].x + furthestPoints[1].x) / 2, (furthestPoints[0].y + furthestPoints[1].y) / 2);
            float radius = Vector2.Distance(furthestPoints[0], furthestPoints[1])/2;
            return new Circle(center, radius);
        }

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

        public static bool IsCircle(List<Vector2> points, float accuracyPercent)
        {
            Circle originalCircle = GetCircleFurthestPoints(points);

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

        public static List<Vector2> GetIncorrectPointsCircle(List<Vector2> points, float accuracyPercent)
        {
            List<Vector2> incorrectPoints = new List<Vector2>();
            Circle originalCircle = GetCircleFurthestPoints(points);

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


        public static void FindFurthestGameObject(List<GameObject> GOs)
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
