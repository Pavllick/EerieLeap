using System.Collections.Generic;

namespace AutoSensorMonitor.Types;

public class ValueConverter
{
    public ConversionType Type { get; set; } = ConversionType.Linear;
    
    /// <summary>
    /// Mathematical expression that uses 'x' as the voltage input variable for ADC sensors,
    /// or references other sensor values using their IDs directly.
    /// Example: "2 * x + 5" for ADC sensors
    /// Example: "(sensor1_id + sensor2_id) / 2" for virtual sensors
    /// Only used when Type is Expression or Virtual
    /// </summary>
    public string? Expression { get; set; }
}
