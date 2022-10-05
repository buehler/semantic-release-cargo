# semantic release cargo

Semantic release plugin to publish cargo packages.
Login with a cargo.io registry token and publish your crate.

## Configuration

### Environment

- `CARGO_REGISTRY_TOKEN`: required token that is used to login against crates.io

### Options

- `check`: Boolean that defines if `cargo check` is executed (defaults to `true`)
- `checkArgs`: Array of strings that contains additional arguments for `cargo check`
- `publishArgs`: Array of strings that contains additional arguments for `cargo publish`

#### Full Configuration Example

```jsonc
// .releaserc.json example
{
  "plugins": [
    [
      "semantic-release-cargo",
      {
        "check": true,
        "checkArgs": ["--all-features"],
        "publishArgs": ["--no-verify"]
      }
    ]
  ]
}
```
