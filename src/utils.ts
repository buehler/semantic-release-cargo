export const cargoExecutable = (executable?: string) => (executable ?? process.platform === 'win32' ? 'cargo.exe' : 'cargo');

export type PluginConfig = {
  executable?: string;
  allFeatures?: boolean;
  check?: boolean;
  checkArgs?: string[];
  publishArgs?: string[];
};

export class SemanticReleaseError extends Error {
  public readonly semanticRelease = true;
  public readonly code: string;
  public readonly details?: string;

  constructor(message: string, code: string, details?: string) {
    super(message);
    Error.captureStackTrace(this, this.constructor);
    this.name = 'SemanticReleaseError';
    this.code = code;
    this.details = details;
  }
}
