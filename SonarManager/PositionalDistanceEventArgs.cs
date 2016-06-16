using System;

namespace SonarManager
{
    public class PositionalDistanceEventArgs : EventArgs
    {
        public int Angle { get; internal set; }
        public double Proximity { get; internal set; }
    }
}