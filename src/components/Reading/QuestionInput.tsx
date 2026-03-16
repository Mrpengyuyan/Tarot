import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  TextField,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Paper,
  Alert,
  Fade,
  Chip,
  LinearProgress,
  Card,
  CardContent,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Collapse,
} from '@mui/material';
import {
  Psychology,
  Lightbulb,
  Favorite,
  TrendingUp,
  Balance,
  LocalHospital,
  AutoAwesome,
  ExpandMore,
  ExpandLess,
  Help,
} from '@mui/icons-material';
import { QuestionType, getQuestionTypeLabel } from '../../stores/gameStore';
import { promptService } from '../../services/promptService';

interface QuestionInputProps {
  initialQuestion?: string;
  initialQuestionType?: QuestionType;
  onQuestionSubmit: (question: string, questionType: QuestionType) => void;
  selectedSpreadName?: string;
}

const getQuestionTypeIcon = (type: QuestionType) => {
  switch (type) {
    case 'love': return <Favorite sx={{ color: '#e91e63' }} />;
    case 'career': return <TrendingUp sx={{ color: '#2196f3' }} />;
    case 'finance': return <Balance sx={{ color: '#4caf50' }} />;
    case 'health': return <LocalHospital sx={{ color: '#ff9800' }} />;
    default: return <AutoAwesome sx={{ color: '#9c27b0' }} />;
  }
};

const QuestionInput: React.FC<QuestionInputProps> = ({
  initialQuestion = '',
  initialQuestionType = 'general',
  onQuestionSubmit,
  selectedSpreadName = ''
}) => {
  const [question, setQuestion] = useState(initialQuestion);
  const [questionType, setQuestionType] = useState<QuestionType>(initialQuestionType);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [suggestedType, setSuggestedType] = useState<QuestionType | null>(null);
  const [isValidating, setIsValidating] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [showGuidelines, setShowGuidelines] = useState(false);

  // 问题示例
  const questionExamples: Record<QuestionType, string[]> = {
    love: [
      '我和现任的关系会如何发展？',
      '我什么时候会遇到真爱？',
      '如何改善我们之间的沟通？',
      '这段感情的未来走向如何？',
    ],
    career: [
      '我的职业发展前景如何？',
      '现在是换工作的好时机吗？',
      '如何提升我的工作能力？',
      '我适合创业吗？',
    ],
    finance: [
      '我的财运在近期会如何？',
      '这个投资决策是否明智？',
      '如何改善我的财务状况？',
      '我应该如何理财？',
    ],
    health: [
      '我的身体状况如何？',
      '如何改善我的健康？',
      '这个治疗方案适合我吗？',
      '如何保持身心平衡？',
    ],
    general: [
      '我现在最需要关注什么？',
      '接下来的人生方向是什么？',
      '如何做出正确的选择？',
      '我的整体运势如何？',
    ],
  };

  // 自动检测问题类型
  useEffect(() => {
    if (question.length > 10) {
      setIsValidating(true);
      const timer = setTimeout(() => {
        const suggested = promptService.suggestQuestionType(question) as QuestionType;
        if (suggested !== questionType && suggested !== 'general') {
          setSuggestedType(suggested);
        } else {
          setSuggestedType(null);
        }
        setIsValidating(false);
      }, 500);

      return () => clearTimeout(timer);
    } else {
      setSuggestedType(null);
      setIsValidating(false);
    }
  }, [question, questionType]);

  // 验证问题内容
  const validateQuestion = (questionText: string): string | null => {
    if (!questionText.trim()) {
      return '请输入您的问题';
    }
    if (questionText.trim().length < 10) {
      return '问题应该至少包含10个字符，以便获得更准确的解读';
    }
    if (questionText.trim().length > 200) {
      return '问题过长，请简洁地表达您的核心疑问';
    }
    return null;
  };

  const handleQuestionChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const newQuestion = event.target.value;
    setQuestion(newQuestion);
    setValidationError(validateQuestion(newQuestion));
  };

  const handleQuestionTypeChange = (newType: QuestionType) => {
    setQuestionType(newType);
    setSuggestedType(null);
  };

  const handleAcceptSuggestion = () => {
    if (suggestedType) {
      setQuestionType(suggestedType);
      setSuggestedType(null);
    }
  };

  const handleExampleClick = (example: string) => {
    setQuestion(example);
    setValidationError(validateQuestion(example));
  };

  const handleSubmit = () => {
    const error = validateQuestion(question);
    if (error) {
      setValidationError(error);
      return;
    }

    onQuestionSubmit(question.trim(), questionType);
  };

  const getQuestionTypeConfig = (type: QuestionType) => {
    return promptService.getQuestionTypeConfig(type);
  };

  const isFormValid = !validateQuestion(question) && question.trim().length >= 10;

  return (
    <Box sx={{ p: 3, maxWidth: 800, mx: 'auto' }}>
      {/* 标题 */}
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <Psychology sx={{ fontSize: '3rem', color: 'primary.main', mb: 2 }} />
        <Typography
          variant="h4"
          sx={{
            fontFamily: 'Cinzel, serif',
            fontWeight: 700,
            color: 'primary.main',
            mb: 2,
          }}
        >
          诉说您的心声
        </Typography>
        <Typography
          variant="body1"
          sx={{
            color: 'text.secondary',
            fontStyle: 'italic',
            mb: 2,
          }}
        >
          清晰而真诚地表达您的问题，塔罗牌将为您带来最精准的指引
        </Typography>
        {selectedSpreadName && (
          <Chip
            label={`已选择牌阵：${selectedSpreadName}`}
            color="primary"
            variant="outlined"
            sx={{ fontFamily: 'Cinzel, serif' }}
          />
        )}
      </Box>

      {/* 问题输入区域 */}
      <Card sx={{ mb: 3, background: 'rgba(26, 26, 46, 0.3)' }}>
        <CardContent sx={{ p: 3 }}>
          {/* 问题类型选择 */}
          <FormControl fullWidth sx={{ mb: 3 }}>
            <InputLabel>问题类型</InputLabel>
            <Select
              value={questionType}
              label="问题类型"
              onChange={(e) => handleQuestionTypeChange(e.target.value as QuestionType)}
            >
              {Object.entries(questionExamples).map(([type, _]) => (
                <MenuItem key={type} value={type}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    {getQuestionTypeIcon(type as QuestionType)}
                    {getQuestionTypeLabel(type as QuestionType)}
                  </Box>
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* AI建议的问题类型 */}
          {isValidating && (
            <Box sx={{ mb: 2 }}>
              <Typography variant="caption" sx={{ color: 'text.secondary', mb: 1, display: 'block' }}>
                正在分析问题类型...
              </Typography>
              <LinearProgress sx={{ height: 2, borderRadius: 1 }} />
            </Box>
          )}

          {suggestedType && (
            <Fade in>
              <Alert
                severity="info"
                action={
                  <Button color="inherit" size="small" onClick={handleAcceptSuggestion}>
                    采用建议
                  </Button>
                }
                sx={{ mb: 2 }}
              >
                根据您的问题内容，建议选择「{getQuestionTypeLabel(suggestedType)}」类型
              </Alert>
            </Fade>
          )}

          {/* 问题输入框 */}
          <TextField
            fullWidth
            multiline
            rows={4}
            label="您的问题"
            placeholder="请详细描述您想要了解的问题..."
            value={question}
            onChange={handleQuestionChange}
            error={!!validationError}
            helperText={validationError || `${question.length}/200 字符`}
            sx={{
              mb: 2,
              '& .MuiOutlinedInput-root': {
                '&:hover fieldset': {
                  borderColor: 'primary.main',
                },
              },
            }}
          />

          {/* 输入指南 */}
          <Box sx={{ mb: 2 }}>
            <Button
              variant="text"
              startIcon={showGuidelines ? <ExpandLess /> : <ExpandMore />}
              onClick={() => setShowGuidelines(!showGuidelines)}
              sx={{ color: 'text.secondary' }}
            >
              输入指南
            </Button>
            <Collapse in={showGuidelines}>
              <Paper elevation={0} sx={{ p: 2, mt: 1, background: 'rgba(0, 0, 0, 0.2)' }}>
                <Typography variant="subtitle2" sx={{ mb: 1, color: 'primary.main' }}>
                  如何提出好的问题：
                </Typography>
                <List dense>
                  <ListItem>
                    <ListItemIcon>
                      <Lightbulb sx={{ fontSize: '1rem' }} />
                    </ListItemIcon>
                    <ListItemText
                      primary="具体明确"
                      secondary="避免过于宽泛的问题，聚焦具体的情况或决策"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <Psychology sx={{ fontSize: '1rem' }} />
                    </ListItemIcon>
                    <ListItemText
                      primary="开放结尾"
                      secondary="用'如何'、'什么'开头，而非'是否'、'会不会'"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <Help sx={{ fontSize: '1rem' }} />
                    </ListItemIcon>
                    <ListItemText
                      primary="内心真实"
                      secondary="诚实面对自己的内心，问题越真诚答案越准确"
                    />
                  </ListItem>
                </List>
              </Paper>
            </Collapse>
          </Box>

          {/* 问题示例 */}
          <Box>
            <Button
              variant="text"
              startIcon={showSuggestions ? <ExpandLess /> : <ExpandMore />}
              onClick={() => setShowSuggestions(!showSuggestions)}
              sx={{ color: 'text.secondary' }}
            >
              查看{getQuestionTypeLabel(questionType)}类问题示例
            </Button>
            <Collapse in={showSuggestions}>
              <Box sx={{ mt: 2 }}>
                <Typography variant="subtitle2" sx={{ mb: 1, color: 'primary.main' }}>
                  点击使用示例问题：
                </Typography>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                  {questionExamples[questionType].map((example, index) => (
                    <Chip
                      key={index}
                      label={example}
                      variant="outlined"
                      clickable
                      onClick={() => handleExampleClick(example)}
                      sx={{
                        '&:hover': {
                          backgroundColor: 'primary.main',
                          color: 'primary.contrastText',
                        },
                      }}
                    />
                  ))}
                </Box>
              </Box>
            </Collapse>
          </Box>
        </CardContent>
      </Card>

      {/* 提交按钮 */}
      <Box sx={{ textAlign: 'center' }}>
        <Button
          variant="contained"
          size="large"
          onClick={handleSubmit}
          disabled={!isFormValid}
          sx={{
            py: 2,
            px: 6,
            fontSize: '1.2rem',
            fontFamily: 'Cinzel, serif',
            fontWeight: 600,
            background: isFormValid
              ? 'linear-gradient(45deg, #D4AF37, #FFD700)'
              : 'rgba(255, 255, 255, 0.12)',
            color: isFormValid ? 'black' : 'text.disabled',
            boxShadow: isFormValid ? '0 8px 32px rgba(212, 175, 55, 0.3)' : 'none',
            '&:hover': isFormValid ? {
              background: 'linear-gradient(45deg, #B8860B, #D4AF37)',
              boxShadow: '0 12px 40px rgba(212, 175, 55, 0.4)',
              transform: 'translateY(-2px)',
            } : {},
          }}
        >
          开始抽牌
        </Button>
        <Typography
          variant="body2"
          sx={{
            color: 'text.secondary',
            mt: 2,
            fontStyle: 'italic',
          }}
        >
          "问题即是答案的开始，真诚的疑问将得到真诚的回应"
        </Typography>
      </Box>
    </Box>
  );
};

export default QuestionInput;