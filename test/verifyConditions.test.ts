import { rejects } from 'node:assert/strict';
import { unlink, writeFile } from 'node:fs/promises';
import test from 'node:test';
import { Context } from 'semantic-release';
import { stub } from 'sinon';
import verifyConditions from '../src/verifyConditions.js';

const context = (env = {}) =>
  ({
    env,
    logger: {
      info: stub(),
      error: stub(),
    },
  } as any as Context);

test('throws on executable not found', async () => {
  await rejects(verifyConditions({ executable: 'foobar' }, context()));
});

test('throws on no registry token', async () => {
  await rejects(verifyConditions({}, context()));
});

test('throws on no cargo toml file', async () => {
  await rejects(verifyConditions({}, context()));
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
