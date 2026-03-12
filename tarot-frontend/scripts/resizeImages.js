const fs = require('fs-extra');
const path = require('path');
const sharp = require('sharp');

/**
 * 图片批量处理脚本
 * 将下载的原始图片转换为项目需要的多种尺寸
 */

const IMAGE_QUALITIES = {
  thumbnail: { width: 200, height: 333, suffix: '_thumb' },
  standard: { width: 400, height: 666, suffix: '_std' },
  high: { width: 800, height: 1332, suffix: '_hd' },
  ultra: { width: 1200, height: 2000, suffix: '_uhd' }
};

const INPUT_DIR = path.join(__dirname, '../temp-images');  // 临时文件夹，放置原始下载图片
const OUTPUT_DIR = path.join(__dirname, '../public/images/tarot-cards');

class ImageProcessor {
  constructor() {
    this.processedCount = 0;
    this.failedCount = 0;
  }

  async processImage(inputPath, outputDir, baseName) {
    try {
      console.log(`🔄 处理图片: ${baseName}`);

      // 确保输出目录存在
      await fs.ensureDir(outputDir);

      // 为每种质量生成图片
      for (const [quality, config] of Object.entries(IMAGE_QUALITIES)) {
        const outputPath = path.join(outputDir, `${baseName}${config.suffix}.jpg`);

        await sharp(inputPath)
          .resize(config.width, config.height, {
            fit: 'cover',
            position: 'center',
            background: { r: 255, g: 255, b: 255, alpha: 1 }
          })
          .jpeg({
            quality: 90,
            progressive: true
          })
          .toFile(outputPath);

        console.log(`  ✅ 生成 ${quality}: ${config.width}x${config.height}`);
      }

      this.processedCount++;
      console.log(`✅ 完成处理: ${baseName}\n`);

    } catch (error) {
      this.failedCount++;
      console.error(`❌ 处理失败 ${baseName}:`, error.message);
    }
  }

  async processMajorArcana() {
    console.log('🔮 处理大阿卡纳图片...\n');

    const majorArcanaDir = path.join(INPUT_DIR, 'major-arcana');
    const outputDir = path.join(OUTPUT_DIR, 'major-arcana');

    if (!(await fs.pathExists(majorArcanaDir))) {
      console.log(`⚠️  目录不存在: ${majorArcanaDir}`);
      console.log('📋 请先创建该目录并放入原始图片');
      return;
    }

    const files = await fs.readdir(majorArcanaDir);
    const imageFiles = files.filter(file =>
      /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(file)
    );

    if (imageFiles.length === 0) {
      console.log('⚠️  未找到图片文件');
      return;
    }

    for (const file of imageFiles) {
      const inputPath = path.join(majorArcanaDir, file);
      const baseName = path.parse(file).name;

      await this.processImage(inputPath, outputDir, baseName);
    }
  }

  async processMinorArcana() {
    console.log('🎴 处理小阿卡纳图片...\n');

    const suits = ['cups', 'wands', 'swords', 'pentacles'];

    for (const suit of suits) {
      const suitDir = path.join(INPUT_DIR, 'minor-arcana', suit);
      const outputDir = path.join(OUTPUT_DIR, 'minor-arcana', suit);

      if (!(await fs.pathExists(suitDir))) {
        console.log(`⚠️  目录不存在: ${suitDir}`);
        continue;
      }

      console.log(`处理 ${suit} 牌组...`);

      const files = await fs.readdir(suitDir);
      const imageFiles = files.filter(file =>
        /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(file)
      );

      for (const file of imageFiles) {
        const inputPath = path.join(suitDir, file);
        const baseName = path.parse(file).name;

        await this.processImage(inputPath, outputDir, baseName);
      }
    }
  }

  async processCardBacks() {
    console.log('🎨 处理牌背图片...\n');

    const cardBacksDir = path.join(INPUT_DIR, 'card-backs');
    const outputDir = path.join(OUTPUT_DIR, 'card-backs');

    if (!(await fs.pathExists(cardBacksDir))) {
      console.log(`⚠️  目录不存在: ${cardBacksDir}`);
      return;
    }

    const files = await fs.readdir(cardBacksDir);
    const imageFiles = files.filter(file =>
      /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(file)
    );

    for (const file of imageFiles) {
      const inputPath = path.join(cardBacksDir, file);
      const baseName = path.parse(file).name;

      await this.processImage(inputPath, outputDir, baseName);
    }
  }

  async setupDirectories() {
    console.log('📁 创建目录结构...\n');

    // 创建输入目录结构
    const inputDirs = [
      path.join(INPUT_DIR, 'major-arcana'),
      path.join(INPUT_DIR, 'minor-arcana', 'cups'),
      path.join(INPUT_DIR, 'minor-arcana', 'wands'),
      path.join(INPUT_DIR, 'minor-arcana', 'swords'),
      path.join(INPUT_DIR, 'minor-arcana', 'pentacles'),
      path.join(INPUT_DIR, 'card-backs')
    ];

    // 创建输出目录结构
    const outputDirs = [
      path.join(OUTPUT_DIR, 'major-arcana'),
      path.join(OUTPUT_DIR, 'minor-arcana', 'cups'),
      path.join(OUTPUT_DIR, 'minor-arcana', 'wands'),
      path.join(OUTPUT_DIR, 'minor-arcana', 'swords'),
      path.join(OUTPUT_DIR, 'minor-arcana', 'pentacles'),
      path.join(OUTPUT_DIR, 'card-backs')
    ];

    for (const dir of [...inputDirs, ...outputDirs]) {
      await fs.ensureDir(dir);
    }

    console.log('✅ 目录结构创建完成');
    console.log(`📂 输入目录: ${INPUT_DIR}`);
    console.log(`📂 输出目录: ${OUTPUT_DIR}\n`);

    // 检查是否有原始图片
    const hasImages = await this.checkForImages();
    if (!hasImages) {
      console.log('📋 使用说明:');
      console.log('1. 将下载的原始图片放入对应的 temp-images 文件夹');
      console.log('2. 大阿卡纳放入: temp-images/major-arcana/');
      console.log('3. 小阿卡纳按花色放入: temp-images/minor-arcana/cups/ 等');
      console.log('4. 牌背图片放入: temp-images/card-backs/');
      console.log('5. 再次运行此脚本进行处理\n');
    }
  }

  async checkForImages() {
    const dirs = [
      path.join(INPUT_DIR, 'major-arcana'),
      path.join(INPUT_DIR, 'minor-arcana', 'cups'),
      path.join(INPUT_DIR, 'minor-arcana', 'wands'),
      path.join(INPUT_DIR, 'minor-arcana', 'swords'),
      path.join(INPUT_DIR, 'minor-arcana', 'pentacles'),
      path.join(INPUT_DIR, 'card-backs')
    ];

    for (const dir of dirs) {
      if (await fs.pathExists(dir)) {
        const files = await fs.readdir(dir);
        const imageFiles = files.filter(file =>
          /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(file)
        );
        if (imageFiles.length > 0) {
          return true;
        }
      }
    }
    return false;
  }

  async run() {
    console.log('🖼️  塔罗牌图片批量处理器\n');

    try {
      // 设置目录
      await this.setupDirectories();

      // 检查是否有图片需要处理
      const hasImages = await this.checkForImages();
      if (!hasImages) {
        return;
      }

      // 处理各类图片
      await this.processMajorArcana();
      await this.processMinorArcana();
      await this.processCardBacks();

      // 输出统计
      console.log('📈 处理完成统计:');
      console.log(`✅ 成功处理: ${this.processedCount} 张图片`);
      console.log(`❌ 处理失败: ${this.failedCount} 张图片`);
      console.log(`📁 输出目录: ${OUTPUT_DIR}`);

      if (this.processedCount > 0) {
        console.log('\n🎉 图片处理完成！');
        console.log('💡 现在可以测试塔罗牌应用的图片显示效果了');
      }

    } catch (error) {
      console.error('💥 处理过程中发生错误:', error);
    }
  }
}

// 如果直接运行此脚本
if (require.main === module) {
  const processor = new ImageProcessor();
  processor.run();
}

module.exports = ImageProcessor;