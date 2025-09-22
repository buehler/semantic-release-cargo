using Microsoft.FSharp.Core;
using Moq;
using SemanticReleaseCargo;

namespace SemanticReleaseCargoTests;

public class VerifyConditionsTests
{
    [Fact]
    public async Task RefusesInvalidCargoExecutable()
    {
        var (ctx, _) = Context();
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Throws(new Errors.SemanticReleaseError("ERROR", "ERROR", FSharpOption<string>.None));


        await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => VerifyConditions.verifyConditions(api.Object, Config(), ctx.Object).Run());
    }

    [Fact]
    public async Task PrintCargoVersion()
    {
        var (ctx, log) = Context();
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync());


        await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => VerifyConditions.verifyConditions(api.Object, Config(), ctx.Object).Run());

        log.Verify(l => l.info("Cargo version: cargo 1.0.0"), Times.Once);
    }

    [Fact]
    public async Task ThrowsWhenNoRegistryTokenIsPresent()
    {
        var (ctx, log) = Context();
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync);

        var ex = await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => VerifyConditions.verifyConditions(api.Object, Config(), ctx.Object).Run());

        Assert.Equal("CARGO_REGISTRY_TOKEN is not set.", ex.Message);
    }

    [Fact]
    public async Task ThrowsWhenNoRegistryTokenIsPresentAndNotPublishing()
    {
        var (ctx, log) = Context();
        var api = new Mock<ExternalApi.IExternalApi>();
        var config = new Mock<Config.PluginConfig>();
        config.Setup(c => c.publish).Returns(false);

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync);

        var ex = await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => VerifyConditions.verifyConditions(api.Object, config.Object, ctx.Object).Run());

        Assert.Equal("CARGO_REGISTRY_TOKEN is not set.", ex.Message);
    }

    [Fact]
    public async Task AllowsNoRegistryTokenIfNotPublishingAndCheckDisabled()
    {
        var (ctx, log) = Context();
        var api = new Mock<ExternalApi.IExternalApi>();
        var config = new Mock<Config.PluginConfig>();
        config.Setup(c => c.publish).Returns(false);
        config.Setup(c => c.alwaysVerifyToken).Returns(false);

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync);
        api
            .Setup(a => a.isReadable(It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);
        api
            .Setup(a => a.isWritable(It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);

        await VerifyConditions.verifyConditions(api.Object, config.Object, ctx.Object).Run();
    }

    [Fact]
    public async Task ExecutesLoginIntoRegistry()
    {
        var (ctx, log) = Context(new()
        {
            {"CARGO_REGISTRY_TOKEN", "token"},
        });
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(new[] {"--version"}))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync());

        api
            .Setup(a => a.exec(new[] {"login", "token"}))
            .Returns(new Tuple<string, string, int>("", "", 1).AsAsync());

        await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => VerifyConditions.verifyConditions(api.Object, Config(), ctx.Object).Run());

        api.Verify(a => a.exec(new[] {"login", "token"}), Times.Once);
    }

    [Fact]
    public async Task ThrowsOnInvalidLoginIntoRegistry()
    {
        var (ctx, log) = Context(new()
        {
            {"CARGO_REGISTRY_TOKEN", "token"},
        });
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(new[] {"--version"}))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync());

        api
            .Setup(a => a.exec(new[] {"login", "token"}))
            .Returns(new Tuple<string, string, int>("", "ERROR", 1).AsAsync());

        var ex = await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => VerifyConditions.verifyConditions(api.Object, Config(), ctx.Object).Run());

        Assert.Equal("Failed to login into registry.", ex.Message);
        Assert.Equal("ERROR", ex.details.Value);
    }

    [Fact]
    public async Task ThrowsOnNonReadableCargoFile()
    {
        var (ctx, log) = Context(new()
        {
            {"CARGO_REGISTRY_TOKEN", "token"},
        });
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(new[] {"--version"}))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync());

        api
            .Setup(a => a.exec(new[] {"login", "token"}))
            .Returns(new Tuple<string, string, int>("", "", 0).AsAsync());

        api
            .Setup(a => a.isReadable(It.IsAny<string>()))
            .Throws<Exception>();

        var ex = await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => VerifyConditions.verifyConditions(api.Object, Config(), ctx.Object).Run());

        Assert.Equal("Could not access ./Cargo.toml file.", ex.Message);
    }
    
    [Fact]
    public async Task ThrowsOnNonWritableCargoFile()
    {
        var (ctx, log) = Context(new()
        {
            {"CARGO_REGISTRY_TOKEN", "token"},
        });
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(new[] {"--version"}))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync());

        api
            .Setup(a => a.exec(new[] {"login", "token"}))
            .Returns(new Tuple<string, string, int>("", "", 0).AsAsync());

        api
            .Setup(a => a.isReadable(It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);
        
        api
            .Setup(a => a.isWritable(It.IsAny<string>()))
            .Throws<Exception>();

        var ex = await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => VerifyConditions.verifyConditions(api.Object, Config(), ctx.Object).Run());

        Assert.Equal("Could not access ./Cargo.toml file.", ex.Message);
    }
    
    [Fact]
    public async Task SuccessfullyTerminates()
    {
        var (ctx, log) = Context(new()
        {
            {"CARGO_REGISTRY_TOKEN", "token"},
        });
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(new[] {"--version"}))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync());

        api
            .Setup(a => a.exec(new[] {"login", "token"}))
            .Returns(new Tuple<string, string, int>("", "", 0).AsAsync());

        api
            .Setup(a => a.isReadable(It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);
        
        api
            .Setup(a => a.isWritable(It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);

        await VerifyConditions.verifyConditions(api.Object, Config(), ctx.Object).Run();
        api.Verify(a => a.isReadable("./Cargo.toml"), Times.Once);
        api.Verify(a => a.isWritable("./Cargo.toml"), Times.Once);
    }

    [Theory]
    [InlineData([new[] {"project_1"}])]
    [InlineData([new[] {"project_1", "project_2"}])]
    [InlineData([new[] {"project_1", "project_2", "project_3"}])]
    public async Task SuccessfullyTerminatesWithMultipleCrates(string[] crates)
    {
        var (ctx, log) = Context(new()
        {
            {"CARGO_REGISTRY_TOKEN", "token"},
        });
        var api = new Mock<ExternalApi.IExternalApi>();
        var config = new Mock<Config.PluginConfig>();
        config.Setup(c => c.crates).Returns(crates);

        api
            .Setup(a => a.exec(new[] {"--version"}))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync());

        api
            .Setup(a => a.exec(new[] {"login", "token"}))
            .Returns(new Tuple<string, string, int>("", "", 0).AsAsync());

        api
            .Setup(a => a.isReadable(It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);

        api
            .Setup(a => a.isWritable(It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);

        await VerifyConditions.verifyConditions(api.Object, config.Object, ctx.Object).Run();

        foreach (var crate in crates)
        {
            api.Verify(a => a.isReadable($"./{crate}/Cargo.toml"), Times.Once);
            api.Verify(a => a.isWritable($"./{crate}/Cargo.toml"), Times.Once);
        }
    }

    [Fact]
    public async Task ThrowsWhenEmptyCratesArrayProvided()
    {
        var (ctx, log) = Context(new()
        {
            {"CARGO_REGISTRY_TOKEN", "token"},
        });
        var api = new Mock<ExternalApi.IExternalApi>();
        var config = new Mock<Config.PluginConfig>();
        config.Setup(c => c.crates).Returns(Array.Empty<string>());

        api
            .Setup(a => a.exec(new[] {"--version"}))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync());

        api
            .Setup(a => a.exec(new[] {"login", "token"}))
            .Returns(new Tuple<string, string, int>("", "", 0).AsAsync());

        api
            .Setup(a => a.isReadable(It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);

        api
            .Setup(a => a.isWritable(It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);

        var ex = await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => VerifyConditions.verifyConditions(api.Object, config.Object, ctx.Object).Run());

        Assert.Equal("'crates' array should be non-empty, add at least one crate or remove this configuratoin option.", ex.Message);
    }

    private static Config.PluginConfig Config() => new Mock<Config.PluginConfig>().Object;

    private (Mock<SemanticRelease.VerifyReleaseContext>, Mock<SemanticRelease.Logger>) Context(
        Dictionary<string, string>? env = null)
    {
        var ctx = new Mock<SemanticRelease.VerifyReleaseContext>();
        var log = new Mock<SemanticRelease.Logger>();

        env ??= new();

        ctx.Setup(c => c.logger).Returns(log.Object);
        ctx.Setup(c => c.env).Returns(env.AsMap());

        return (ctx, log);
    }
}
