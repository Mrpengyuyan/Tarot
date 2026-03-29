import { SpreadType } from '../types/api';

interface SpreadDisplayMeta {
  name: string;
  description: string;
  positions: string[];
}

const SPREAD_META: Record<number, SpreadDisplayMeta> = {
  1: {
    name: '单牌指引',
    description: '适合快速获取一个核心提示，适用于日常提问或简短决策。',
    positions: ['核心指引'],
  },
  2: {
    name: '过去 - 现在 - 未来',
    description: '用三张牌快速梳理事件脉络，查看问题的来源、现状与走向。',
    positions: ['过去', '现在', '未来'],
  },
  3: {
    name: '爱情牌阵',
    description: '聚焦关系状态、双方感受与未来发展，适合情感类提问。',
    positions: ['你的感受', '对方感受', '关系现状', '阻碍因素', '未来发展'],
  },
  4: {
    name: '事业牌阵',
    description: '用于梳理工作与职业发展，帮助识别优势、挑战与行动方向。',
    positions: ['当前状态', '优势', '核心课题', '挑战', '行动建议', '阶段结果'],
  },
  5: {
    name: '凯尔特十字',
    description: '适合深入分析复杂问题，从现状、阻力到外部环境与最终趋势进行完整解读。',
    positions: ['现状', '阻力', '远因', '近因', '可能发展', '潜意识', '自我状态', '外部环境', '期待与担忧', '最终结果'],
  },
  6: {
    name: '财富牌阵',
    description: '聚焦财务现状、风险与后续策略，适合金钱与资源规划类问题。',
    positions: ['当前财务', '收入来源', '支出压力', '理财建议'],
  },
};

const getFallbackText = (value?: string | null): string => {
  return (value || '').trim();
};

export const getSpreadDisplayName = (spread?: SpreadType | null): string => {
  if (!spread) return '';
  return SPREAD_META[spread.id]?.name || getFallbackText(spread.name_en) || getFallbackText(spread.name);
};

export const getSpreadDisplayDescription = (spread?: SpreadType | null): string => {
  if (!spread) return '';
  return SPREAD_META[spread.id]?.description || getFallbackText(spread.description);
};

export const getSpreadPositionLabels = (
  spread?: SpreadType | null,
  fallbackCount?: number,
): string[] => {
  if (spread && SPREAD_META[spread.id]) {
    return SPREAD_META[spread.id].positions;
  }

  if (spread?.positions?.length) {
    return spread.positions.map((position, index) => position.name?.trim() || `位置 ${index + 1}`);
  }

  const count = fallbackCount ?? spread?.card_count ?? 0;
  return Array.from({ length: count }, (_, index) => `位置 ${index + 1}`);
};
