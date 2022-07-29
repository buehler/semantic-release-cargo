module.exports = {
  cargoExecutable: (executable) => (executable ?? process.platform === 'win32' ? 'cargo.exe' : 'cargo'),
};
