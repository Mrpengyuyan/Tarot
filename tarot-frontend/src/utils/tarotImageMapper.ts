import { TarotCard } from '../types/api';

/**
 * 将塔罗牌卡片信息映射到实际的图片文件路径
 *
 * 后端返回的路径格式: /images/cards/eight_of_cups.jpg
 * 实际文件路径格式: /images/tarot-cards/minor-arcana/cups/08-cups-8_std.jpg
 */
export function getTarotCardImagePath(card: TarotCard): string {
  const { card_number, card_type, suit, name_en } = card;

  // 格式化卡号为两位数
  const formattedNumber = String(card_number).padStart(2, '0');

  // 大阿卡纳
  if (card_type === 'major_arcana') {
    // 从英文名提取关键词
    const keyword = name_en
      .toLowerCase()
      .replace('the ', '')
      .replace(/\s+/g, '-');

    return `/images/tarot-cards/major-arcana/${formattedNumber}-${keyword}_std.jpg`;
  }

  // 小阿卡纳
  if (card_type === 'minor_arcana' && suit) {
    // 处理特殊牌（宫廷牌）
    const lowerName = name_en.toLowerCase();

    if (lowerName.includes('ace')) {
      return `/images/tarot-cards/minor-arcana/${suit}/01-${suit}-ace_std.jpg`;
    }

    if (lowerName.includes('page')) {
      return `/images/tarot-cards/minor-arcana/${suit}/11-${suit}-page_std.jpg`;
    }

    if (lowerName.includes('knight')) {
      return `/images/tarot-cards/minor-arcana/${suit}/12-${suit}-knight_std.jpg`;
    }

    if (lowerName.includes('queen')) {
      return `/images/tarot-cards/minor-arcana/${suit}/13-${suit}-queen_std.jpg`;
    }

    if (lowerName.includes('king')) {
      return `/images/tarot-cards/minor-arcana/${suit}/14-${suit}-king_std.jpg`;
    }

    // 数字牌 (2-10)
    return `/images/tarot-cards/minor-arcana/${suit}/${formattedNumber}-${suit}-${card_number}_std.jpg`;
  }

  // 降级到默认图片
  return '/images/tarot-cards/default.jpg';
}

/**
 * 获取所有可能的图片路径（用于尝试加载）
 */
export function getTarotCardImagePaths(card: TarotCard): string[] {
  const paths: string[] = [];

  // 主路径（带_std后缀）
  paths.push(getTarotCardImagePath(card));

  // 备选路径 - 无后缀版本（temp-images格式）
  const mainPath = getTarotCardImagePath(card);
  if (mainPath.includes('_std.jpg')) {
    paths.push(mainPath.replace('_std.jpg', '.jpg'));
    paths.push(mainPath.replace('_std.jpg', '_hd.jpg'));
    paths.push(mainPath.replace('_std.jpg', '_thumb.jpg'));
  }

  // 特殊修正：pages vs page
  if (mainPath.includes('-page_std.jpg')) {
    paths.push(mainPath.replace('-page_std.jpg', '-pages.jpg'));
  }

  // 备选路径 - 后端原始路径
  if (card.image_url) {
    paths.push(card.image_url);
  }

  // 最终降级
  paths.push('/images/tarot-cards/default.jpg');

  return paths;
}

/**
 * 获取带错误重试的图片路径
 * 用于Card3DFlip组件，支持自动fallback
 */
export function getTarotCardImagePathWithFallback(card: TarotCard, attemptNumber: number = 0): string {
  const paths = getTarotCardImagePaths(card);
  return paths[attemptNumber] || paths[paths.length - 1];
}
