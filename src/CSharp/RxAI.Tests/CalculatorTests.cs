namespace RxAI.Tests;

public class CalculatorTests
{
    [Fact]
    public void Add_WhenGiven5And5_Returns10()
    {
        // Arrange
        int a = 5;
        int b = 5;
        int expected = 10;

        // Act
        int result = Calculator.Add(a, b);

        // Assert
        Assert.Equal(expected, result);
    }
}
