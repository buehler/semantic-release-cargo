import { rejects } from 'node:assert/strict';
import { unlink, writeFile } from 'node:fs/promises';
import { test } from 'node:test';
import { Context } from 'semantic-release';
import { stub } from 'sinon';
import verifyConditions from '../src/verifyConditions';

const context = (env = {}) =>
  ({
    env,
    logger: {
      info: stub(),
      error: stub(),
    },
  } as any as Context);

test('throws on wrong executable', async () => {
  await rejects(verifyConditions({ executable: 'foobar' }, context()), { code: 'ECARGOEXECUTABLE' });
});

test('throws on no registry token', async () => {
  await rejects(verifyConditions({}, context()), { code: 'ENOREGISTRYTOKEN' });
});

test('throws on no cargo toml file', async () => {
  await rejects(verifyConditions({}, context({ CARGO_REGISTRY_TOKEN: 'foobar' })), { code: 'ECARGOTOML' });
});

test('successfully checks conditions', async () => {
  try {
    await writeFile('./Cargo.toml', '', 'utf8');
    await verifyConditions({}, context({ CARGO_REGISTRY_TOKEN: 'foobar' }));
  } finally {
    try {
      await unlink('./Cargo.toml');
    } catch {}
  }
});
