using MicroCMS.E2E.Tests.Fixtures;
using Xunit;

namespace MicroCMS.E2E.Tests;

/// <summary>
/// Shared collection that allows all E2E test classes to use the same
/// <see cref="MicroCmsWebApplicationFactory"/> instance for the duration of the run.
/// The factory's <see cref="MicroCmsWebApplicationFactory.InitializeAsync"/> is called
/// once here; individual test classes implement <see cref="IAsyncLifetime"/> to await it.
/// </summary>
[CollectionDefinition("WebHost")]
public sealed class WebHostCollection : ICollectionFixture<MicroCmsWebApplicationFactory>;
