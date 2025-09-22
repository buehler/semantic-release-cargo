const registry = ['--registry', 'kellnr']

export default {
    branches: ['*'],
    plugins: [
        [
            '..',
            {
                loginArgs: registry,
                checkArgs: ['--all-targets'],
                publishArgs: ['--no-verify', ...registry],
                crates: ['project_1', 'project_2'],
            }
        ]
    ]
}
