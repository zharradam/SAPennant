const fs = require('fs');
const { execSync } = require('child_process');

let commit = 'unknown';
try {
  commit = execSync('git rev-parse --short HEAD').toString().trim();
} catch (e) {
  commit = 'unknown';
}

const info = {
  version: `v${new Date().toLocaleDateString('en-AU', { day: '2-digit', month: 'short', year: 'numeric' })}`,
  commit,
};

fs.writeFileSync(
  'src/environments/build-info.ts',
  `export const buildInfo = ${JSON.stringify(info, null, 2)} as const;\n`
);