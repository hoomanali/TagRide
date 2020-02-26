using System;
namespace TagRides.Shared.Utilities
{
    public readonly struct Vector2
    {
        public readonly double x;
        public readonly double y;

        public Vector2(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public double Magnitude => Math.Sqrt(SqrMagnitude);
        public double SqrMagnitude => x * x + y * y;

        public Vector2 Normalized
        {
            get
            {
                double m = Magnitude;
                return new Vector2(x / m, y / m);
            }
        }

        public double Dot(Vector2 other)
        {
            return x * other.x + y * other.y;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x + b.x, a.y + b.y);
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x - b.x, a.y - b.y);
        }

        public static Vector2 operator *(Vector2 a, double s)
        {
            return new Vector2(a.x * s, a.y * s);
        }

        public static Vector2 operator *(double s, Vector2 a)
        {
            return a * s;
        }

        public static Vector2 operator /(Vector2 a, double s)
        {
            return a * (1 / s);
        }
    }
}
