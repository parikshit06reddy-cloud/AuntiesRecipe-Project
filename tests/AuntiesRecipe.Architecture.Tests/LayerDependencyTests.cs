using System.Reflection;
using System.Xml.Linq;

namespace AuntiesRecipe.Architecture.Tests;

/// <summary>
/// Validates that the enterprise layered architecture dependency rules are not violated.
/// These tests run in CI and will fail the build if any layer references a forbidden layer.
/// </summary>
public class LayerDependencyTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "AuntiesRecipe.sln")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? throw new InvalidOperationException("Could not locate AuntiesRecipe.sln");
    }

    private static HashSet<string> GetProjectReferences(string projectRelativePath)
    {
        var csprojPath = Path.Combine(SolutionRoot, projectRelativePath);
        var doc = XDocument.Load(csprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        return doc.Descendants(ns + "ProjectReference")
            .Select(e =>
            {
                var include = e.Attribute("Include")?.Value ?? "";
                var normalized = include.Replace('\\', '/');
                return Path.GetFileNameWithoutExtension(normalized);
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> GetAssemblyReferences(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? "")
            .Where(n => n.StartsWith("AuntiesRecipe", StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // ── Domain Layer: ZERO project references ──

    [Fact]
    public void Domain_has_no_project_references()
    {
        var refs = GetProjectReferences("src/AuntiesRecipe.Domain/AuntiesRecipe.Domain.csproj");

        refs.Should().BeEmpty("Domain is the innermost layer and must not reference any other project");
    }

    [Fact]
    public void Domain_assembly_does_not_reference_any_solution_assembly()
    {
        var assembly = typeof(Domain.Entities.Category).Assembly;
        var solutionRefs = GetAssemblyReferences(assembly);

        solutionRefs.Should().BeEmpty("Domain must have zero coupling to other solution assemblies");
    }

    [Fact]
    public void Domain_does_not_reference_EntityFrameworkCore()
    {
        var assembly = typeof(Domain.Entities.Category).Assembly;
        var allRefs = assembly.GetReferencedAssemblies().Select(a => a.Name ?? "");

        allRefs.Should().NotContain(
            name => name.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase),
            "Domain must not depend on EF Core -- use Fluent API in Infrastructure instead");
    }

    [Fact]
    public void Domain_does_not_reference_AspNetCore()
    {
        var assembly = typeof(Domain.Entities.Category).Assembly;
        var allRefs = assembly.GetReferencedAssemblies().Select(a => a.Name ?? "");

        allRefs.Should().NotContain(
            name => name.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase),
            "Domain must not depend on ASP.NET Core");
    }

    // ── Application Layer: references Domain ONLY ──

    [Fact]
    public void Application_references_only_Domain()
    {
        var refs = GetProjectReferences("src/AuntiesRecipe.Application/AuntiesRecipe.Application.csproj");

        refs.Should().BeEquivalentTo(
            ["AuntiesRecipe.Domain"],
            "Application may only reference Domain");
    }

    [Fact]
    public void Application_assembly_does_not_reference_Infrastructure_or_Web()
    {
        var assembly = typeof(Application.Abstractions.IMenuService).Assembly;
        var solutionRefs = GetAssemblyReferences(assembly);

        solutionRefs.Should().NotContain("AuntiesRecipe.Infrastructure",
            "Application must not reference Infrastructure (dependency inversion violation)");
        solutionRefs.Should().NotContain("AuntiesRecipe.Web",
            "Application must not reference Web");
    }

    [Fact]
    public void Application_does_not_reference_EntityFrameworkCore()
    {
        var assembly = typeof(Application.Abstractions.IMenuService).Assembly;
        var allRefs = assembly.GetReferencedAssemblies().Select(a => a.Name ?? "");

        allRefs.Should().NotContain(
            name => name.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase),
            "Application must not depend on EF Core -- it defines contracts only");
    }

    [Fact]
    public void Application_does_not_reference_AspNetCore()
    {
        var assembly = typeof(Application.Abstractions.IMenuService).Assembly;
        var allRefs = assembly.GetReferencedAssemblies().Select(a => a.Name ?? "");

        allRefs.Should().NotContain(
            name => name.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase),
            "Application must not depend on ASP.NET Core");
    }

    // ── Infrastructure Layer: references Application + Domain, NEVER Web ──

    [Fact]
    public void Infrastructure_references_Application_and_Domain_only()
    {
        var refs = GetProjectReferences("src/AuntiesRecipe.Infrastructure/AuntiesRecipe.Infrastructure.csproj");

        refs.Should().BeEquivalentTo(
            ["AuntiesRecipe.Application", "AuntiesRecipe.Domain"],
            "Infrastructure may reference Application and Domain only");
    }

    [Fact]
    public void Infrastructure_assembly_does_not_reference_Web()
    {
        var assembly = typeof(Infrastructure.DependencyInjection).Assembly;
        var solutionRefs = GetAssemblyReferences(assembly);

        solutionRefs.Should().NotContain("AuntiesRecipe.Web",
            "Infrastructure must never reference Web (upward dependency violation)");
    }

    // ── Web Layer: references Application + Infrastructure ──

    [Fact]
    public void Web_references_Application_and_Infrastructure_only()
    {
        var refs = GetProjectReferences("src/AuntiesRecipe.Web/AuntiesRecipe.Web.csproj");

        var solutionProjectRefs = refs
            .Where(r => r.StartsWith("AuntiesRecipe", StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        solutionProjectRefs.Should().BeEquivalentTo(
            ["AuntiesRecipe.Application", "AuntiesRecipe.Infrastructure"],
            "Web should reference Application (contracts) and Infrastructure (composition root wiring)");
    }

    // ── Cross-cutting: No circular dependencies ──

    [Fact]
    public void No_circular_dependency_between_Application_and_Infrastructure()
    {
        var appRefs = GetProjectReferences("src/AuntiesRecipe.Application/AuntiesRecipe.Application.csproj");

        appRefs.Should().NotContain("AuntiesRecipe.Infrastructure",
            "Application referencing Infrastructure would create a circular dependency");
    }

    // ── Structural conventions ──

    [Fact]
    public void Application_interfaces_live_in_Abstractions_folder()
    {
        var applicationTypes = typeof(Application.Abstractions.IMenuService).Assembly.GetTypes();

        var serviceInterfaces = applicationTypes
            .Where(t => t.IsInterface && t.Name.StartsWith("I") && t.Name.EndsWith("Service"));

        foreach (var iface in serviceInterfaces)
        {
            iface.Namespace.Should().Be(
                "AuntiesRecipe.Application.Abstractions",
                $"service interface {iface.Name} must live in Application.Abstractions namespace");
        }
    }

    [Fact]
    public void Infrastructure_services_implement_Application_interfaces()
    {
        var infraAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
        var appAssembly = typeof(Application.Abstractions.IMenuService).Assembly;

        var appServiceInterfaces = appAssembly.GetTypes()
            .Where(t => t.IsInterface
                        && t.Namespace == "AuntiesRecipe.Application.Abstractions"
                        && t.Name.EndsWith("Service"))
            .ToList();

        var infraServiceTypes = infraAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace == "AuntiesRecipe.Infrastructure.Services")
            .ToList();

        foreach (var iface in appServiceInterfaces)
        {
            infraServiceTypes.Should().Contain(
                t => iface.IsAssignableFrom(t),
                $"Application interface {iface.Name} must have an implementation in Infrastructure.Services");
        }
    }

    [Fact]
    public void Domain_entities_are_plain_classes_without_framework_attributes()
    {
        var domainAssembly = typeof(Domain.Entities.Category).Assembly;
        var entityTypes = domainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace?.Contains("Entities") == true);

        foreach (var entity in entityTypes)
        {
            var properties = entity.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var attrs = prop.GetCustomAttributes();
                foreach (var attr in attrs)
                {
                    var attrNamespace = attr.GetType().Namespace ?? "";
                    attrNamespace.Should().NotContain("EntityFrameworkCore",
                        $"{entity.Name}.{prop.Name} must not use EF Core attributes in Domain");
                    attrNamespace.Should().NotContain("ComponentModel.DataAnnotations",
                        $"{entity.Name}.{prop.Name} must not use DataAnnotations in Domain");
                }
            }
        }
    }

    [Fact]
    public void Application_DTOs_are_sealed_records()
    {
        var appAssembly = typeof(Application.Abstractions.IMenuService).Assembly;
        var dtoTypes = appAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Dto") && t.IsPublic);

        foreach (var dto in dtoTypes)
        {
            dto.IsSealed.Should().BeTrue($"DTO {dto.Name} should be sealed");

            var isRecord = dto.GetMethods().Any(m => m.Name == "<Clone>$");
            isRecord.Should().BeTrue($"DTO {dto.Name} should be a record type");
        }
    }
}
