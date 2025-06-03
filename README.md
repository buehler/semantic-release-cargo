# semantic release cargo

Semantic release plugin to publish cargo packages.
Login with a cargo.io registry token and publish your crate.

## Configuration

### Environment

- `CARGO_REGISTRY_TOKEN`: required token that is used to login against crates.io.<br>
This is not required or verified if both the `publish` and `alwaysVerifyToken` options are false.

### Options

- `allFeatures`: Boolean that attaches `--all-features` to the cargo commands (defaults to `false`)
- `check`: Boolean that defines if `cargo check` is executed (defaults to `true`)
- `checkArgs`: Array of strings that contains additional arguments for `cargo check`
- `publish`: Boolean that defines if `cargo publish` is executed (defaults to `true`)
- `publishArgs`: Array of strings that contains additional arguments for `cargo publish`
- `alwaysVerifyToken`: Boolean that causes `CARGO_REGISTRY_TOKEN` verification to be skipped if both it and `publish` are `false` (defaults to `true`)

#### Full Configuration Example

```jsonc
// .releaserc.json example
{
  "plugins": [
    [
      "semantic-release-cargo",
      {
        "allFeatures": true,
        "check": true,
        "checkArgs": ["--no-deps"],
        "publish": true,
        "publishArgs": ["--no-verify"]
      }
    ]
  ]
}
```
