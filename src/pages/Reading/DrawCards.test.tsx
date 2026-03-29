import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { tarotService } from '../../services/tarotService';
import DrawCards from './DrawCards';

jest.mock('../../services/tarotService', () => ({
  tarotService: {
    getAllSpreads: jest.fn(),
    createReading: jest.fn(),
    drawCardsForReading: jest.fn(),
    getReadingCards: jest.fn(),
    createInterpretation: jest.fn(),
    getReadingDetailInfo: jest.fn(),
  },
  localDrawingUtils: {
    generateCardImageUrl: jest.fn(() => '/mock-card.jpg'),
  },
}));

jest.mock('../../components/Tarot/CardSpread', () => ({
  __esModule: true,
  default: ({ drawnCards, onCardFlip }: any) => (
    <div>
      {drawnCards.map((drawnCard: any, index: number) => (
        <button key={index} onClick={() => onCardFlip?.(drawnCard.card, index)}>
          {`flip-${index}`}
        </button>
      ))}
    </div>
  ),
}));

jest.mock('../../components/UI/Loading', () => ({
  __esModule: true,
  default: ({ message }: { message?: string }) => <div>{message || 'loading'}</div>,
}));

jest.mock('../../components/Effects/CosmicBackground', () => ({
  __esModule: true,
  default: () => <div data-testid="cosmic-background" />,
}));

jest.mock(
  'react-router-dom',
  () => ({
    useNavigate: () => jest.fn(),
  }),
  { virtual: true },
);

const mockTarotService = tarotService as jest.Mocked<typeof tarotService>;

describe('DrawCards', () => {
  beforeEach(() => {
    jest.clearAllMocks();

    mockTarotService.getAllSpreads.mockResolvedValue([
      {
        id: 1,
        name: 'single-card',
        description: 'single spread',
        card_count: 1,
        difficulty_level: 1,
        is_beginner_friendly: true,
      },
    ]);

    mockTarotService.createReading.mockResolvedValue({
      id: 123,
      user_id: 1,
      spread_type_id: 1,
      question: '我接下来应该优先做什么？',
      question_type: 'general',
      status: 'pending',
      created_at: '2026-03-24T00:00:00Z',
    });

    mockTarotService.drawCardsForReading.mockResolvedValue({
      prediction_id: 123,
      status: 'success',
      card_draws: [],
    });

    mockTarotService.getReadingCards.mockResolvedValue([
      {
        id: 1,
        prediction_id: 123,
        tarot_card_id: 1,
        position: 1,
        is_reversed: false,
        drawn_at: '2026-03-24T00:00:00Z',
        tarot_card: {
          id: 1,
          name_zh: '愚者',
          name_en: 'The Fool',
          card_number: 0,
          card_type: 'major_arcana',
        },
      },
    ]);

    mockTarotService.createInterpretation.mockResolvedValue({
      id: 10,
      prediction_id: 123,
      overall_interpretation: '这是模型返回的解读结果。',
      card_analysis: '单张牌提示你先聚焦最重要的事情。',
      relationship_analysis: null,
      advice: '先做优先级最高的一步。',
      warning: null,
      summary: null,
      key_themes: '聚焦,行动',
      generated_at: '2026-03-24T00:00:10Z',
    });

    mockTarotService.getReadingDetailInfo.mockResolvedValue({
      id: 123,
      user_id: 1,
      spread_type_id: 1,
      question: '我接下来应该优先做什么？',
      question_type: 'general',
      status: 'completed',
      created_at: '2026-03-24T00:00:00Z',
      interpretation: {
        id: 10,
        prediction_id: 123,
        overall_interpretation: '这是模型返回的解读结果。',
        generated_at: '2026-03-24T00:00:10Z',
      },
    });
  });

  it('requests AI interpretation after all cards are flipped and renders the answer', async () => {
    render(<DrawCards />);

    await screen.findByText('单牌指引');

    fireEvent.click(screen.getByText('单牌指引'));
    fireEvent.click(screen.getByRole('button', { name: '下一步' }));

    fireEvent.change(screen.getByLabelText('你的问题'), {
      target: { value: '我接下来应该优先做什么？' },
    });

    fireEvent.click(screen.getByRole('button', { name: '进入抽牌' }));
    fireEvent.click(screen.getByRole('button', { name: '开始抽牌' }));
    fireEvent.click(await screen.findByRole('button', { name: 'flip-0' }));

    expect(await screen.findByText('这是模型返回的解读结果。')).toBeInTheDocument();

    await waitFor(() => {
      expect(mockTarotService.createInterpretation).toHaveBeenCalledWith(
        123,
        expect.objectContaining({
          forceAI: true,
          timeoutMs: 65000,
        }),
      );
    });
  });
});
