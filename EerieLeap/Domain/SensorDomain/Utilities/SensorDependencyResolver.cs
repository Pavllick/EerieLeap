using System.ComponentModel.DataAnnotations;
using EerieLeap.Domain.SensorDomain.Models;
using EerieLeap.Utilities;

namespace EerieLeap.Domain.SensorDomain.Utilities;

public class SensorDependencyResolver {
    private readonly Dictionary<string, HashSet<string>> _dependencies = new();
    private readonly Dictionary<string, Sensor> _sensors = new();

    public void AddSensor([Required] Sensor sensor) {
        _sensors[sensor.Id.Value] = sensor;

        _dependencies[sensor.Id.Value] = ExpressionEvaluator
            .ExtractSensorIds(sensor.Configuration.ConversionExpression ?? string.Empty)
            .ToHashSet();
    }

    public IOrderedEnumerable<Sensor> GetProcessingOrder() {
        var visited = new HashSet<string>();
        var temp = new HashSet<string>();
        var order = new List<Sensor>();

        foreach (var sensorId in _sensors.Keys) {
            if (!visited.Contains(sensorId)) {
                if (HasCyclicDependency(sensorId, visited, temp, order)) {
                    throw new InvalidOperationException($"Cyclic dependency detected in sensor {sensorId}");
                }
            }
        }

        // Physical sensors should be processed first, followed by virtual sensors in dependency order
        return order
            .OrderBy(s => s.Configuration.Type == SensorType.Virtual)
            .ThenBy(s => order.IndexOf(s));
    }

    private bool HasCyclicDependency(string sensorId, HashSet<string> visited, HashSet<string> temp, List<Sensor> order) {
        if (temp.Contains(sensorId))
            return true;
        if (visited.Contains(sensorId))
            return false;

        temp.Add(sensorId);

        foreach (var dep in _dependencies[sensorId]) {
            if (!_sensors.ContainsKey(dep))
                throw new InvalidOperationException($"Sensor {sensorId} depends on non-existent sensor {dep}");

            if (HasCyclicDependency(dep, visited, temp, order))
                return true;
        }

        temp.Remove(sensorId);
        visited.Add(sensorId);
        order.Add(_sensors[sensorId]);
        return false;
    }
}
