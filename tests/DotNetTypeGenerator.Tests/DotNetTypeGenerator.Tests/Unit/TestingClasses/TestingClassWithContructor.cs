namespace DotNetTypeGenerator.Tests.Unit.TestingClasses;

public class TestingClassWithContructor
{
    private string _test;

	public TestingClassWithContructor(string test)
	{
		_test = test;
	}

	public string Get() => _test;
}
