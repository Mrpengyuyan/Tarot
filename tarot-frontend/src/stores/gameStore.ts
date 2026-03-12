import { create } from 'zustand';
import { TarotCard, SpreadType } from '../types/api';

// 游戏阶段枚举
export enum GamePhase {
  IDLE = 'idle',                    // 空闲状态
  SELECTING_SPREAD = 'selecting_spread', // 选择牌阵
  ASKING_QUESTION = 'asking_question',   // 输入问题
  DRAWING_CARDS = 'drawing_cards',       // 抽牌中
  CARDS_DRAWN = 'cards_drawn',           // 抽牌完成
  INTERPRETING = 'interpreting',         // AI解读中
  COMPLETED = 'completed',               // 占卜完成
}

// 问题类型
export type QuestionType = 'love' | 'career' | 'finance' | 'health' | 'general';

// 抽取的牌信息
export interface DrawnCard {
  card: TarotCard;
  position: number;
  isReversed: boolean;
  positionMeaning: string;
}

// 占卜会话状态
export interface ReadingSession {
  id: string;
  question: string;
  questionType: QuestionType;
  spread: SpreadType | null;
  drawnCards: DrawnCard[];
  interpretation?: string;
  createdAt: Date;
  completedAt?: Date;
}

interface GameState {
  // 当前游戏状态
  currentPhase: GamePhase;
  currentSession: ReadingSession | null;

  // 可用的牌阵和卡牌
  availableSpreads: SpreadType[];
  tarotDeck: TarotCard[];

  // UI状态
  isLoading: boolean;
  loadingMessage: string;
  error: string | null;

  // 动画控制
  cardAnimations: {
    isDrawing: boolean;
    currentDrawingCard: number;
    showCards: boolean;
  };

  // Actions
  setPhase: (phase: GamePhase) => void;
  startNewReading: () => void;
  setQuestion: (question: string, type: QuestionType) => void;
  selectSpread: (spread: SpreadType) => void;
  drawCards: (cards: DrawnCard[]) => void;
  setInterpretation: (interpretation: string) => void;
  completeReading: () => void;
  resetGame: () => void;

  // 数据管理
  setSpreads: (spreads: SpreadType[]) => void;
  setTarotDeck: (cards: TarotCard[]) => void;

  // UI控制
  setLoading: (loading: boolean, message?: string) => void;
  setError: (error: string | null) => void;

  // 动画控制
  setCardAnimations: (animations: Partial<GameState['cardAnimations']>) => void;
}

export const useGameStore = create<GameState>((set, get) => ({
  // 初始状态
  currentPhase: GamePhase.IDLE,
  currentSession: null,
  availableSpreads: [],
  tarotDeck: [],
  isLoading: false,
  loadingMessage: '',
  error: null,
  cardAnimations: {
    isDrawing: false,
    currentDrawingCard: 0,
    showCards: false,
  },

  // 游戏流程控制
  setPhase: (phase) => set({ currentPhase: phase }),

  startNewReading: () => {
    const sessionId = `reading_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    set({
      currentPhase: GamePhase.SELECTING_SPREAD,
      currentSession: {
        id: sessionId,
        question: '',
        questionType: 'general',
        spread: null,
        drawnCards: [],
        createdAt: new Date(),
      },
      error: null,
      cardAnimations: {
        isDrawing: false,
        currentDrawingCard: 0,
        showCards: false,
      },
    });
  },

  setQuestion: (question, type) => {
    const { currentSession } = get();
    if (currentSession) {
      set({
        currentSession: {
          ...currentSession,
          question,
          questionType: type,
        },
        currentPhase: GamePhase.ASKING_QUESTION,
      });
    }
  },

  selectSpread: (spread) => {
    const { currentSession } = get();
    if (currentSession) {
      set({
        currentSession: {
          ...currentSession,
          spread,
        },
        currentPhase: GamePhase.ASKING_QUESTION,
      });
    }
  },

  drawCards: (cards) => {
    const { currentSession } = get();
    if (currentSession) {
      set({
        currentSession: {
          ...currentSession,
          drawnCards: cards,
        },
        currentPhase: GamePhase.CARDS_DRAWN,
        cardAnimations: {
          isDrawing: false,
          currentDrawingCard: 0,
          showCards: true,
        },
      });
    }
  },

  setInterpretation: (interpretation) => {
    const { currentSession } = get();
    if (currentSession) {
      set({
        currentSession: {
          ...currentSession,
          interpretation,
        },
        currentPhase: GamePhase.COMPLETED,
      });
    }
  },

  completeReading: () => {
    const { currentSession } = get();
    if (currentSession) {
      set({
        currentSession: {
          ...currentSession,
          completedAt: new Date(),
        },
        currentPhase: GamePhase.COMPLETED,
      });
    }
  },

  resetGame: () => {
    set({
      currentPhase: GamePhase.IDLE,
      currentSession: null,
      error: null,
      isLoading: false,
      loadingMessage: '',
      cardAnimations: {
        isDrawing: false,
        currentDrawingCard: 0,
        showCards: false,
      },
    });
  },

  // 数据管理
  setSpreads: (spreads) => set({ availableSpreads: spreads }),

  setTarotDeck: (cards) => set({ tarotDeck: cards }),

  // UI控制
  setLoading: (loading, message = '') => set({
    isLoading: loading,
    loadingMessage: message
  }),

  setError: (error) => set({ error }),

  // 动画控制
  setCardAnimations: (animations) => {
    const { cardAnimations } = get();
    set({
      cardAnimations: {
        ...cardAnimations,
        ...animations,
      },
    });
  },
}));

// 辅助函数
export const getPhaseTitle = (phase: GamePhase): string => {
  switch (phase) {
    case GamePhase.IDLE:
      return '准备开始';
    case GamePhase.SELECTING_SPREAD:
      return '选择牌阵';
    case GamePhase.ASKING_QUESTION:
      return '提出问题';
    case GamePhase.DRAWING_CARDS:
      return '抽取塔罗牌';
    case GamePhase.CARDS_DRAWN:
      return '查看牌面';
    case GamePhase.INTERPRETING:
      return 'AI解读中';
    case GamePhase.COMPLETED:
      return '占卜完成';
    default:
      return '未知状态';
  }
};

export const getQuestionTypeLabel = (type: QuestionType): string => {
  switch (type) {
    case 'love':
      return '感情恋爱';
    case 'career':
      return '事业工作';
    case 'finance':
      return '财运投资';
    case 'health':
      return '健康养生';
    case 'general':
      return '综合运势';
    default:
      return '综合运势';
  }
};