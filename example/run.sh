#! /usr/bin/env sh

if ! npm start; then
    res=$?
    echo 'Failed to start kellnr service'
    exit $res
fi

setup() {
    npm run config # Temproary disable tag/commit signing, since it usually causes interactive password prompt.
    npm run commit # Generate dummy commit describing breaking change.
}

teardown() {
    npm run untag    # Remove release tag (local and remote).
    npm run unconfig # Reset signing settings to global .gitconfig values.
    npm run uncommit # Softly reset dummy commit.
    npm run restore  # Restore previous version of each Cargo.toml manifest.
}

setup

npm run release # Create release.
res=$?

teardown # Runs unconditionally: implies successfull or failed release process.

if [ $res -eq 0 ]; then
    npm run check
    res=$?
    if [ $res -eq 0 ]; then
        echo 'Crates were successfully pushed.'
    else
        echo 'Cannot find some or all publihed crates.'
    fi
else
    echo 'Failed to create release.'
fi

npm run stop
exit $res
