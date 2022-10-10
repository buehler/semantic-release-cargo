import { readFile, writeFile } from 'node:fs/promises';
import { Context } from 'semantic-release';
import { cargoExecutable, exec, PluginConfig, SemanticReleaseError } from './utils';

/**
 * Prepare the package for release (perform "cargo check" and set version number).
 */
export default async (
  { executable, allFeatures = false, check = true, checkArgs = [] }: PluginConfig,
  { logger, nextRelease }: Context
) => {
  logger.info(`Write new release version (${nextRelease?.version}) into Cargo.toml.`);
  const tomlContent = await readFile('./Cargo.toml', 'utf8');
  await writeFile(
    './Cargo.toml',
    tomlContent.replace(/^version\s*=\s*(.*)/m, `version = "${nextRelease?.version}"`),
    'utf8'
  );

  if (check) {
    logger.info('Perform cargo check.');
    if (allFeatures && !checkArgs.includes('--all-features')) {
      checkArgs.push('--all-features');
    }

    const { stderr, exitCode } = await exec(cargoExecutable(executable), ['check', ...checkArgs]);
    if (exitCode !== 0) {
      throw new SemanticReleaseError('Cargo check failed', 'ECARGOCHECK', stderr);
    }
  }
};
