namespace SampleBadTests;

public class BeforeClassThrowsTests
{
    [BeforeClass]
    public static void SetupAll() => throw new InvalidOperationException("boom: BeforeClass");

    [Test]
    public void TestBody_IsFine() { }
}

public class BeforeThrowsTests
{
    [Before]
    public void Setup() => throw new Exception("boom: Before");

    [Test]
    public void TestBody_IsFine() { }
}

public class AfterThrowsTests
{
    [After]
    public void Teardown() => throw new Exception("boom: After");

    [Test]
    public void TestBody_Passes() { }
}

public class AfterClassThrowsTests
{
    [AfterClass]
    public static void TearDownAll() => throw new Exception("boom: AfterClass");

    [Test]
    public void TestBody_Passes() { }
}

public class ConstructorThrowsTests
{
    public ConstructorThrowsTests() => throw new Exception("boom: ctor");

    [Test]
    public void NeverRuns() { }
}

public class ControlFailures
{
    [Test]
    public void NormalFail() => throw new Exception("boom: test body (should be FAIL, not ERROR)");

    [Test(Expected = typeof(InvalidOperationException))]
    public void ExpectedException_Passes() => throw new InvalidOperationException("expected");
}
