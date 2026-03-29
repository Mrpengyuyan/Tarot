const invalidQuestionPattern = /^[?\uFFFD锛燂拷\s]+$/;

export const isCorruptedReadingQuestion = (question?: string | null): boolean => {
  const text = (question || '').trim();
  if (!text) {
    return true;
  }

  if (invalidQuestionPattern.test(text)) {
    return true;
  }

  const invalidCount = (text.match(/[?\uFFFD锛燂拷]/g) || []).length;
  return invalidCount > 0 && invalidCount / text.length >= 0.5;
};

export const getReadingQuestionDisplay = (params: {
  question?: string | null;
  questionType?: string | null;
  spreadName?: string | null;
}): string => {
  const { question, questionType, spreadName } = params;
  const cleanQuestion = (question || '').trim();

  if (!isCorruptedReadingQuestion(cleanQuestion)) {
    return cleanQuestion;
  }

  const normalizedType = (questionType || '').trim();
  const normalizedSpread = (spreadName || '').trim().toLowerCase();

  if (normalizedType === 'love') {
    return '这段关系接下来会如何发展？';
  }

  if (normalizedType === 'career') {
    return '我最近的工作方向应该如何调整？';
  }

  if (normalizedType === 'finance') {
    return '我最近的财务决策需要注意什么？';
  }

  if (normalizedType === 'health') {
    return '我最近的状态该如何调节？';
  }

  if (normalizedSpread.includes('past-present-future') || normalizedSpread.includes('过去')) {
    return '这件事接下来最值得我关注的变化是什么？';
  }

  if (normalizedSpread.includes('celtic') || normalizedSpread.includes('凯尔特')) {
    return '我当下面对的核心课题是什么？';
  }

  return '我现在最需要关注什么？';
};
