// 简化的塔罗牌图片工具

export type ImageQuality = 'thumbnail' | 'standard' | 'high' | 'ultra';

// 质量配置映射
const QUALITY_SUFFIXES = {
  thumbnail: '_thumb',
  standard: '_std',
  high: '_hd',
  ultra: '_uhd'
};

// 塔罗牌数据
const TAROT_CARDS = {
  majorArcana: [
    { id: 0, name: 'fool', nameZh: '愚者' },
    { id: 1, name: 'magician', nameZh: '魔术师' },
    { id: 2, name: 'high-priestess', nameZh: '女祭司' },
    { id: 3, name: 'empress', nameZh: '皇后' },
    { id: 4, name: 'emperor', nameZh: '皇帝' },
    { id: 5, name: 'hierophant', nameZh: '教皇' },
    { id: 6, name: 'lovers', nameZh: '恋人' },
    { id: 7, name: 'chariot', nameZh: '战车' },
    { id: 8, name: 'strength', nameZh: '力量' },
    { id: 9, name: 'hermit', nameZh: '隐士' },
    { id: 10, name: 'wheel-of-fortune', nameZh: '命运之轮' },
    { id: 11, name: 'justice', nameZh: '正义' },
    { id: 12, name: 'hanged-man', nameZh: '倒吊人' },
    { id: 13, name: 'death', nameZh: '死神' },
    { id: 14, name: 'temperance', nameZh: '节制' },
    { id: 15, name: 'devil', nameZh: '恶魔' },
    { id: 16, name: 'tower', nameZh: '塔' },
    { id: 17, name: 'star', nameZh: '星星' },
    { id: 18, name: 'moon', nameZh: '月亮' },
    { id: 19, name: 'sun', nameZh: '太阳' },
    { id: 20, name: 'judgement', nameZh: '审判' },
    { id: 21, name: 'world', nameZh: '世界' }
  ],
  minorArcana: {
    wands: { nameZh: '权杖', nameEn: 'Wands' },
    cups: { nameZh: '圣杯', nameEn: 'Cups' },
    swords: { nameZh: '宝剑', nameEn: 'Swords' },
    pentacles: { nameZh: '星币', nameEn: 'Pentacles' }
  }
};

/**
 * 根据卡片ID生成图片URL
 */
export function getCardImageUrl(cardId: number, quality: ImageQuality = 'standard'): string {
  const suffix = QUALITY_SUFFIXES[quality];
  const baseUrl = '/images/tarot-cards';

  // 大阿卡纳 (0-21)
  if (cardId >= 0 && cardId <= 21) {
    const card = TAROT_CARDS.majorArcana[cardId];
    if (card) {
      const fileName = `${card.id.toString().padStart(2, '0')}-${card.name}${suffix}.jpg`;
      return `${baseUrl}/major-arcana/${fileName}`;
    }
  }

  // 小阿卡纳 (22-77)
  if (cardId >= 22 && cardId <= 77) {
    const minorIndex = cardId - 22;
    const suits = Object.keys(TAROT_CARDS.minorArcana);
    const suitIndex = Math.floor(minorIndex / 14);
    const cardNumber = (minorIndex % 14) + 1;

    if (suitIndex < suits.length) {
      const suit = suits[suitIndex];
      let cardName: string;

      if (cardNumber === 1) {
        cardName = 'ace';
      } else if (cardNumber <= 10) {
        cardName = cardNumber.toString();
      } else {
        const courtCards = ['page', 'knight', 'queen', 'king'];
        cardName = courtCards[cardNumber - 11];
      }

      const fileName = `${cardNumber.toString().padStart(2, '0')}-${suit}-${cardName}${suffix}.jpg`;
      return `${baseUrl}/minor-arcana/${suit}/${fileName}`;
    }
  }

  // 默认返回占位图
  return `${baseUrl}/placeholder${suffix}.jpg`;
}

/**
 * 获取卡片背面图片URL
 */
export function getCardBackUrl(backStyle: string = 'classic', quality: ImageQuality = 'standard'): string {
  const suffix = QUALITY_SUFFIXES[quality];
  return `/images/tarot-cards/card-backs/${backStyle}${suffix}.jpg`;
}

/**
 * 预加载图片
 */
export function preloadImage(url: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = reject;
    img.src = url;
  });
}

/**
 * 获取卡片信息
 */
export function getCardInfo(cardId: number) {
  if (cardId >= 0 && cardId <= 21) {
    return {
      ...TAROT_CARDS.majorArcana[cardId],
      type: 'major_arcana' as const
    };
  }

  if (cardId >= 22 && cardId <= 77) {
    const minorIndex = cardId - 22;
    const suits = Object.keys(TAROT_CARDS.minorArcana);
    const suitIndex = Math.floor(minorIndex / 14);
    const cardNumber = (minorIndex % 14) + 1;

    if (suitIndex < suits.length) {
      const suit = suits[suitIndex];
      let cardName: string;

      if (cardNumber === 1) {
        cardName = 'ace';
      } else if (cardNumber <= 10) {
        cardName = cardNumber.toString();
      } else {
        const courtCards = ['page', 'knight', 'queen', 'king'];
        cardName = courtCards[cardNumber - 11];
      }

      return {
        id: cardId,
        name: `${suit}-${cardName}`,
        nameZh: `${(TAROT_CARDS.minorArcana as any)[suit].nameZh}${cardName}`,
        type: 'minor_arcana' as const,
        suit,
        number: cardNumber
      };
    }
  }

  return null;
}