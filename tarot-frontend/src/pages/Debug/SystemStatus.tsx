import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Button,
  Alert,
  CircularProgress,
  Chip,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Paper,
} from '@mui/material';
import {
  CheckCircle,
  Error,
  Refresh,
  ExpandMore,
  BugReport,
  Settings,
  Cloud,
  Storage,
  Psychology,
} from '@mui/icons-material';
import {
  runSystemTests,
  validateEnvironment,
  getDebugInfo,
} from '../../utils/testUtils';
import { config } from '../../config/env';

interface TestResult {
  success: boolean;
  message: string;
  details?: any;
}

interface SystemTestResults {
  overall: boolean;
  results: {
    api: TestResult;
    database: TestResult;
    ai: TestResult;
  };
}

const SystemStatus: React.FC = () => {
  const [testing, setTesting] = useState(false);
  const [testResults, setTestResults] = useState<SystemTestResults | null>(null);
  const [environmentStatus, setEnvironmentStatus] = useState(validateEnvironment());
  const [debugInfo] = useState(getDebugInfo());

  useEffect(() => {
    // 组件加载时自动运行测试
    handleRunTests();
  }, []);

  const handleRunTests = async () => {
    setTesting(true);
    try {
      const results = await runSystemTests();
      setTestResults(results);
      setEnvironmentStatus(validateEnvironment());
    } catch (error) {
      console.error('系统测试失败:', error);
    } finally {
      setTesting(false);
    }
  };

  const getStatusIcon = (success: boolean) => {
    return success ? (
      <CheckCircle sx={{ color: 'success.main' }} />
    ) : (
      <Error sx={{ color: 'error.main' }} />
    );
  };

  const getStatusColor = (success: boolean) => {
    return success ? 'success' : 'error';
  };

  return (
    <Box sx={{ p: 3, maxWidth: 1200, mx: 'auto' }}>
      {/* 页面标题 */}
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <BugReport sx={{ fontSize: '3rem', color: 'primary.main', mb: 2 }} />
        <Typography
          variant="h4"
          sx={{
            fontFamily: 'Cinzel, serif',
            fontWeight: 700,
            color: 'primary.main',
            mb: 2,
          }}
        >
          系统状态检查
        </Typography>
        <Typography variant="body1" sx={{ color: 'text.secondary', mb: 3 }}>
          检查API连接、数据库状态、AI服务等系统组件的运行状况
        </Typography>
        <Button
          variant="contained"
          startIcon={testing ? <CircularProgress size={20} /> : <Refresh />}
          onClick={handleRunTests}
          disabled={testing}
          sx={{
            background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
            color: 'black',
            '&:hover': {
              background: 'linear-gradient(45deg, #B8860B, #D4AF37)',
            },
          }}
        >
          {testing ? '测试中...' : '重新测试'}
        </Button>
      </Box>

      {/* 环境配置状态 */}
      <Card sx={{ mb: 3, background: 'rgba(26, 26, 46, 0.3)' }}>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Settings />
            环境配置状态
          </Typography>

          {environmentStatus.errors.length > 0 && (
            <Alert severity="error" sx={{ mb: 2 }}>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>配置错误:</Typography>
              <ul style={{ margin: 0, paddingLeft: 20 }}>
                {environmentStatus.errors.map((error, index) => (
                  <li key={index}>{error}</li>
                ))}
              </ul>
            </Alert>
          )}

          {environmentStatus.warnings.length > 0 && (
            <Alert severity="warning" sx={{ mb: 2 }}>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>配置警告:</Typography>
              <ul style={{ margin: 0, paddingLeft: 20 }}>
                {environmentStatus.warnings.map((warning, index) => (
                  <li key={index}>{warning}</li>
                ))}
              </ul>
            </Alert>
          )}

          {environmentStatus.isValid && environmentStatus.warnings.length === 0 && (
            <Alert severity="success">
              所有环境配置正常
            </Alert>
          )}

          <Box sx={{ mt: 2 }}>
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              API地址: {config.apiBaseUrl}
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              AI服务: {config.coze.botId ? '已配置' : '未配置'}
            </Typography>
          </Box>
        </CardContent>
      </Card>

      {/* 系统测试结果 */}
      {testResults && (
        <Card sx={{ mb: 3, background: 'rgba(26, 26, 46, 0.3)' }}>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
              {getStatusIcon(testResults.overall)}
              系统测试结果
              <Chip
                label={testResults.overall ? '全部通过' : '存在问题'}
                color={getStatusColor(testResults.overall)}
                size="small"
              />
            </Typography>

            <List>
              {/* API连接测试 */}
              <ListItem>
                <ListItemIcon>
                  <Cloud sx={{ color: testResults.results.api.success ? 'success.main' : 'error.main' }} />
                </ListItemIcon>
                <ListItemText
                  primary="API连接"
                  secondary={testResults.results.api.message}
                />
                <Chip
                  label={testResults.results.api.success ? '正常' : '异常'}
                  color={getStatusColor(testResults.results.api.success)}
                  size="small"
                />
              </ListItem>

              {/* 数据库连接测试 */}
              <ListItem>
                <ListItemIcon>
                  <Storage sx={{ color: testResults.results.database.success ? 'success.main' : 'error.main' }} />
                </ListItemIcon>
                <ListItemText
                  primary="数据库连接"
                  secondary={testResults.results.database.message}
                />
                <Chip
                  label={testResults.results.database.success ? '正常' : '异常'}
                  color={getStatusColor(testResults.results.database.success)}
                  size="small"
                />
              </ListItem>

              {/* AI服务测试 */}
              <ListItem>
                <ListItemIcon>
                  <Psychology sx={{ color: testResults.results.ai.success ? 'success.main' : 'error.main' }} />
                </ListItemIcon>
                <ListItemText
                  primary="AI服务"
                  secondary={testResults.results.ai.message}
                />
                <Chip
                  label={testResults.results.ai.success ? '正常' : '异常'}
                  color={getStatusColor(testResults.results.ai.success)}
                  size="small"
                />
              </ListItem>
            </List>
          </CardContent>
        </Card>
      )}

      {/* 详细调试信息 */}
      <Accordion sx={{ background: 'rgba(26, 26, 46, 0.3)' }}>
        <AccordionSummary expandIcon={<ExpandMore />}>
          <Typography variant="h6">详细调试信息</Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Paper elevation={0} sx={{ p: 2, background: 'rgba(0, 0, 0, 0.2)' }}>
            <Typography variant="subtitle2" sx={{ mb: 1, color: 'primary.main' }}>
              环境信息:
            </Typography>
            <pre style={{ fontSize: '0.8rem', color: '#ccc', overflow: 'auto' }}>
              {JSON.stringify(debugInfo, null, 2)}
            </pre>
          </Paper>

          {testResults && (
            <Paper elevation={0} sx={{ p: 2, mt: 2, background: 'rgba(0, 0, 0, 0.2)' }}>
              <Typography variant="subtitle2" sx={{ mb: 1, color: 'primary.main' }}>
                测试结果详情:
              </Typography>
              <pre style={{ fontSize: '0.8rem', color: '#ccc', overflow: 'auto' }}>
                {JSON.stringify(testResults.results, null, 2)}
              </pre>
            </Paper>
          )}
        </AccordionDetails>
      </Accordion>

      {/* 帮助信息 */}
      <Alert severity="info" sx={{ mt: 3 }}>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>使用说明:</Typography>
        <ul style={{ margin: 0, paddingLeft: 20 }}>
          <li>此页面用于检查系统各组件的运行状态</li>
          <li>如果发现问题，请检查对应的配置文件</li>
          <li>确保后端服务正在运行在正确的端口</li>
          <li>AI服务需要正确配置Coze Bot ID和Access Token</li>
        </ul>
      </Alert>
    </Box>
  );
};

export default SystemStatus;