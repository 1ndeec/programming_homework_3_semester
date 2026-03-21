namespace SampleGoodTests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : Attribute
{
    public Type? Expected { get; set; }
    public string? Ignore { get; set; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeClassAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterClassAttribute : Attribute { }
