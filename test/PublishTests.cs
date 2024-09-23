using Moq;
using SemanticReleaseCargo;

namespace SemanticReleaseCargoTests;

public class PublishTests
{
    [Fact]
    public async Task DoesNotPublishIfDisabled()
    {
        var ctx = Context();
        var cfg = Config(publish: false);
        var api = new Mock<ExternalApi.IExternalApi>();
        
        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("", "error", 1).AsAsync());

        await Publish.publish(api.Object, cfg.Object, ctx.Object).Run();

        api.Verify(a => a.exec(It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task ThrowOnPublishError()
    {
        var ctx = Context();
        var cfg = Config();
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("", "error", 1).AsAsync());

        await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => Publish.publish(api.Object, cfg.Object, ctx.Object).Run());

        api.Verify(a => a.exec(new[] {"publish", "--allow-dirty"}), Times.Once);
    }

    [Theory]
    [InlineData(false, new string[] { }, new[] {"publish", "--allow-dirty"})]
    [InlineData(false, new[] {"--foo"}, new[] {"publish", "--foo", "--allow-dirty"})]
    [InlineData(true, new string[] { }, new[] {"publish", "--all-features", "--allow-dirty"})]
    [InlineData(true, new[] {"--foo"}, new[] {"publish", "--foo", "--all-features", "--allow-dirty"})]
    [InlineData(true, new[] {"--all-features", "--foo"}, new[] {"publish", "--all-features", "--foo", "--allow-dirty"})]
    [InlineData(false, new[] {"--allow-dirty", "--foo"}, new[] {"publish", "--allow-dirty", "--foo"})]
    [InlineData(true, new[] {"--all-features", "--allow-dirty", "--foo"},
        new[] {"publish", "--all-features", "--allow-dirty", "--foo"})]
    public async Task RunCargoPublishWithArgs(bool allFeatures, string[] args, string[] expected)
    {
        var ctx = Context();
        var cfg = Config(allFeatures: allFeatures, publishArgs: args.ToList());
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("", "", 0).AsAsync());

        await Publish.publish(api.Object, cfg.Object, ctx.Object).Run();

        api.Verify(a => a.exec(expected), Times.Once);
    }

    private static Mock<Config.PluginConfig> Config(
        bool allFeatures = false,
        bool check = true,
        bool publish = true,
        List<string>? checkArgs = null,
        List<string>? publishArgs = null)
    {
        var config = new Mock<Config.PluginConfig>();

        config.Setup(c => c.allFeatures).Returns(allFeatures);
        config.Setup(c => c.check).Returns(check);
        config.Setup(c => c.publish).Returns(publish);
        config.Setup(c => c.checkArgs).Returns((checkArgs ?? []).ToArray());
        config.Setup(c => c.publishArgs).Returns((publishArgs ?? []).ToArray());

        return config;
    }

    private Mock<SemanticRelease.Context> Context(
        Dictionary<string, string>? env = null)
    {
        var ctx = new Mock<SemanticRelease.Context>();
        var log = new Mock<SemanticRelease.Logger>();

        env ??= new();

        ctx.Setup(c => c.logger).Returns(log.Object);
        ctx.Setup(c => c.env).Returns(env.AsMap());

        return ctx;
    }
}
