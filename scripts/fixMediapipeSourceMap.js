const fs = require('fs');
const path = require('path');

const targetPath = path.join(
  __dirname,
  '..',
  'node_modules',
  '@mediapipe',
  'tasks-vision',
  'vision_bundle_mjs.js.map',
);

const mapPayload = {
  version: 3,
  file: 'vision_bundle_mjs.js',
  sources: [],
  names: [],
  mappings: '',
};

function ensureSourceMap() {
  if (fs.existsSync(targetPath)) {
    console.log(`[fix-mediapipe-sourcemap] source map already exists: ${targetPath}`);
    return;
  }

  fs.mkdirSync(path.dirname(targetPath), { recursive: true });
  fs.writeFileSync(targetPath, JSON.stringify(mapPayload), 'utf8');
  console.log(`[fix-mediapipe-sourcemap] wrote stub source map: ${targetPath}`);
}

ensureSourceMap();
