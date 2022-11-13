using Xunit.Abstractions;

namespace DotNetTypeGenerator.Tests.Unit;

public class AssemblyGeneratorTests
{
    private readonly ITestOutputHelper _testOutputHelper;
	private AssemblyGenerator _generator;

    public AssemblyGeneratorTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper;
		_generator = new AssemblyGenerator();
	}

	[Fact]
	public void CanGenerateAssemblyFromCode()
	{
		var code =
            """
				public class TestClass
				{
					public void RunThings()
					{
						var x = 0;
						var y = 1;
			
						y = x + 10;
					}
				}
			""";

		var result = _generator.GenerateAssembly(code);
		var type = result.GetExportedTypes().Single();

		Assert.Equal("TestClass", type.Name);
	}
}
