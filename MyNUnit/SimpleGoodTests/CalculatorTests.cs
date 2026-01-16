namespace SampleGoodTests;

public class CalculatorTests
{
    private static int _beforeClassCount;
    private static int _afterClassCount;

    private static int _beforeCount;
    private static int _afterCount;

    private int _x;

    public static int BeforeClassCount => _beforeClassCount;
    public static int AfterClassCount => _afterClassCount;
    public static int BeforeCount => _beforeCount;
    public static int AfterCount => _afterCount;

    [BeforeClass]
    public static void BeforeAll()
    {
        _beforeClassCount++;
        _beforeCount = 0;
        _afterCount = 0;
    }

    [AfterClass]
    public static void AfterAll()
    {
        _afterClassCount++;
    }

    [Before]
    public void Setup()
    {
        _beforeCount++;
        _x = 10;
    }

    [After]
    public void TearDown()
    {
        _afterCount++;
    }

    [Test]
    public void Add_Passes()
    {
        if (_x + 5 != 15)
            throw new Exception("Add failed");
    }

    [Test(Expected = typeof(DivideByZeroException))]
    public void DivideByZero_IsExpected()
    {
        var z = 0;
        _ = 1 / z;
    }

    [Test(Ignore = "Not implemented yet")]
    public void Ignored_Test()
    {
        throw new Exception("Should never run");
    }

    [Test]
    public async Task Async_Test_Passes()
    {
        await Task.Delay(5);
        if (_x != 10)
            throw new Exception("Async setup failed");
    }
}
