const SemanticReleaseError = require('@semantic-release/error');
const { constants } = require('fs');
const { access } = require('fs/promises');
const { cargoExecutable } = require('./utils');

/**
 * @typedef {Object} PluginConfig
 * @property {string?} executable the executable that shall be called.
 */

/**
 * Checks if all necessary elements are in place.
 *
 * @param {PluginConfig} pluginConfig
 * @param {import('semantic-release').Context} context
 */
module.exports = async ({ executable }, { env, logger }) => {
  const { execa } = await import('execa');

  try {
    const { stdout } = await execa(cargoExecutable(executable), ['--version']);
    logger.info(`Cargo version: ${stdout}`);
  } catch (e) {
    logger.error(e);
    throw new SemanticReleaseError(
      `Cargo executable (${cargoExecutable(executable)}) not valid.`,
      'ECARGOEXECUTABLE',
      e.message
    );
  }

  if (!env['CARGO_REGISTRY_TOKEN']) {
    logger.error('CARGO_REGISTRY_TOKEN is not set');
    throw new SemanticReleaseError(
      'CARGO_REGISTRY_TOKEN is not set',
      'ENOREGISTRYTOKEN',
      'To access the crate registry, a registry token must be provided.'
    );
  }

  logger.info(`Login into crates.io`);
  const { exitCode, stderr } = await execa(cargoExecutable(executable), ['login', env['CARGO_REGISTRY_TOKEN']]);
  if (exitCode !== 0) {
    logger.error('Could not log into the crate registry');
    throw new SemanticReleaseError('Could not log into the crate registry', 'ECARGOAUTH', stderr);
  }

  try {
    await access('./Cargo.toml', constants.R_OK);
  } catch (e) {
    logger.error('Could not access Cargo.toml');
    throw new SemanticReleaseError('Could not access Cargo.toml', 'ECARGOTOML', e.message);
  }
};
