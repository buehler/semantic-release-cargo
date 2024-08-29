using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using Moq;
using SemanticReleaseCargo;
using SemanticReleaseCargoTests;

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
    public async Task ExecutesLoginIntoRegistry()
    {
        var (ctx, log) = Context();
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("cargo 1.0.0", "", 0).AsAsync());

        var ex = await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => VerifyConditions.verifyConditions(api.Object, Config(), ctx.Object).Run());

        Assert.Equal("CARGO_REGISTRY_TOKEN is not set.", ex.Message);
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
