const assert = require('node:assert/strict');
const { unlink, writeFile } = require('node:fs/promises');
const test = require('node:test');
const { stub } = require('sinon');
const verifyConditions = require('../lib/verifyConditions');

const context = (env = {}) => ({
  env,
  logger: {
    info: stub(),
    error: stub(),
  },
});

test('throws on executable not found', async () => {
  await assert.rejects(verifyConditions({ executable: 'foobar' }, context()));
});

test('throws on no registry token', async () => {
  await assert.rejects(verifyConditions({}, context()));
});

test('throws on no cargo toml file', async () => {
  await assert.rejects(verifyConditions({}, context()));
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
