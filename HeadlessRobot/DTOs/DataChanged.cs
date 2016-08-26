using System;
using System.Collections.Generic;

namespace HeadlessRobot.DTOs
{
    public sealed class DataChanged
    {
        public DataChanged()
        {
            IRSensorReadings = new List<IRSensorReading>();
        }
        public Exception Error { get; set; }
        public int AmbientLight { get; set; }
        public double ProximitySensorRaw { get; set; }
        public double ProximitySensorDistance { get; set; }
        public int SonarAngle { get; set; }
        public double SonarSensorRaw { get; set; }
        public double SonarSensorDistance { get; set; }
        // ReSharper disable once InconsistentNaming
        public IList<IRSensorReading> IRSensorReadings { get; set; }
    }
}
