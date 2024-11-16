using AutoSensorMonitor.Hardware;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

namespace AutoSensorMonitor.Tests.Unit.Hardware;

public class AdcFactoryTests
{
    [Fact]
    public void CreateAdc_WhenCalled_ShouldReturnAdcInstance()
    {
        // Arrange
        var mockFactoryLogger = new Mock<ILogger<AdcFactory>>();
        var mockAdcLogger = new Mock<ILogger<Adc>>();
        var factory = new AdcFactory(mockFactoryLogger.Object, mockAdcLogger.Object);

        // Act
        var adc = factory.CreateAdc();

        // Assert
        Assert.NotNull(adc);
        Assert.IsType<MockAdc>(adc);
    }
}
