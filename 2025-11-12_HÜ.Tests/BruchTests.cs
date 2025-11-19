using System;
using Xunit;

public class BruchTests
{
    [Theory]
    [InlineData("1 1/2", "1", "2 1/2")]
    [InlineData("2 3/8", "1 5/6", "4 5/24")]
    [InlineData("1/2", "1/2", "1")]
    [InlineData("2", "2", "4")]
    public void Addition_ReturnsExpected(string a, string b, string expected)
    {
        var b1 = new Bruch(a);
        var b2 = new Bruch(b);
        var sum = b1.addiere(b2);
        Assert.Equal(expected, sum.ToString());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1 //2")]
    public void Constructor_InvalidInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => new Bruch(input));
    }
}
