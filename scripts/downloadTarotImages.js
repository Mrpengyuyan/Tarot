const fs = require("fs-extra");
const path = require("path");

async function main() {
  const targetDir = path.join(__dirname, "../temp-images");
  await fs.ensureDir(targetDir);

  console.log("download-images script is a placeholder in this repository.");
  console.log(`Put raw tarot images under: ${targetDir}`);
  console.log("Then run: npm run optimize-images");
}

main().catch((err) => {
  console.error("download-images failed:", err);
  process.exitCode = 1;
});

