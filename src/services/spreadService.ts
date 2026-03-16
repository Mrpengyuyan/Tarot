import { api } from './api';
import { SpreadType } from '../types/api';

/**
 * 牌阵相关API服务
 */
export class SpreadService {

  /**
   * 获取所有牌阵
   */
  async getAllSpreads(): Promise<SpreadType[]> {
    try {
      const response = await api.get('/spreads/');
      return response as unknown as SpreadType[];
    } catch (error) {
      console.error('获取牌阵失败:', error);
      throw new Error('获取牌阵数据失败');
    }
  }

  /**
   * 获取牌阵详情
   */
  async getSpreadDetail(spreadId: number): Promise<SpreadType> {
    try {
      const response = await api.get(`/spreads/${spreadId}`);
      return response as unknown as SpreadType;
    } catch (error) {
      console.error('获取牌阵详情失败:', error);
      throw new Error('获取牌阵详情失败');
    }
  }

  /**
   * 获取热门牌阵
   */
  async getPopularSpreads(limit: number = 10): Promise<SpreadType[]> {
    try {
      const response = await api.get(`/spreads/popular?limit=${limit}`);
      return response as unknown as SpreadType[];
    } catch (error) {
      console.error('获取热门牌阵失败:', error);
      throw new Error('获取热门牌阵失败');
    }
  }

  /**
   * 获取初学者友好的牌阵
   */
  async getBeginnerSpreads(skip: number = 0, limit: number = 20): Promise<SpreadType[]> {
    try {
      const response = await api.get(`/spreads/beginner?skip=${skip}&limit=${limit}`);
      return response as unknown as SpreadType[];
    } catch (error) {
      console.error('获取初学者牌阵失败:', error);
      throw new Error('获取初学者牌阵失败');
    }
  }

  /**
   * 根据问题类型获取推荐牌阵
   */
  async getSpreadsByQuestionType(
    questionType: 'love' | 'career' | 'finance' | 'health' | 'general',
    skip: number = 0,
    limit: number = 50
  ): Promise<SpreadType[]> {
    try {
      const response = await api.get(
        `/spreads/by-question-type/${questionType}?skip=${skip}&limit=${limit}`
      );
      return response as unknown as SpreadType[];
    } catch (error) {
      console.error('根据问题类型获取牌阵失败:', error);
      throw new Error('获取推荐牌阵失败');
    }
  }

  /**
   * 根据难度获取牌阵
   */
  async getSpreadsByDifficulty(
    minLevel: number = 1,
    maxLevel: number = 5
  ): Promise<SpreadType[]> {
    try {
      const allSpreads = await this.getAllSpreads();
      return allSpreads.filter(
        spread => spread.difficulty_level >= minLevel && spread.difficulty_level <= maxLevel
      );
    } catch (error) {
      console.error('根据难度获取牌阵失败:', error);
      throw new Error('获取牌阵失败');
    }
  }

  /**
   * 根据卡牌数量筛选牌阵
   */
  async getSpreadsByCardCount(
    minCards: number = 1,
    maxCards: number = 10
  ): Promise<SpreadType[]> {
    try {
      const allSpreads = await this.getAllSpreads();
      return allSpreads.filter(
        spread => spread.card_count >= minCards && spread.card_count <= maxCards
      );
    } catch (error) {
      console.error('根据卡牌数量获取牌阵失败:', error);
      throw new Error('获取牌阵失败');
    }
  }

  /**
   * 搜索牌阵
   */
  async searchSpreads(query: string): Promise<SpreadType[]> {
    try {
      const allSpreads = await this.getAllSpreads();
      const lowerQuery = query.toLowerCase();

      return allSpreads.filter(spread =>
        spread.name.toLowerCase().includes(lowerQuery) ||
        spread.description.toLowerCase().includes(lowerQuery) ||
        (spread.name_en && spread.name_en.toLowerCase().includes(lowerQuery))
      );
    } catch (error) {
      console.error('搜索牌阵失败:', error);
      throw new Error('搜索牌阵失败');
    }
  }

  /**
   * 获取推荐牌阵
   */
  async getRecommendedSpreads(
    questionType?: string,
    isBeginnerFriendly?: boolean,
    maxDifficulty?: number
  ): Promise<SpreadType[]> {
    try {
      let spreads = await this.getAllSpreads();

      // 按问题类型筛选
      if (questionType && questionType !== 'general') {
        const suitabilityMap: Record<string, keyof SpreadType> = {
          love: 'suitable_for_love',
          career: 'suitable_for_career',
          finance: 'suitable_for_finance',
          health: 'suitable_for_health',
        };

        const suitabilityField = suitabilityMap[questionType];
        if (suitabilityField) {
          spreads = spreads.filter(spread => (spread as any)[suitabilityField]);
        }
      }

      // 按初学者友好度筛选
      if (isBeginnerFriendly) {
        spreads = spreads.filter(spread => spread.is_beginner_friendly);
      }

      // 按难度筛选
      if (maxDifficulty) {
        spreads = spreads.filter(spread => spread.difficulty_level <= maxDifficulty);
      }

      // 按使用次数排序（如果有的话）
      spreads.sort((a, b) => (b.usage_count || 0) - (a.usage_count || 0));

      return spreads.slice(0, 6); // 返回前6个推荐
    } catch (error) {
      console.error('获取推荐牌阵失败:', error);
      throw new Error('获取推荐牌阵失败');
    }
  }

  /**
   * 验证牌阵ID是否存在
   */
  async validateSpreadId(spreadId: number): Promise<boolean> {
    try {
      await this.getSpreadDetail(spreadId);
      return true;
    } catch (error) {
      return false;
    }
  }

  /**
   * 获取牌阵统计信息
   */
  async getSpreadStats(): Promise<{
    total: number;
    beginner_friendly: number;
    by_difficulty: Record<number, number>;
    by_card_count: Record<number, number>;
    average_difficulty: number;
  }> {
    try {
      const spreads = await this.getAllSpreads();

      const stats = {
        total: spreads.length,
        beginner_friendly: spreads.filter(s => s.is_beginner_friendly).length,
        by_difficulty: {} as Record<number, number>,
        by_card_count: {} as Record<number, number>,
        average_difficulty: 0,
      };

      // 统计难度分布
      spreads.forEach(spread => {
        const level = spread.difficulty_level;
        stats.by_difficulty[level] = (stats.by_difficulty[level] || 0) + 1;

        const cardCount = spread.card_count;
        stats.by_card_count[cardCount] = (stats.by_card_count[cardCount] || 0) + 1;
      });

      // 计算平均难度
      const totalDifficulty = spreads.reduce((sum, spread) => sum + spread.difficulty_level, 0);
      stats.average_difficulty = spreads.length > 0 ? totalDifficulty / spreads.length : 0;

      return stats;
    } catch (error) {
      console.error('获取牌阵统计失败:', error);
      throw new Error('获取统计信息失败');
    }
  }

  /**
   * 检查牌阵适用性
   */
  isSpreadSuitableFor(spread: SpreadType, questionType: string): boolean {
    const suitabilityMap: Record<string, keyof SpreadType> = {
      love: 'suitable_for_love',
      career: 'suitable_for_career',
      finance: 'suitable_for_finance',
      health: 'suitable_for_health',
      general: 'suitable_for_general',
    };

    const field = suitabilityMap[questionType];
    return field ? Boolean((spread as any)[field]) : true;
  }

  /**
   * 根据用户水平推荐牌阵
   */
  async getSpreadsByUserLevel(level: 'beginner' | 'intermediate' | 'advanced'): Promise<SpreadType[]> {
    try {
      const spreads = await this.getAllSpreads();

      switch (level) {
        case 'beginner':
          return spreads.filter(s => s.is_beginner_friendly && s.difficulty_level <= 2);
        case 'intermediate':
          return spreads.filter(s => s.difficulty_level >= 2 && s.difficulty_level <= 4);
        case 'advanced':
          return spreads.filter(s => s.difficulty_level >= 4);
        default:
          return spreads;
      }
    } catch (error) {
      console.error('根据用户水平获取牌阵失败:', error);
      throw new Error('获取推荐牌阵失败');
    }
  }

  /**
   * 获取快速占卜牌阵（卡牌数少）
   */
  async getQuickSpreads(): Promise<SpreadType[]> {
    try {
      const spreads = await this.getAllSpreads();
      return spreads
        .filter(spread => spread.card_count <= 3)
        .sort((a, b) => a.card_count - b.card_count);
    } catch (error) {
      console.error('获取快速占卜牌阵失败:', error);
      throw new Error('获取快速牌阵失败');
    }
  }

  /**
   * 获取深度占卜牌阵（卡牌数多）
   */
  async getDetailedSpreads(): Promise<SpreadType[]> {
    try {
      const spreads = await this.getAllSpreads();
      return spreads
        .filter(spread => spread.card_count >= 7)
        .sort((a, b) => b.card_count - a.card_count);
    } catch (error) {
      console.error('获取详细占卜牌阵失败:', error);
      throw new Error('获取详细牌阵失败');
    }
  }

  /**
   * 获取牌阵的位置说明
   */
  getSpreadPositionGuide(spread: SpreadType): Array<{
    position: number;
    name: string;
    meaning: string;
    description: string;
  }> {
    if (spread.positions) {
      return spread.positions.map(pos => ({
        position: pos.position,
        name: pos.name,
        meaning: pos.meaning,
        description: pos.meaning // 使用meaning作为description
      }));
    }

    // 如果没有预定义位置，生成默认位置
    const positions = [];
    for (let i = 0; i < spread.card_count; i++) {
      positions.push({
        position: i + 1,
        name: `位置 ${i + 1}`,
        meaning: '待解读',
        description: `第 ${i + 1} 张牌的含义`,
      });
    }

    return positions;
  }

  /**
   * 计算牌阵复杂度评分
   */
  calculateComplexityScore(spread: SpreadType): number {
    let score = 0;

    // 卡牌数量影响复杂度
    score += spread.card_count * 10;

    // 难度等级影响复杂度
    score += spread.difficulty_level * 20;

    // 初学者友好度降低复杂度
    if (spread.is_beginner_friendly) {
      score -= 30;
    }

    return Math.max(0, score);
  }
}

// 创建全局实例
export const spreadService = new SpreadService();

// 默认导出
export default spreadService;