using Microsoft.FSharp.Core;
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

    [Theory]
    [InlineData(
        false,
        new[] {"project_1"},
        new[] {"publish", "-Z", "package-workspace", "--package", "project_1", "--allow-dirty"})]
    [InlineData(
        true,
        new[] {"project_1"},
        new[] {"publish", "--all-features", "-Z", "package-workspace", "--package", "project_1", "--allow-dirty"})]
    [InlineData(
        false,
        new[] {"project_1", "project_2"},
        new[] {"publish", "-Z", "package-workspace", "--package", "project_1", "--package", "project_2", "--allow-dirty"})]
    [InlineData(
        true,
        new[] {"project_1", "project_2"},
        new[] {"publish", "--all-features", "-Z", "package-workspace", "--package", "project_1", "--package", "project_2", "--allow-dirty"})]
    [InlineData(
        false,
        new[] {"project_1", "project_2", "project_3"},
        new[] {"publish", "-Z", "package-workspace", "--package", "project_1", "--package", "project_2", "--package", "project_3", "--allow-dirty"})]
    [InlineData(
        true,
        new[] {"project_1", "project_2", "project_3"},
        new[] {"publish", "--all-features", "-Z", "package-workspace", "--package", "project_1", "--package", "project_2", "--package", "project_3", "--allow-dirty"})]
    public async Task RunCargoPublishWithArgsWithMultipleCrates(bool allFeatures, string[] crates, string[] expected)
    {
        var ctx = Context();
        var cfg = Config(allFeatures: allFeatures, crates: crates.ToList());
        var api = new Mock<ExternalApi.IExternalApi>();

        foreach (var crate in crates)
        {
            api
                .Setup(a => a.readFile($"./{crate}/Cargo.toml"))
                .Returns($"""
                        FoobarCargo
                        name = "{crate}"
                        version = "0.1.0"
                        Other stuff.
                        """.AsAsync());
            api
                .Setup(a => a.writeFile($"./{crate}/Cargo.toml", It.IsAny<string>()))
                .Returns(Helpers.UnitAsync);
        }

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("", "", 0).AsAsync());

        await Publish.publish(api.Object, cfg.Object, ctx.Object).Run();

        api.Verify(a => a.exec(expected), Times.Once);
    }

    [Theory]
    [InlineData(new[] {"project_1"}, new[] {"-p", "project_0" })]
    [InlineData(new[] {"project_1"}, new[] {"--package", "project_0"})]
    [InlineData(new[] {"project_1"}, new[] {"-Z", "package-workspace"})]
    [InlineData(new[] {"project_1", "project_2"}, new[] {"-p", "project_0" })]
    [InlineData(new[] {"project_1", "project_2"}, new[] {"--package", "project_0"})]
    [InlineData(new[] {"project_1", "project_2"}, new[] {"-Z", "package-workspace"})]
    [InlineData(new[] {"project_1", "project_2", "project_3"}, new[] {"-p", "project_0"})]
    [InlineData(new[] {"project_1", "project_2", "project_3"}, new[] {"--package", "project_0"})]
    [InlineData(new[] {"project_1", "project_2", "project_3"}, new[] {"-Z", "package-workspace"})]
    public async Task ThrowsWhenRunPublishWithMultipleCratesAndProhibitedArgs(string[] crates, string[] args)
    {
        var ctx = Context();
        var cfg = Config(crates: crates.ToList(), publishArgs: args.ToList());
        var api = new Mock<ExternalApi.IExternalApi>();

        foreach (var crate in crates)
        {
            api
                .Setup(a => a.readFile($"./{crate}/Cargo.toml"))
                .Returns($"""
                        FoobarCargo
                        name = "{crate}"
                        version = "0.1.0"
                        Other stuff.
                        """.AsAsync());
            api
                .Setup(a => a.writeFile($"./{crate}/Cargo.toml", It.IsAny<string>()))
                .Returns(Helpers.UnitAsync);
        }

        var ex = await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => Publish.publish(api.Object, cfg.Object, ctx.Object).Run());

        Assert.Equal(
            $"'{string.Join(" ", args)}' flag is invalid to use with 'crates' configuration option.",
            ex.Message
        );
    }

    private static Mock<Config.PluginConfig> Config(
        bool allFeatures = false,
        bool check = true,
        bool publish = true,
        List<string>? checkArgs = null,
        List<string>? publishArgs = null,
        List<string>? crates = null)
    {
        var config = new Mock<Config.PluginConfig>();

        config.Setup(c => c.allFeatures).Returns(allFeatures);
        config.Setup(c => c.check).Returns(check);
        config.Setup(c => c.publish).Returns(publish);
        config.Setup(c => c.checkArgs).Returns((checkArgs ?? []).ToArray());
        config.Setup(c => c.publishArgs).Returns((publishArgs ?? []).ToArray());
        config.Setup(c => c.crates).Returns(crates?.ToArray() ?? FSharpOption<string[]>.None);

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
