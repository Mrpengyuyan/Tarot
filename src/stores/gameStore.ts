import { create } from 'zustand';
import { TarotCard, SpreadType } from '../types/api';

export enum GamePhase {
  IDLE = 'idle',
  SELECTING_SPREAD = 'selecting_spread',
  ASKING_QUESTION = 'asking_question',
  DRAWING_CARDS = 'drawing_cards',
  CARDS_DRAWN = 'cards_drawn',
  INTERPRETING = 'interpreting',
  COMPLETED = 'completed',
}

export type QuestionType = 'love' | 'career' | 'finance' | 'health' | 'general';

export interface DrawnCard {
  card: TarotCard;
  position: number;
  isReversed: boolean;
  positionMeaning: string;
}

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
  currentPhase: GamePhase;
  currentSession: ReadingSession | null;
  availableSpreads: SpreadType[];
  tarotDeck: TarotCard[];
  isLoading: boolean;
  loadingMessage: string;
  error: string | null;
  cardAnimations: {
    isDrawing: boolean;
    currentDrawingCard: number;
    showCards: boolean;
  };
  setPhase: (phase: GamePhase) => void;
  startNewReading: () => void;
  setQuestion: (question: string, type: QuestionType) => void;
  selectSpread: (spread: SpreadType) => void;
  drawCards: (cards: DrawnCard[]) => void;
  setInterpretation: (interpretation: string) => void;
  completeReading: () => void;
  resetGame: () => void;
  setSpreads: (spreads: SpreadType[]) => void;
  setTarotDeck: (cards: TarotCard[]) => void;
  setLoading: (loading: boolean, message?: string) => void;
  setError: (error: string | null) => void;
  setCardAnimations: (animations: Partial<GameState['cardAnimations']>) => void;
}

export const useGameStore = create<GameState>((set, get) => ({
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

  setPhase: (phase) => set({ currentPhase: phase }),

  startNewReading: () => {
    const sessionId = `reading_${Date.now()}_${Math.random().toString(36).slice(2, 11)}`;
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
    if (!currentSession) return;

    set({
      currentSession: {
        ...currentSession,
        question,
        questionType: type,
      },
      currentPhase: GamePhase.ASKING_QUESTION,
    });
  },

  selectSpread: (spread) => {
    const { currentSession } = get();
    if (!currentSession) return;

    set({
      currentSession: {
        ...currentSession,
        spread,
      },
      currentPhase: GamePhase.ASKING_QUESTION,
    });
  },

  drawCards: (cards) => {
    const { currentSession } = get();
    if (!currentSession) return;

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
  },

  setInterpretation: (interpretation) => {
    const { currentSession } = get();
    if (!currentSession) return;

    set({
      currentSession: {
        ...currentSession,
        interpretation,
      },
      currentPhase: GamePhase.COMPLETED,
    });
  },

  completeReading: () => {
    const { currentSession } = get();
    if (!currentSession) return;

    set({
      currentSession: {
        ...currentSession,
        completedAt: new Date(),
      },
      currentPhase: GamePhase.COMPLETED,
    });
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

  setSpreads: (spreads) => set({ availableSpreads: spreads }),
  setTarotDeck: (cards) => set({ tarotDeck: cards }),
  setLoading: (loading, message = '') => set({ isLoading: loading, loadingMessage: message }),
  setError: (error) => set({ error }),

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

export const getPhaseTitle = (phase: GamePhase): string => {
  switch (phase) {
    case GamePhase.IDLE:
      return '准备开始';
    case GamePhase.SELECTING_SPREAD:
      return '选择牌阵';
    case GamePhase.ASKING_QUESTION:
      return '输入问题';
    case GamePhase.DRAWING_CARDS:
      return '正在抽牌';
    case GamePhase.CARDS_DRAWN:
      return '查看牌面';
    case GamePhase.INTERPRETING:
      return 'AI 解读中';
    case GamePhase.COMPLETED:
      return '占卜完成';
    default:
      return '未知状态';
  }
};

export const getQuestionTypeLabel = (type: QuestionType): string => {
  switch (type) {
    case 'love':
      return '感情关系';
    case 'career':
      return '事业工作';
    case 'finance':
      return '财务规划';
    case 'health':
      return '健康状态';
    case 'general':
      return '综合运势';
    default:
      return '综合运势';
  }
};
