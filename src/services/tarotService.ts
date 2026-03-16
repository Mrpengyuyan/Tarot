import { api } from './api';
import { TarotCard, SpreadType } from '../types/api';
import { getCardImageUrl } from '../utils/cardImageUtils';

// 抽牌相关类型定义
export interface DrawnCard {
  card: TarotCard;
  isReversed: boolean;
  position: number;
}

export interface DrawCardsParams {
  count: number;
  excludeIds?: number[];
}

export interface CreateReadingParams {
  spreadId: number;
  question: string;
  questionType: 'love' | 'career' | 'finance' | 'health' | 'general';
}

export interface Reading {
  id: number;
  user_id: number;
  spread_type_id: number;
  question: string;
  question_type: 'love' | 'career' | 'finance' | 'health' | 'general';
  status: 'pending' | 'processing' | 'completed' | 'failed';
  created_at: string;
  completed_at?: string;
  is_favorite?: boolean;
  user_rating?: number | null;
  user_notes?: string | null;
}

export interface ReadingSummary {
  id: number;
  question: string;
  question_type: 'love' | 'career' | 'finance' | 'health' | 'general';
  status: 'pending' | 'processing' | 'completed' | 'failed';
  created_at: string;
  is_favorite?: boolean;
  user_rating?: number | null;
}

export interface ReadingDetail extends Reading {
  spread_type?: SpreadType;
  card_draws?: CardDrawWithMeaning[];
  interpretation?: Interpretation | null;
}

export interface ReadingStats {
  total_predictions: number;
  completed_predictions: number;
  favorite_predictions: number;
  most_used_question_type?: string;
  average_rating?: number | null;
}

export interface CardDrawWithMeaning {
  id: number;
  prediction_id: number;
  tarot_card_id: number;
  position: number;
  is_reversed: boolean;
  drawn_at: string;
  tarot_card?: TarotCard;
  card_meaning?: {
    id: number;
    name_zh: string;
    name_en: string;
    is_reversed: boolean;
    meaning: string;
    keywords: string[];
    position?: number;
    position_name?: string;
    position_meaning?: string;
  };
  position_name?: string;
  position_meaning?: string;
}

export interface DrawCardsResponse {
  prediction_id: number;
  card_draws: CardDrawWithMeaning[];
  status: string;
}

export interface Interpretation {
  id: number;
  prediction_id: number;
  overall_interpretation: string;
  card_analysis?: string | null;
  relationship_analysis?: string | null;
  advice?: string | null;
  warning?: string | null;
  summary?: string | null;
  key_themes?: string | null;
  model_used?: string | null;
  model_version?: string | null;
  confidence_score?: number | null;
  generated_at: string;
}

export interface ReadingWithCards {
  reading: Reading;
  cards: CardDrawWithMeaning[];
}

export const tarotService = {
  // 获取所有塔罗牌
  getAllCards: async (): Promise<TarotCard[]> => {
    return api.get('/cards/');
  },

  // 获取塔罗牌详情
  getCardDetail: async (cardId: number): Promise<TarotCard> => {
    return api.get(`/cards/${cardId}`);
  },

  // 随机抽牌
  drawRandomCards: async (params: DrawCardsParams): Promise<TarotCard[]> => {
    const queryParams = new URLSearchParams({
      count: params.count.toString(),
    });

    if (params.excludeIds && params.excludeIds.length > 0) {
      params.excludeIds.forEach(id => {
        queryParams.append('exclude_ids', id.toString());
      });
    }

    return api.get(`/cards/draw?${queryParams.toString()}`);
  },

  // 获取大阿卡纳牌
  getMajorArcanaCards: async (): Promise<TarotCard[]> => {
    return api.get('/cards/major-arcana');
  },

  // 获取小阿卡纳牌
  getMinorArcanaCards: async (): Promise<TarotCard[]> => {
    return api.get('/cards/minor-arcana');
  },

  // 搜索塔罗牌
  searchCards: async (query: string, skip = 0, limit = 50): Promise<TarotCard[]> => {
    return api.get(`/cards/search?q=${encodeURIComponent(query)}&skip=${skip}&limit=${limit}`);
  },

  // 获取所有牌阵
  getAllSpreads: async (): Promise<SpreadType[]> => {
    return api.get('/spreads/');
  },

  // 获取牌阵详情
  getSpreadDetail: async (spreadId: number): Promise<SpreadType> => {
    return api.get(`/spreads/${spreadId}`);
  },

  // 获取热门牌阵
  getPopularSpreads: async (limit = 10): Promise<SpreadType[]> => {
    return api.get(`/spreads/popular?limit=${limit}`);
  },

  // 获取初学者牌阵
  getBeginnerSpreads: async (skip = 0, limit = 20): Promise<SpreadType[]> => {
    return api.get(`/spreads/beginner?skip=${skip}&limit=${limit}`);
  },

  // 根据问题类型获取牌阵
  getSpreadsByQuestionType: async (questionType: string, skip = 0, limit = 50): Promise<SpreadType[]> => {
    return api.get(`/spreads/by-question-type/${questionType}?skip=${skip}&limit=${limit}`);
  },

  // 创建新的塔罗阅读
  createReading: async (params: CreateReadingParams): Promise<Reading> => {
    return api.post('/records/', {
      spread_type_id: params.spreadId,
      question: params.question,
      question_type: params.questionType,
    });
  },

  // 为阅读抽牌
  drawCardsForReading: async (readingId: number): Promise<DrawCardsResponse> => {
    return api.post(`/records/${readingId}/draw`);
  },

  // 获取占卜的抽牌结果（包含卡牌详情与含义）
  getReadingCards: async (readingId: number): Promise<CardDrawWithMeaning[]> => {
    return api.get(`/records/${readingId}/cards`);
  },

  // 创建AI解读
  createInterpretation: async (
    readingId: number,
    options?: {
      userContext?: string;
      forceAI?: boolean;
      timeoutMs?: number;
    }
  ): Promise<Interpretation> => {
    const params: Record<string, string | boolean> = {};
    if (options?.userContext) {
      params.user_context = options.userContext;
    }
    if (options?.forceAI !== undefined) {
      params.force_ai = options.forceAI;
    }

    return api.post(`/records/${readingId}/interpret`, null, {
      params,
      timeout: options?.timeoutMs,
    });
  },

  // 获取用户的阅读记录
  getUserReadings: async (skip = 0, limit = 20): Promise<ReadingSummary[]> => {
    return api.get(`/records/?skip=${skip}&limit=${limit}`);
  },

  getReadingDetailInfo: async (readingId: number): Promise<ReadingDetail> => {
    return api.get(`/records/${readingId}`);
  },

  // 获取阅读详情
  getReadingDetail: async (readingId: number): Promise<ReadingWithCards> => {
    const reading = await api.get(`/records/${readingId}`) as unknown as Reading;
    const cards = await api.get(`/records/${readingId}/cards`) as unknown as CardDrawWithMeaning[];
    return { reading, cards };
  },

  // 获取用户阅读统计
  getUserStats: async (): Promise<ReadingStats> => {
    return api.get('/records/stats');
  },

  // 获取最近的阅读
  getRecentReadings: async (days = 7, limit = 10): Promise<ReadingSummary[]> => {
    return api.get(`/records/recent?days=${days}&limit=${limit}`);
  },
};

// 本地抽牌工具函数
export const localDrawingUtils = {
  // 生成随机正逆位
  generateRandomReversed: (reversedProbability = 0.3): boolean => {
    return Math.random() < reversedProbability;
  },

  // 生成卡片图片URL（带fallback）
  generateCardImageUrl: (card: TarotCard): string => {
    // 优先使用卡片自带的image_url
    if (card.image_url && card.image_url.trim()) {
      return card.image_url;
    }

    // 尝试使用cardImageUtils生成URL
    try {
      const generatedUrl = getCardImageUrl(card.id, 'standard');
      if (generatedUrl) {
        return generatedUrl;
      }
    } catch (error) {
      console.warn(`无法为卡片 ${card.id} 生成图片URL:`, error);
    }

    // Fallback方案
    const fallbackUrls = [
      `/images/tarot-cards/${card.id}.jpg`,
      `/images/tarot-cards/card-${card.card_number}.jpg`,
      `/images/cards/${card.name_en?.toLowerCase().replace(/\s+/g, '-')}.jpg`,
      `/images/tarot-cards/default.jpg`
    ];

    return fallbackUrls[0];
  },

  // 从牌组中随机抽取指定数量的牌
  drawFromDeck: (deck: TarotCard[], count: number, excludeIds: number[] = []): DrawnCard[] => {
    const availableCards = deck.filter(card => !excludeIds.includes(card.id));

    if (availableCards.length < count) {
      throw new Error(`可用牌数量不足，需要 ${count} 张，但只有 ${availableCards.length} 张可用`);
    }

    const shuffled = [...availableCards].sort(() => Math.random() - 0.5);
    const drawn = shuffled.slice(0, count);

    return drawn.map((card, index) => ({
      card: {
        ...card,
        image_url: localDrawingUtils.generateCardImageUrl(card)
      },
      isReversed: localDrawingUtils.generateRandomReversed(),
      position: index,
    }));
  },

  // 重新洗牌
  shuffleDeck: (deck: TarotCard[]): TarotCard[] => {
    const shuffled = [...deck];
    for (let i = shuffled.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]];
    }
    return shuffled;
  },

  // 检查牌阵适用性
  isSpreadSuitableFor: (spread: SpreadType, questionType: string): boolean => {
    const suitabilityMap: { [key: string]: string } = {
      'love': 'suitable_for_love',
      'career': 'suitable_for_career',
      'finance': 'suitable_for_finance',
      'health': 'suitable_for_health',
      'general': 'suitable_for_general',
    };

    const suitabilityField = suitabilityMap[questionType];
    return suitabilityField ? Boolean((spread as any)[suitabilityField]) : true;
  },

  // 根据难度推荐牌阵
  filterSpreadsByDifficulty: (spreads: SpreadType[], maxDifficulty: number): SpreadType[] => {
    return spreads.filter(spread => spread.difficulty_level <= maxDifficulty);
  },

  // 获取初学者友好的牌阵
  getBeginnerFriendlySpreads: (spreads: SpreadType[]): SpreadType[] => {
    return spreads.filter(spread => spread.is_beginner_friendly);
  },
};

export default tarotService;
