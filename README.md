# semantic release cargo

Semantic release plugin to publish cargo packages.
Login with a cargo.io registry token and publish your crate.

## Configuration

### Environment

- `CARGO_REGISTRY_TOKEN`: required token that is used to login against crates.io

### Options

- `allFeatures`: Boolean that attaches `--all-features` to the cargo commands (defaults to `false`)
- `check`: Boolean that defines if `cargo check` is executed (defaults to `true`)
- `checkArgs`: Array of strings that contains additional arguments for `cargo check`
- `publish`: Boolean that defines if `cargo publish` is executed (defaults to `true`)
- `publishArgs`: Array of strings that contains additional arguments for `cargo publish`

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
