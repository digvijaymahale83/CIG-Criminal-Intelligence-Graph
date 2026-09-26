using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Tests.Architecture;

public class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotReference_Infrastructure_Or_API()
    {
        var domainAssembly = Assembly.Load("Domain");
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies().Select(a => a.Name);

        referencedAssemblies.Should().NotContain("Infrastructure");
        referencedAssemblies.Should().NotContain("API");
    }

    [Fact]
    public void Application_ShouldNotReference_Infrastructure_Or_API()
    {
        var applicationAssembly = Assembly.Load("Application");
        var referencedAssemblies = applicationAssembly.GetReferencedAssemblies().Select(a => a.Name);

        referencedAssemblies.Should().NotContain("Infrastructure");
        referencedAssemblies.Should().NotContain("API");
    }

    [Fact]
    public void Application_ShouldReference_Domain()
    {
        var applicationAssembly = Assembly.Load("Application");
        var referencedAssemblies = applicationAssembly.GetReferencedAssemblies().Select(a => a.Name);

        referencedAssemblies.Should().Contain("Domain");
    }
}
