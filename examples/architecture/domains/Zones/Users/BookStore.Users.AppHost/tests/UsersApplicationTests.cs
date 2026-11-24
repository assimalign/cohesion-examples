// [proposed] Stage-1 validation as a unit test (v2 §7.5): emit one platform's view of the users application and assert
// on what that environment would receive. The AppHost is a compiler front end, so its output is testable without a
// cluster or a ResourceManager. References to other applications' resources are not resolved here; admission does that.
//
// [proposed] Everything from Fabric.Testing (ApplicationEmitter, ApplicationView, ResourceView), the Projects member in
// a test project, and the spec paths: no document format is fixed yet, so a path mirrors the declaration's member
// (Scale is "scale", Storage is "storage").

using System.Threading.Tasks;

using Assimalign.Cohesion.Fabric.Providers;                   // Platforms and ResourceLock: the one AppHost namespace
using Assimalign.Cohesion.Fabric.Testing;

using Shouldly;

using Xunit;

namespace BookStore.Users.AppHost.Tests;

public sealed class UsersApplicationTests
{
    [Fact]
    public async Task KubernetesView_HasNoDiagnostics_AndCarriesWhatTheAppHostDeclares()
    {
        // Act
        ApplicationView view = await ApplicationEmitter.EmitAsync(
            Projects.BookStore_Users_AppHost, Platforms.Kubernetes, TestContext.Current.CancellationToken);

        // Assert
        view.Diagnostics.ShouldBeEmpty();
        view.Excluded.ShouldBeEmpty();

        // No node pool, storage class, request or limit is asserted: the type's class and size are names, and the
        // environment maps them (placement.classes, placement.sizes). None of them is in the document.
        ResourceView api = view.Resource("Cohesion.Web/applications/users-api");
        api.Spec.GetInt32("scale").ShouldBe(2);
        api.References.ShouldContain(
            reference => reference.Target.EndsWith("/identityHubs/identity") && reference.RequiresBind);

        ResourceView database = view.Resource("Cohesion.Database/databases/users-db");
        database.Locks.ShouldContain(ResourceLock.CannotDelete);
        database.Spec.GetString("storage").ShouldBe("10Gi");

        // A parameter resolves at admission, not at emit: the view holds a reference to it, never a value.
        ResourceView exposure = view.Resource("Cohesion.Networking/exposures/users-public");
        exposure.References.ShouldContain(reference => reference.Target.EndsWith("/parameters/publicHost"));

        view.Resource("Kubernetes.Policy/podDisruptionBudgets/users-api").ShouldNotBeNull();
    }

    [Fact]
    public async Task InProcessView_LeavesOutKubernetesOnlyResources_AndKeepsTheDeclaredScale()
    {
        // Act
        ApplicationView view = await ApplicationEmitter.EmitAsync(
            Projects.BookStore_Users_AppHost, Platforms.InProcess, TestContext.Current.CancellationToken);

        // Assert
        view.Diagnostics.ShouldBeEmpty();
        view.Excluded.ShouldContain("Kubernetes.Policy/podDisruptionBudgets/users-api");

        // One instance in process is a rule of the platform, printed by every local run. No line of the AppHost and no
        // field of the document says so, which is why the scale is still the declared one.
        ResourceView api = view.Resource("Cohesion.Web/applications/users-api");
        api.Spec.GetInt32("scale").ShouldBe(2);
    }
}
