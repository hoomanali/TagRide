using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TagRides.Shared.Game
{
    /// <summary>
    /// Used to track points and levels based on points
    /// </summary>
    public abstract class PointTracker : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected PointTracker(int baseLevel = 0)
        {
            this.baseLevel = baseLevel;
        }

        public int Points
        {
            get => points;
            set
            {
                if (value == points) return;

                int oldLevel = Level;

                points = value;
                InvokePropertyChanged("Points");
                InvokePropertyChanged("LevelProgress");

                if (oldLevel != Level)
                    InvokePropertyChanged("Level");
            }
        }

        public int Level => baseLevel + (int)PointsToLevel(points);
        public float LevelProgress
        {
            get
            {
                float level = PointsToLevel(points);
                return level - (int)level;
            }
        }

        abstract protected float PointsToLevel(int points);

        private int points = 0;
        private readonly int baseLevel;

        protected void InvokePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    /// <summary>
    /// Linear conversion from points to level
    /// </summary>
    public class LinearPointTracker : PointTracker
    {
        public LinearPointTracker(int pointsPerLevel, int baseLevel = 0)
            : base(baseLevel)
        {
            this.pointsPerLevel = pointsPerLevel;
        }

        public int PointsPerLevel => pointsPerLevel;

        protected override float PointsToLevel(int points)
        {
            return (float)points / pointsPerLevel;
        }

        readonly int pointsPerLevel;
    }
}
