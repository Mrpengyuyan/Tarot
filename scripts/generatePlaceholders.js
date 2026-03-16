const fs = require("fs-extra");
const path = require("path");

async function main() {
  const dir = path.join(__dirname, "../public/images/tarot-cards");
  await fs.ensureDir(dir);

  const placeholderPath = path.join(dir, "default.jpg");
  if (!(await fs.pathExists(placeholderPath))) {
    const onePixelJpegBase64 =
      "/9j/4AAQSkZJRgABAQAAAQABAAD/2wCEAAkGBxAQEBUQEBAVFhUVFRUVFRUVFRUVFRUVFRUXFhUVFRUYHSggGBolHRUVITEhJSkrLi4uFx8zODMtNygtLisBCgoKDg0OGxAQGy0lICUtLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLf/AABEIAAEAAQMBIgACEQEDEQH/xAAXAAEBAQEAAAAAAAAAAAAAAAAAAQID/8QAFhEBAQEAAAAAAAAAAAAAAAAAAAER/9oADAMBAAIQAxAAAAHRk2f/xAAZEAEAAwEBAAAAAAAAAAAAAAABABEhMUH/2gAIAQEAAT8AotM9t6f/xAAVEQEBAAAAAAAAAAAAAAAAAAABEP/aAAgBAgEBPwCf/8QAFhEBAQEAAAAAAAAAAAAAAAAAABEB/9oACAEDAQE/AVP/2Q==";
    await fs.writeFile(
      placeholderPath,
      Buffer.from(onePixelJpegBase64, "base64"),
    );
    console.log(`Created placeholder file: ${placeholderPath}`);
  } else {
    console.log(`Placeholder already exists: ${placeholderPath}`);
  }
}

main().catch((err) => {
  console.error("generate-placeholders failed:", err);
  process.exitCode = 1;
});
