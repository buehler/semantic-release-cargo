import { rejects } from 'node:assert/strict';
import { unlink, writeFile } from 'node:fs/promises';
import { test } from 'node:test';
import rewiremock from 'rewiremock/node';
import { Context } from 'semantic-release';
import { stub } from 'sinon';

const { afterEach } = require('node:test');

const context = (env = {}) =>
  ({
    env,
    logger: {
      info: stub(),
      error: stub(),
    },
  } as any as Context);

afterEach(() => {
  rewiremock.disable();
});

test('throws on wrong executable', async () => {
  const { default: verifyConditions } = require('../src/verifyConditions') as typeof import('../src/verifyConditions');
  await rejects(verifyConditions({ executable: 'foobar' }, context()), { code: 'ECARGOEXECUTABLE' });
});

test('throws on no registry token', async () => {
  const { default: verifyConditions } = require('../src/verifyConditions') as typeof import('../src/verifyConditions');
  await rejects(verifyConditions({}, context()), { code: 'ENOREGISTRYTOKEN' });
});

test('throws on incorrect cargo auth', async () => {
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      exec: stub()
        .onFirstCall()
        .resolves({ stdout: 'cargo 1.0.0' })
        .onSecondCall()
        .resolves({ exitCode: 1, stderr: 'foobar' }),
    })
    .callThrough();
  rewiremock.enable();

  const { default: verifyConditions } = require('../src/verifyConditions') as typeof import('../src/verifyConditions');
  await rejects(verifyConditions({}, context({ CARGO_REGISTRY_TOKEN: 'foobar' })), { code: 'ECARGOAUTH' });
});

test('throws on no cargo toml file', async () => {
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      exec: stub().onFirstCall().resolves({ stdout: 'cargo 1.0.0' }).onSecondCall().resolves({ exitCode: 0 }),
    })
    .callThrough();
  rewiremock.enable();

  const { default: verifyConditions } = require('../src/verifyConditions') as typeof import('../src/verifyConditions');
  await rejects(verifyConditions({}, context({ CARGO_REGISTRY_TOKEN: 'foobar' })), { code: 'ECARGOTOML' });
});

test('successfully checks conditions', async () => {
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      exec: stub().onFirstCall().resolves({ stdout: 'cargo 1.0.0' }).onSecondCall().resolves({ exitCode: 0 }),
    })
    .callThrough();
  rewiremock<typeof import('node:fs/promises')>(() => require('node:fs/promises'))
    .with({
      access: stub().resolves(),
    })
    .callThrough();
  rewiremock.enable();

  const { default: verifyConditions } = require('../src/verifyConditions') as typeof import('../src/verifyConditions');

  try {
    await writeFile('./Cargo.toml', '', 'utf8');
    await verifyConditions({}, context({ CARGO_REGISTRY_TOKEN: 'foobar' }));
  } finally {
    try {
      await unlink('./Cargo.toml');
    } catch {}
  }
});
