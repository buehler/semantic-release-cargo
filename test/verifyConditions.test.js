import { jest } from '@jest/globals';

const context = (env = {}) => ({
  env,
  logger: {
    info: jest.fn(),
    debug: jest.fn(),
    error: jest.fn(),
  },
});

describe('verifyConditions', () => {
  it('should refuse if cargo is invalid', async () => {
    jest.unstable_mockModule('../dist/Cargo.js', () => ({
      cargoExecutable: 'cargo',
      exec: jest.fn(() => {
        throw new Error('Cargo.exe is invalid');
      }),
    }));

    const {
      export$: { verifyConditions },
    } = await import('../dist/Program.js');

    expect(() => verifyConditions({}, context())).rejects.toMatchObject({
      'code@3': 'ECARGOEXECUTABLE',
    });
  });

  it('should print the cargo version number', async () => {
    jest.unstable_mockModule('../dist/Cargo.js', () => ({
      cargoExecutable: 'cargo',
      exec: jest.fn(() => ['cargo 1.0.0', '', 0]),
    }));

    const {
      export$: { verifyConditions },
    } = await import('../dist/Program.js');

    const ctx = context();
    const info = ctx.logger.info;
    try {
      await verifyConditions({}, ctx);
    } catch (e) {
      console.log(e);
    }
    expect(info).toHaveBeenCalledWith('Cargo version: 1.0');
  });
});
