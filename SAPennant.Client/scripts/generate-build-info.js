const fs = require('fs');
const { execSync } = require('child_process');

const info = {
  date: new Date().toLocaleDateString('en-AU', { day: '2-digit', month: 'short', year: 'numeric' }),
  time: new Date().toLocaleTimeString('en-AU', { hour: '2-digit', minute: '2-digit' }),
  commit: execSync('git rev-parse --short HEAD').toString().trim(),
};

fs.writeFileSync(
  'src/environments/build-info.ts',
  `export const buildInfo = ${JSON.stringify(info, null, 2)} as const;\n`
);