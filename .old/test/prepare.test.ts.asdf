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

test('writes new relesae version into Cargo.toml', async () => {
  const write = stub().resolves();
  rewiremock<typeof import('fs/promises')>(() => require('fs/promises')).with({
    readFile: stub().resolves('version = "0.0.0"'),
    writeFile: write,
  });
  rewiremock.enable();

  const { default: prepare } = require('../src/prepare') as typeof import('../src/prepare');
  await prepare({ check: false }, context());

  ok(write.called);
  ok(write.calledWith('./Cargo.toml', 'version = "1.0.0"', 'utf8'));
});

test('performs cargo check if default', async () => {
  rewiremock<typeof import('fs/promises')>(() => require('fs/promises')).with({
    readFile: stub().resolves('version = "0.0.0"'),
    writeFile: stub().resolves(),
  });

  const exec = stub().resolves({ exitCode: 0 });
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      exec,
    })
    .callThrough();
  rewiremock.enable();

  const { default: prepare } = require('../src/prepare') as typeof import('../src/prepare');
  await prepare({}, context());

  ok(exec.called);
});

test('does not performs cargo check if configured false', async () => {
  rewiremock<typeof import('fs/promises')>(() => require('fs/promises')).with({
    readFile: stub().resolves('version = "0.0.0"'),
    writeFile: stub().resolves(),
  });

  const exec = stub().resolves({ exitCode: 0 });
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      exec,
    })
    .callThrough();
  rewiremock.enable();

  const { default: prepare } = require('../src/prepare') as typeof import('../src/prepare');
  await prepare({ check: false }, context());

  ok(!exec.called);
});

test('performs cargo check if configured true', async () => {
  rewiremock<typeof import('fs/promises')>(() => require('fs/promises')).with({
    readFile: stub().resolves('version = "0.0.0"'),
    writeFile: stub().resolves(),
  });

  const exec = stub().resolves({ exitCode: 0 });
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      exec,
    })
    .callThrough();
  rewiremock.enable();

  const { default: prepare } = require('../src/prepare') as typeof import('../src/prepare');
  await prepare({ check: true }, context());

  ok(exec.called);
});

test('throws if cargo check is not successful', async () => {
  rewiremock<typeof import('fs/promises')>(() => require('fs/promises')).with({
    readFile: stub().resolves('version = "0.0.0"'),
    writeFile: stub().resolves(),
  });

  const exec = stub().resolves({ exitCode: 1, stderr: 'err' });
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      exec,
    })
    .callThrough();
  rewiremock.enable();

  const { default: prepare } = require('../src/prepare') as typeof import('../src/prepare');
  await rejects(prepare({}, context()), { code: 'ECARGOCHECK' });
});

test('attaches --all-features if configured', async () => {
  rewiremock<typeof import('fs/promises')>(() => require('fs/promises')).with({
    readFile: stub().resolves('version = "0.0.0"'),
    writeFile: stub().resolves(),
  });

  const exec = stub().resolves({ exitCode: 0 });
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      cargoExecutable: () => 'cargo',
      exec,
    })
    .callThrough();
  rewiremock.enable();

  const { default: prepare } = require('../src/prepare') as typeof import('../src/prepare');
  await prepare({ allFeatures: true }, context());

  ok(exec.called);
  ok(exec.calledWith('cargo', ['check', '--all-features']));
});

test('attaches additional check args', async () => {
  rewiremock<typeof import('fs/promises')>(() => require('fs/promises')).with({
    readFile: stub().resolves('version = "0.0.0"'),
    writeFile: stub().resolves(),
  });

  const exec = stub().resolves({ exitCode: 0 });
  rewiremock<typeof import('../src/utils')>(() => require('../src/utils'))
    .with({
      cargoExecutable: () => 'cargo',
      exec,
    })
    .callThrough();
  rewiremock.enable();

  const { default: prepare } = require('../src/prepare') as typeof import('../src/prepare');
  await prepare({ checkArgs: ['foo', 'bar'] }, context());

  ok(exec.called);
  ok(exec.calledWith('cargo', ['check', 'foo', 'bar']));
});
