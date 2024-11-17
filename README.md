# EerieLeap 🌡️

EerieLeap is an open-source sensor monitoring system built with .NET Core. It provides a robust platform for reading, processing, and managing sensor data with support for both physical ADC sensors and virtual calculated sensors.

## Features

- 🔌 **ADC Integration**: Direct interface with ADC hardware for real-time sensor readings
- 🔄 **Virtual Sensors**: Create derived measurements using mathematical expressions
- 🌐 **RESTful API**: Full HTTP API for configuration and data access
- ⚡ **Real-time Updates**: Continuous sensor monitoring with configurable sampling rates
- 📊 **Flexible Configuration**: JSON-based configuration for ADC and sensor settings
- 🧮 **Expression Engine**: Advanced mathematical expressions for sensor value conversion and virtual sensor calculations

## Getting Started

### Prerequisites

- .NET 6.0 or later
- Compatible ADC hardware

### Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/eerie_leap.git
cd eerie_leap
```

2. Build the solution:
```bash
dotnet build
```

3. Run the application:
```bash
dotnet run --project EerieLeap
```

## Configuration

The system uses several configuration files:

### Application Settings
The `appsettings.json` file in the EerieLeap project contains application-level settings:
- `ConfigurationPath`: Path to the directory containing sensor and ADC configurations
- Standard ASP.NET Core settings (logging, etc.)

### Launch Settings
The `Properties/launchSettings.json` file configures the application's runtime environment:
- HTTP/HTTPS URLs and ports
- Environment variables
- Runtime profile settings

### Sensor and ADC Configuration
These files are stored in the directory specified by `ConfigurationPath`:

#### ADC Configuration
The `adc.json` file contains device-specific settings that control how the ADC hardware operates, including sampling rates, conversion modes, and power settings. The configuration structure depends on the specific ADC hardware being used.

#### Sensor Configuration
The `sensors.json` file defines two types of sensors:

#### Physical Sensors
Physical sensors represent direct connections to ADC channels. Their configuration includes:
- Channel assignment
- Voltage ranges for input
- Value ranges for output
- Units and sampling rates
- Custom conversion expressions using NCalc (voltage value referenced as 'x')

Both physical and virtual sensors use the [NCalc](https://github.com/ncalc/ncalc) expression evaluation engine:
- For physical sensors: transforms raw voltage readings ('x') into final values
- For virtual sensors: combines readings from other sensors (referenced as {sensor_id})
- Supports arithmetic operations, mathematical functions (Sin, Cos, Log, etc.), and constants (PI, E)

For detailed expression syntax and capabilities, refer to the [NCalc documentation](https://github.com/ncalc/ncalc/wiki/Documentation).

#### Virtual Sensors
Virtual sensors perform calculations based on readings from other sensors.

## API Endpoints

### Configuration Endpoints
- `GET /config` - Get complete configuration
- `GET /config/adc` - Get ADC configuration
- `POST /config/adc` - Update ADC configuration
- `GET /config/sensors` - Get all sensor configurations
- `POST /config/sensors` - Update sensor configurations
- `GET /sensors/{id}` - Get specific sensor configuration

### Reading Endpoints
- `GET /readings` - Get all current sensor readings
- `GET /readings/{id}` - Get reading for specific sensor

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [.NET Core](https://dotnet.microsoft.com/)
- Uses [NCalc](https://github.com/ncalc/ncalc) for expression evaluation
- Implements [Metalama](https://www.postsharp.net/metalama) for aspects
