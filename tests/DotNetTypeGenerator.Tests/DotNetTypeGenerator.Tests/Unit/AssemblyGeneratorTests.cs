using System.Diagnostics;
using System.Reflection;
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

	[Fact]
	public void PersistsAssemblyByDefault()
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
		Assert.NotEmpty(result.Location);
	}

	[Fact]
	public void CanGenerateMemoryOnlyAssembly()
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

		var result = new AssemblyGenerator(persist: false).GenerateAssembly(code);

		Assert.Empty(result.Location);
    }

	[Fact]
	public void CanReferenceOutputOfFirstGeneration()
	{
        var firstCode =
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

		var secondCode =
			"""
				public class AnotherTestClass
				{
					public void RunThings()
					{
						var y = new TestClass();
						y.RunThings();
					}
				}
			""";

		var firstGenerator = new AssemblyGenerator();
		var firstAssembly = firstGenerator.GenerateAssembly(firstCode);

		var secondGenerator = new AssemblyGenerator();
		secondGenerator.ReferenceAssembly(firstAssembly);
		var secondAssembly = secondGenerator.GenerateAssembly(secondCode);
		var type = secondAssembly.GetExportedTypes().Single();

		Assert.Equal("AnotherTestClass", type.Name);
    }

	[Fact]
	public void AssemblyNameShouldnyContainExtension()
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
		var assemblyName = result.GetName().Name;
		var trimmedAssemblyName = Path.GetFileNameWithoutExtension(assemblyName);

		Assert.Equal(assemblyName, trimmedAssemblyName);
    }

	[Fact]
	public void AssemblyShouldContainVersionInfo()
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
		var versionInfo = FileVersionInfo.GetVersionInfo(result.Location);
		var fileVersion = versionInfo.FileVersion;

		Assert.NotNull(fileVersion);
    }

	[Fact]
	public void CanSetAssemblyContextFactory()
	{
		var myContext = new AssemblyContextForUnitTesting();
		var generator = new AssemblyGenerator(true, null, null, () => myContext);

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

		generator.GenerateAssembly(code);

		Assert.True(myContext.HasLoaded);
    }

	public class AssemblyContextForUnitTesting : CustomAssemblyLoadContext
	{
		public bool HasLoaded { get; set; }

		public AssemblyContextForUnitTesting()
		{
		}

		public AssemblyContextForUnitTesting(List<Assembly> assemblies) : base(assemblies)
		{
		}

        protected override Assembly? Load(AssemblyName assemblyName)
        {
			HasLoaded = true;

            return base.Load(assemblyName);
        }
    }

	[Collection(nameof(NotThreadSafeResourceCollection))]
    public class DefaultContextTests : IDisposable
    {
		private readonly ITestOutputHelper _testOutputHelper;
		private AssemblyGenerator _generator;
		private AssemblyContextForUnitTesting _myContext;

		public DefaultContextTests(ITestOutputHelper testOutputHelper)
		{
			_myContext = new();
			AssemblyGenerator.DefaultAssemblyLoadContextFactory = () => _myContext;
			_testOutputHelper = testOutputHelper;
			_generator = new AssemblyGenerator();
		}

		[Fact]
		public void CanSetDefaultAssemblyContextFactory()
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

			_generator.GenerateAssembly(code);

			Assert.True(_myContext.HasLoaded);
        }

        public void Dispose()
        {
			AssemblyGenerator.DefaultAssemblyLoadContextFactory = () => new CustomAssemblyLoadContext();
        }
    }
}
