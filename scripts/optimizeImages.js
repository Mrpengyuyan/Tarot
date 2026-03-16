async function main() {
  console.log("Running image optimizer...");
  const ImageProcessor = require("./resizeImages");
  const processor = new ImageProcessor();
  await processor.run();
}

main().catch((err) => {
  console.error("optimize-images failed:", err);
  process.exitCode = 1;
});

