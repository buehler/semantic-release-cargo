import { ok, rejects } from 'node:assert/strict';
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
    nextRelease: { version: '1.0.0' },
  } as any as Context);

afterEach(() => {
  rewiremock.disable();
});

test('performs cargo publish', async () => {
  const exec = stub().resolves({ exitCode: 0 });
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      exec,
    })
    .callThrough();
  rewiremock.enable();

  const { default: publish } = require('../src/publish') as typeof import('../src/publish');
  await publish({}, context());

  ok(exec.called);
});

test('attaches --all-features if configured', async () => {
  const exec = stub().resolves({ exitCode: 0 });
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      cargoExecutable: () => 'cargo',
      exec,
    })
    .callThrough();
  rewiremock.enable();

  const { default: publish } = require('../src/publish') as typeof import('../src/publish');
  await publish({ allFeatures: true }, context());

  ok(exec.called);
  ok(exec.calledWith('cargo', ['publish', '--all-features', '--allow-dirty']));
});

test('attaches additional publish args', async () => {
  const exec = stub().resolves({ exitCode: 0 });
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      cargoExecutable: () => 'cargo',
      exec,
    })
    .callThrough();
  rewiremock.enable();

  const { default: publish } = require('../src/publish') as typeof import('../src/publish');
  await publish({ publishArgs: ['foo', 'bar'] }, context());

  ok(exec.called);
  ok(exec.calledWith('cargo', ['publish', 'foo', 'bar', '--allow-dirty']));
});

test('throws if cargo check is not successful', async () => {
  const exec = stub().resolves({ exitCode: 1, stderr: 'err' });
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      exec,
    })
    .callThrough();
  rewiremock.enable();

  const { default: publish } = require('../src/publish') as typeof import('../src/publish');
  await rejects(publish({}, context()), { code: 'ECARGOPUBLISH' });
});
