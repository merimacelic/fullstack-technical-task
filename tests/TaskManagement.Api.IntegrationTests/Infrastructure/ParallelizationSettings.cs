// Force xUnit to run every collection in this assembly sequentially. The
// WebApplicationFactory<Program> machinery shares static state (Serilog's global
// logger, the Program entry point) that doesn't survive concurrent hosts, and
// Testcontainers-backed factories burn enough resources that parallelism is
// counterproductive even when it works.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
