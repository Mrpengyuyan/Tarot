import { ImageQuality, ImageDimensions, TarotDeckConfig } from '../types/cardImages';

/**
 * 图片资源管理工具函数
 */

// 标准塔罗牌信息
export const TAROT_CARDS_INFO = {
  // 大阿卡纳 (0-21)
  majorArcana: [
    { id: 0, name: 'fool', name_zh: '愚者', name_en: 'The Fool' },
    { id: 1, name: 'magician', name_zh: '魔术师', name_en: 'The Magician' },
    { id: 2, name: 'high-priestess', name_zh: '女祭司', name_en: 'The High Priestess' },
    { id: 3, name: 'empress', name_zh: '皇后', name_en: 'The Empress' },
    { id: 4, name: 'emperor', name_zh: '皇帝', name_en: 'The Emperor' },
    { id: 5, name: 'hierophant', name_zh: '教皇', name_en: 'The Hierophant' },
    { id: 6, name: 'lovers', name_zh: '恋人', name_en: 'The Lovers' },
    { id: 7, name: 'chariot', name_zh: '战车', name_en: 'The Chariot' },
    { id: 8, name: 'strength', name_zh: '力量', name_en: 'Strength' },
    { id: 9, name: 'hermit', name_zh: '隐士', name_en: 'The Hermit' },
    { id: 10, name: 'wheel-of-fortune', name_zh: '命运之轮', name_en: 'Wheel of Fortune' },
    { id: 11, name: 'justice', name_zh: '正义', name_en: 'Justice' },
    { id: 12, name: 'hanged-man', name_zh: '倒吊人', name_en: 'The Hanged Man' },
    { id: 13, name: 'death', name_zh: '死神', name_en: 'Death' },
    { id: 14, name: 'temperance', name_zh: '节制', name_en: 'Temperance' },
    { id: 15, name: 'devil', name_zh: '恶魔', name_en: 'The Devil' },
    { id: 16, name: 'tower', name_zh: '塔', name_en: 'The Tower' },
    { id: 17, name: 'star', name_zh: '星星', name_en: 'The Star' },
    { id: 18, name: 'moon', name_zh: '月亮', name_en: 'The Moon' },
    { id: 19, name: 'sun', name_zh: '太阳', name_en: 'The Sun' },
    { id: 20, name: 'judgement', name_zh: '审判', name_en: 'Judgement' },
    { id: 21, name: 'world', name_zh: '世界', name_en: 'The World' }
  ],

  // 小阿卡纳花色
  minorArcana: {
    wands: { name_zh: '权杖', name_en: 'Wands', symbol: '🏒' },
    cups: { name_zh: '圣杯', name_en: 'Cups', symbol: '🏆' },
    swords: { name_zh: '宝剑', name_en: 'Swords', symbol: '⚔️' },
    pentacles: { name_zh: '星币', name_en: 'Pentacles', symbol: '🪙' }
  }
};

// 图片质量配置
export const IMAGE_QUALITY_CONFIG: Record<ImageQuality, ImageDimensions & { suffix: string }> = {
  thumbnail: { width: 200, height: 333, suffix: '_thumb' },
  standard: { width: 400, height: 666, suffix: '_std' },
  high: { width: 800, height: 1332, suffix: '_hd' },
  ultra: { width: 1200, height: 2000, suffix: '_uhd' }
};

// 默认塔罗牌套装配置
export const DEFAULT_DECK_CONFIG: TarotDeckConfig = {
  id: 'classic-rider-waite',
  name: '经典莱德伟特牌',
  description: '最经典的塔罗牌套装，适合初学者和专业占卜师',
  totalCards: 78,
  majorArcanaCount: 22,
  minorArcanaCount: 56,
  style: {
    theme: 'classic',
    colorScheme: 'gold',
    artistInfo: {
      name: 'Pamela Colman Smith',
      website: 'https://en.wikipedia.org/wiki/Pamela_Colman_Smith'
    }
  },
  cardBacks: [
    {
      id: 'classic',
      name: '经典牌背',
      images: {
        back: {
          thumbnail: { url: '', dimensions: { width: 200, height: 333 }, format: 'jpg', quality: 'thumbnail' },
          standard: { url: '', dimensions: { width: 400, height: 666 }, format: 'jpg', quality: 'standard' },
          high: { url: '', dimensions: { width: 800, height: 1332 }, format: 'jpg', quality: 'high' }
        }
      }
    }
  ],
  copyright: {
    year: 1909,
    holder: 'Public Domain',
    license: 'CC0'
  }
};

/**
 * 生成塔罗牌图片URL
 */
export const generateCardImageUrl = (
  cardId: number,
  quality: ImageQuality = 'standard',
  baseUrl: string = '/images/tarot-cards',
  format: 'jpg' | 'png' | 'webp' = 'jpg'
): string => {
  const cardInfo = getCardInfo(cardId);
  const qualityConfig = IMAGE_QUALITY_CONFIG[quality];

  let path: string;

  if (cardInfo.type === 'major_arcana') {
    path = `${baseUrl}/major-arcana/${cardId.toString().padStart(2, '0')}-${cardInfo.name}${qualityConfig.suffix}.${format}`;
  } else {
    const suitFolder = cardInfo.suit;
    const cardNumber = cardInfo.number;
    path = `${baseUrl}/minor-arcana/${suitFolder}/${cardNumber.toString().padStart(2, '0')}-${suitFolder}-${cardNumber}${qualityConfig.suffix}.${format}`;
  }

  return path;
};

/**
 * 生成牌背图片URL
 */
export const generateCardBackUrl = (
  backId: string = 'classic',
  quality: ImageQuality = 'standard',
  baseUrl: string = '/images/tarot-cards',
  format: 'jpg' | 'png' | 'webp' = 'jpg'
): string => {
  const qualityConfig = IMAGE_QUALITY_CONFIG[quality];
  return `${baseUrl}/card-backs/${backId}${qualityConfig.suffix}.${format}`;
};

/**
 * 获取卡片信息
 */
export const getCardInfo = (cardId: number): {
  type: 'major_arcana' | 'minor_arcana';
  suit: string;
  number: number;
  name: string;
  name_zh: string;
  name_en: string;
} => {
  if (cardId >= 0 && cardId <= 21) {
    // 大阿卡纳
    const cardInfo = TAROT_CARDS_INFO.majorArcana[cardId];
    return {
      type: 'major_arcana',
      suit: '',
      number: cardId,
      name: cardInfo.name,
      name_zh: cardInfo.name_zh,
      name_en: cardInfo.name_en
    };
  } else if (cardId >= 22 && cardId <= 77) {
    // 小阿卡纳
    const minorIndex = cardId - 22;
    const suits = Object.keys(TAROT_CARDS_INFO.minorArcana);
    const suitIndex = Math.floor(minorIndex / 14);
    const cardNumber = (minorIndex % 14) + 1;
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

    const suitInfo = TAROT_CARDS_INFO.minorArcana[suit as keyof typeof TAROT_CARDS_INFO.minorArcana];

    return {
      type: 'minor_arcana',
      suit,
      number: cardNumber,
      name: `${cardName}-of-${suit}`,
      name_zh: `${suitInfo.name_zh}${cardName === 'ace' ? 'A' : cardName}`,
      name_en: `${cardName} of ${suitInfo.name_en}`
    };
  } else {
    throw new Error(`Invalid card ID: ${cardId}`);
  }
};

/**
 * 获取所有卡片ID列表
 */
export const getAllCardIds = (): number[] => {
  return Array.from({ length: 78 }, (_, i) => i);
};

/**
 * 获取大阿卡纳卡片ID列表
 */
export const getMajorArcanaIds = (): number[] => {
  return Array.from({ length: 22 }, (_, i) => i);
};

/**
 * 获取小阿卡纳卡片ID列表
 */
export const getMinorArcanaIds = (): number[] => {
  return Array.from({ length: 56 }, (_, i) => i + 22);
};

/**
 * 按花色获取小阿卡纳卡片ID
 */
export const getMinorArcanaBySuit = (suit: 'wands' | 'cups' | 'swords' | 'pentacles'): number[] => {
  const suits = ['wands', 'cups', 'swords', 'pentacles'];
  const suitIndex = suits.indexOf(suit);
  if (suitIndex === -1) {
    throw new Error(`Invalid suit: ${suit}`);
  }

  const startId = 22 + (suitIndex * 14);
  return Array.from({ length: 14 }, (_, i) => startId + i);
};

/**
 * 验证图片URL是否有效
 */
export const validateImageUrl = async (url: string): Promise<boolean> => {
  try {
    const response = await fetch(url, { method: 'HEAD' });
    return response.ok && response.headers.get('content-type')?.startsWith('image/') === true;
  } catch {
    return false;
  }
};

/**
 * 获取图片文件大小
 */
export const getImageSize = async (url: string): Promise<number | null> => {
  try {
    const response = await fetch(url, { method: 'HEAD' });
    if (response.ok) {
      const contentLength = response.headers.get('content-length');
      return contentLength ? parseInt(contentLength, 10) : null;
    }
  } catch {
    // 静默失败
  }
  return null;
};

/**
 * 检查图片是否已缓存
 */
export const isImageCached = (url: string): boolean => {
  // 这个函数需要配合imageService使用
  // 这里提供一个基础实现
  try {
    const img = new Image();
    img.src = url;
    return img.complete && img.naturalHeight !== 0;
  } catch {
    return false;
  }
};

/**
 * 预加载单张图片
 */
export const preloadImage = (url: string): Promise<HTMLImageElement> => {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.crossOrigin = 'anonymous';
    img.onload = () => resolve(img);
    img.onerror = () => reject(new Error(`Failed to load image: ${url}`));
    img.src = url;
  });
};

/**
 * 批量预加载图片
 */
export const preloadImages = async (
  urls: string[],
  options: {
    concurrency?: number;
    onProgress?: (loaded: number, total: number) => void;
  } = {}
): Promise<HTMLImageElement[]> => {
  const { concurrency = 5, onProgress } = options;
  const results: HTMLImageElement[] = [];
  let loaded = 0;

  // 分批处理
  for (let i = 0; i < urls.length; i += concurrency) {
    const batch = urls.slice(i, i + concurrency);
    const batchPromises = batch.map(async (url) => {
      try {
        const img = await preloadImage(url);
        loaded++;
        onProgress?.(loaded, urls.length);
        return img;
      } catch (error) {
        loaded++;
        onProgress?.(loaded, urls.length);
        console.warn(`Failed to preload image: ${url}`, error);
        return null;
      }
    });

    const batchResults = await Promise.all(batchPromises);
    results.push(...batchResults.filter(Boolean) as HTMLImageElement[]);
  }

  return results;
};

/**
 * 生成响应式图片srcSet
 */
export const generateSrcSet = (
  cardId: number,
  baseUrl: string = '/images/tarot-cards',
  format: 'jpg' | 'png' | 'webp' = 'jpg'
): string => {
  const qualities: ImageQuality[] = ['thumbnail', 'standard', 'high'];

  return qualities
    .map(quality => {
      const url = generateCardImageUrl(cardId, quality, baseUrl, format);
      const config = IMAGE_QUALITY_CONFIG[quality];
      return `${url} ${config.width}w`;
    })
    .join(', ');
};

/**
 * 获取适合当前设备的图片质量
 */
export const getOptimalImageQuality = (
  containerWidth: number,
  devicePixelRatio: number = window.devicePixelRatio || 1
): ImageQuality => {
  const effectiveWidth = containerWidth * devicePixelRatio;

  if (effectiveWidth <= 250) {
    return 'thumbnail';
  } else if (effectiveWidth <= 500) {
    return 'standard';
  } else if (effectiveWidth <= 1000) {
    return 'high';
  } else {
    return 'ultra';
  }
};

/**
 * 检测WebP支持
 */
export const supportsWebP = (): Promise<boolean> => {
  return new Promise((resolve) => {
    const webP = new Image();
    webP.onload = webP.onerror = () => {
      resolve(webP.height === 2);
    };
    webP.src = 'data:image/webp;base64,UklGRjoAAABXRUJQVlA4IC4AAACyAgCdASoCAAIALmk0mk0iIiIiIgBoSygABc6WWgAA/veff/0PP8bA//LwYAAA';
  });
};

/**
 * 获取最佳图片格式
 */
export const getOptimalImageFormat = async (): Promise<'webp' | 'jpg'> => {
  const hasWebPSupport = await supportsWebP();
  return hasWebPSupport ? 'webp' : 'jpg';
};

export default {
  generateCardImageUrl,
  generateCardBackUrl,
  getCardInfo,
  getAllCardIds,
  getMajorArcanaIds,
  getMinorArcanaIds,
  getMinorArcanaBySuit,
  validateImageUrl,
  getImageSize,
  isImageCached,
  preloadImage,
  preloadImages,
  generateSrcSet,
  getOptimalImageQuality,
  supportsWebP,
  getOptimalImageFormat
};