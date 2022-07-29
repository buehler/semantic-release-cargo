const SemanticReleaseError = require('@semantic-release/error');
const { cargoExecutable } = require('./utils');

/**
 * @typedef {Object} PluginConfig
 * @property {string?} executable the executable that shall be called.
 * @property {string?} publishArgs additional arguments for the publish call.
 */

/**
 * Publish the current package to the crate registry.
 *
 * @param {PluginConfig} pluginConfig
 * @param {import('semantic-release').Context} context
 */
module.exports = async ({ executable, publishArgs }, { logger }) => {
  const { execa } = await import('execa');

  logger.info('Publish cargo package.');
  const { stderr, exitCode } = await execa(cargoExecutable(executable), ['publish', '--allow-dirty', publishArgs]);
  if (exitCode !== 0) {
    throw new SemanticReleaseError('Cargo check failed', 'ECARGOCHECK', stderr);
  }
};
