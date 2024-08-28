import assert from 'node:assert/strict';
import { afterEach, describe, it, mock } from 'node:test';

const context = (env = {}) => ({
  env,
  logger: {
    info: mock.fn(),
    debug: mock.fn(),
    error: mock.fn(),
  },
});

describe('verifyConditions', () => {
  afterEach(() => mock.reset());

  // it('should refuse if cargo is invalid', async (t) => {
  //   const m = t.mock.module('../dist/Cargo.js', {
  //     namedExports: {
  //       cargoExecutable: 'cargo',
  //       exec: () => {
  //         throw new Error('Test Case');
  //       },
  //     },
  //   });
  //   t.after(() => m.restore());

  //   const s = await import('../dist/Program.js');

  //   const {
  //     export$: { verifyConditions },
  //   } = s;

  //   await assert.rejects(async () => verifyConditions({}, context()), {
  //     'code@3': 'ECARGOEXECUTABLE',
  //   });
  // });

  it('should print the cargo version number', async (t) => {
    const m = t.mock.module('../dist/Cargo.js', {
      namedExports: {
        cargoExecutable: 'cargo',
        exec: () => () => ['cargo 1.0.0', '', 0],
      },
    });
    t.after(() => m.restore());

    const {
      export$: { verifyConditions },
    } = await import('../dist/Program.js');

    const ctx = context();
    try {
      await verifyConditions({}, ctx);
    } catch (e) {
      console.log(e);
    }

    assert.equal(ctx.logger.info.mock.callCount(), 1);
  });
});
