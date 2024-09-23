using Microsoft.FSharp.Core;
using Moq;
using SemanticReleaseCargo;

namespace SemanticReleaseCargoTests;

public class PrepareTests
{
    [Fact]
    public async Task WritesNewVersionIntoCargoFile()
    {
        var ctx = Context();
        var cfg = Config(check: false);
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.readFile("./Cargo.toml"))
            .Returns("""
                     FoobarCargo
                     version = "0.1.0"
                     Other stuff.
                     """.AsAsync());

        api
            .Setup(a => a.writeFile("./Cargo.toml", It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);

        await Prepare.prepare(api.Object, cfg.Object, ctx.Object).Run();

        api.Verify(a => a.writeFile("./Cargo.toml", It.Is<string>(s => s.Contains("version = \"1.2.3\""))), Times.Once);
    }

    [Fact]
    public async Task DontPerformCheckIfDisabled()
    {
        var ctx = Context();
        var cfg = Config(check: false);
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.readFile("./Cargo.toml"))
            .Returns("version = \"0.1.0\"".AsAsync());

        api
            .Setup(a => a.writeFile("./Cargo.toml", It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);

        await Prepare.prepare(api.Object, cfg.Object, ctx.Object).Run();

        api.Verify(a => a.exec(It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task ThrowOnCheckError()
    {
        var ctx = Context();
        var cfg = Config();
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.readFile("./Cargo.toml"))
            .Returns("version = \"0.1.0\"".AsAsync());

        api
            .Setup(a => a.writeFile("./Cargo.toml", It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("", "error", 1).AsAsync());

        await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => Prepare.prepare(api.Object, cfg.Object, ctx.Object).Run());

        api.Verify(a => a.exec(new[] {"check"}), Times.Once);
    }

    [Theory]
    [InlineData(false, new string[] { }, new[] {"check"})]
    [InlineData(false, new[] {"--foo"}, new[] {"check", "--foo"})]
    [InlineData(true, new string[] { }, new[] {"check", "--all-features"})]
    [InlineData(true, new[] {"--foo"}, new[] {"check", "--foo", "--all-features"})]
    [InlineData(true, new[] {"--all-features", "--foo"}, new[] {"check", "--all-features", "--foo"})]
    public async Task RunCargoCheckWithArgs(bool allFeatures, string[] args, string[] expected)
    {
        var ctx = Context();
        var cfg = Config(allFeatures: allFeatures, checkArgs: args.ToList());
        var api = new Mock<ExternalApi.IExternalApi>();

        api
            .Setup(a => a.readFile("./Cargo.toml"))
            .Returns("version = \"0.1.0\"".AsAsync());

        api
            .Setup(a => a.writeFile("./Cargo.toml", It.IsAny<string>()))
            .Returns(Helpers.UnitAsync);

        api
            .Setup(a => a.exec(It.IsAny<string[]>()))
            .Returns(new Tuple<string, string, int>("", "", 0).AsAsync());

        await Prepare.prepare(api.Object, cfg.Object, ctx.Object).Run();

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

    private Mock<SemanticRelease.PrepareContext> Context(
        Dictionary<string, string>? env = null)
    {
        var ctx = new Mock<SemanticRelease.PrepareContext>();
        var log = new Mock<SemanticRelease.Logger>();

        env ??= new();

        ctx.Setup(c => c.logger).Returns(log.Object);
        ctx.Setup(c => c.env).Returns(env.AsMap());
        ctx.Setup(c => c.nextRelease).Returns(new Release());

        return ctx;
    }

    private record Release : SemanticRelease.NextRelease
    {
        public string version => "1.2.3";
        public string gitTag => null!;
        public string gitHead => null!;
        public string name => null!;
        public SemanticRelease.ReleaseType type => SemanticRelease.ReleaseType.Major;
        public string channel => null!;
        public FSharpOption<string> notes => FSharpOption<string>.None;
    }
}
