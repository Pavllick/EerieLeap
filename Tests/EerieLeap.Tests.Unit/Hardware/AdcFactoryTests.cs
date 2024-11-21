using EerieLeap.Hardware;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EerieLeap.Tests.Unit.Hardware;

public class AdcFactoryTests {
    [Fact]
    public void CreateAdc_WhenCalled_ShouldReturnAdcInstance() {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var factory = new AdcFactory(mockLogger.Object);

        // Act
        var adc = factory.CreateAdc();

        // Assert
        Assert.NotNull(adc);
        Assert.IsType<MockAdc>(adc);

        mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }
}
