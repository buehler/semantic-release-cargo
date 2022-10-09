import { execa } from 'execa';
import { Context } from 'semantic-release';
import { cargoExecutable, PluginConfig, SemanticReleaseError } from './utils.js';

/**
 * Publish the current package to the crate registry.
 */
export default async ({ executable, allFeatures = false, publishArgs = [] }: PluginConfig, { logger }: Context) => {
  logger.info('Publish cargo package.');
  if (allFeatures && !publishArgs.includes('--all-features')) {
    publishArgs.push('--all-features');
  }
  const { stderr, exitCode } = await execa(cargoExecutable(executable), ['publish', '--allow-dirty', ...publishArgs]);
  if (exitCode !== 0) {
    throw new SemanticReleaseError('Cargo publish failed', 'ECARGOPUBLISH', stderr);
  }
};
