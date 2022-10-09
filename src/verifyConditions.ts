import { execa } from 'execa';
import { access, constants } from 'node:fs/promises';
import { Context } from 'semantic-release';
import { cargoExecutable, PluginConfig, SemanticReleaseError } from './utils.js';

/**
 * Checks if all necessary elements are in place.
 */
export default async ({ executable }: PluginConfig, { env, logger }: Context) => {
  try {
    const { stdout } = await execa(cargoExecutable(executable), ['--version']);
    logger.info(`Cargo version: ${stdout}`);
  } catch (e: any) {
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
  } catch (e: any) {
    logger.error('Could not access Cargo.toml');
    throw new SemanticReleaseError('Could not access Cargo.toml', 'ECARGOTOML', e.message);
  }
};
