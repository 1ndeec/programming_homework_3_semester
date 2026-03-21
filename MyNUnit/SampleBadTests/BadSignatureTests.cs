namespace SampleBadTests;

public class BadSignatureTests
{
    [Test]
    public static void StaticTest_IsInvalid() { }

    [Test]
    public int ReturnsInt_IsInvalid() => 123;

    [Test]
    public void HasParam_IsInvalid(int x) { }
}

public class BadHookSignatures
{
    [BeforeClass]
    public void BeforeClass_MustBeStatic() { }

    [AfterClass]
    public void AfterClass_MustBeStatic() { }
}
