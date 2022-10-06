const SemanticReleaseError = require('@semantic-release/error');
const { readFile, writeFile } = require('node:fs/promises');
const { cargoExecutable } = require('./utils');

/**
 * @typedef {Object} PluginConfig
 * @property {string?} executable the executable that shall be called.
 * @property {Boolean?} check if check is executed.
 * @property {Boolean?} allFeatures if check is executed.
 * @property {Array<string>?} checkArgs additional arguments for the publish call.
 */

/**
 * Prepare the package for release (perform "cargo check" and set version number).
 *
 * @param {PluginConfig} pluginConfig
 * @param {import('semantic-release').Context} context
 */
module.exports = async ({ executable, allFeatures = false, check = true, checkArgs = [] }, { logger, nextRelease }) => {
  const { execa } = await import('execa');

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

    const { stderr, exitCode } = await execa(cargoExecutable(executable), ['check', ...checkArgs]);
    if (exitCode !== 0) {
      throw new SemanticReleaseError('Cargo check failed', 'ECARGOCHECK', stderr);
    }
  }
};
