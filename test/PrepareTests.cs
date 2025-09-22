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

    [Theory]
    [InlineData([new[] {"project_1"}])]
    [InlineData([new[] {"project_1", "project_2"}])]
    [InlineData([new[] {"project_1", "project_2", "project_3"}])]
    public async Task WritesNewVersionIntoMultipleCargoFiles(string[] crates)
    {
        var ctx = Context();
        var cfg = Config(check: false, crates: crates.ToList());
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

        await Prepare.prepare(api.Object, cfg.Object, ctx.Object).Run();

        foreach (var crate in crates)
        {
            api.Verify(
                a => a.writeFile(
                        $"./{crate}/Cargo.toml",
                        It.Is<string>(s => s.Contains("version = \"1.2.3\""))
                    ),
                Times.Once
            );
        }
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

    [Theory]
    [InlineData(
        false,
        new[] {"project_1" },
        new[] {"check", "-Z", "package-workspace", "--package", "project_1"})]
    [InlineData(
        true,
        new[] {"project_1" },
        new[] {"check", "-Z", "package-workspace", "--package", "project_1", "--all-features"})]
    [InlineData(
        false,
        new[] {"project_1", "project_2"},
        new[] {"check", "-Z", "package-workspace", "--package", "project_1", "--package", "project_2"})]
    [InlineData(
        true,
        new[] {"project_1", "project_2"},
        new[] {"check", "-Z", "package-workspace", "--package", "project_1", "--package", "project_2", "--all-features"})]
    [InlineData(
        false,
        new[] {"project_1", "project_2", "project_3"},
        new[] {"check", "-Z", "package-workspace", "--package", "project_1", "--package", "project_2", "--package", "project_3"})]
    [InlineData(
        true,
        new[] {"project_1", "project_2", "project_3"},
        new[] {"check", "-Z", "package-workspace", "--package", "project_1", "--package", "project_2", "--package", "project_3", "--all-features"})]
    public async Task RunCargoCheckWithArgsWithMultipleCrates(bool allFeatures, string[] crates, string[] expected)
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

        await Prepare.prepare(api.Object, cfg.Object, ctx.Object).Run();

        api.Verify(a => a.exec(expected), Times.Once);
    }

    [Theory]
    [InlineData(new[] {"project_1"}, new[] {"-p", "project_0"})]
    [InlineData(new[] {"project_1"}, new[] {"--package", "project_0" })]
    [InlineData(new[] {"project_1"}, new[] {"-Z", "package-workspace" })]
    [InlineData(new[] {"project_1", "project_2"}, new[] {"-p", "project_0"})]
    [InlineData(new[] {"project_1", "project_2"}, new[] {"--package", "project_0"})]
    [InlineData(new[] {"project_1", "project_2"}, new[] {"-Z", "package-workspace"})]
    [InlineData(new[] {"project_1", "project_2", "project_3"}, new[] {"-p", "project_0"})]
    [InlineData(new[] {"project_1", "project_2", "project_3"}, new[] {"--package", "project_0"})]
    [InlineData(new[] {"project_1", "project_2", "project_3"}, new[] {"-Z", "package-workspace"})]
    public async Task ThrowsWhenRunCheckWithMultipleCratesAndProhibitedArgs(string[] crates, string[] args)
    {
        var ctx = Context();
        var cfg = Config(crates: crates.ToList(), checkArgs: args.ToList());
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
        }

        foreach (var crate in crates)
        {
            api
                .Setup(a => a.writeFile($"./{crate}/Cargo.toml", It.IsAny<string>()))
                .Returns(Helpers.UnitAsync);
        }

        var ex = await Assert.ThrowsAsync<Errors.SemanticReleaseError>(
            () => Prepare.prepare(api.Object, cfg.Object, ctx.Object).Run());

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
