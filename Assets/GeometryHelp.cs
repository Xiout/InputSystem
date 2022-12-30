using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets
{
    class GeometryHelp
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
    }
}
