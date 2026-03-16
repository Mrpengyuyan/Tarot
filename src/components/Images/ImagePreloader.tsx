import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Typography,
  LinearProgress,
  Card,
  CardContent,
  Fade,
  Collapse,
  IconButton,
  Chip,
  Alert,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Cached as CachedIcon,
} from '@mui/icons-material';
import { useBatchImageLoader } from '../../hooks/useImageLoader';
import { ImageQuality } from '../../types/cardImages';
import { getAllCardIds, getMajorArcanaIds, getMinorArcanaIds } from '../../utils/imageUtils';
import { imageService } from '../../services/imageService';

interface ImagePreloaderProps {
  // 预加载配置
  preloadScope?: 'all' | 'major' | 'minor' | 'custom';
  customCardIds?: number[];
  quality?: ImageQuality;
  autoStart?: boolean;

  // UI配置
  showProgress?: boolean;
  showDetails?: boolean;
  compact?: boolean;

  // 回调函数
  onComplete?: (success: number, failed: number) => void;
  onProgress?: (loaded: number, total: number, progress: number) => void;
}

const ImagePreloader: React.FC<ImagePreloaderProps> = ({
  preloadScope = 'all',
  customCardIds = [],
  quality = 'standard',
  autoStart = false,
  showProgress = true,
  showDetails = false,
  compact = false,
  onComplete,
  onProgress,
}) => {
  const [cardIds, setCardIds] = useState<number[]>([]);
  const [detailsExpanded, setDetailsExpanded] = useState(false);
  const [cacheStats, setCacheStats] = useState({ size: 0, count: 0, hitRate: 0 });

  // 获取要预加载的卡片ID列表
  useEffect(() => {
    let ids: number[];

    switch (preloadScope) {
      case 'major':
        ids = getMajorArcanaIds();
        break;
      case 'minor':
        ids = getMinorArcanaIds();
        break;
      case 'custom':
        ids = customCardIds;
        break;
      default:
        ids = getAllCardIds();
        break;
    }

    setCardIds(ids);
  }, [preloadScope, customCardIds]);

  // 使用批量加载钩子
  const { loadProgress, isLoading, preloadImages } = useBatchImageLoader(cardIds, quality);

  // 更新缓存统计
  const updateCacheStats = useCallback(() => {
    const stats = imageService.getCacheStats();
    setCacheStats(stats);
  }, []);

  // 开始预加载
  const startPreload = useCallback(async () => {
    await preloadImages();
    updateCacheStats();

    if (onComplete) {
      onComplete(loadProgress.loaded, loadProgress.failed);
    }
  }, [preloadImages, loadProgress.loaded, loadProgress.failed, onComplete, updateCacheStats]);

  // 自动开始预加载
  useEffect(() => {
    if (autoStart && cardIds.length > 0) {
      startPreload();
    }
  }, [autoStart, cardIds.length, startPreload]);

  // 进度回调
  useEffect(() => {
    if (onProgress) {
      onProgress(loadProgress.loaded, loadProgress.total, loadProgress.progress);
    }
  }, [loadProgress, onProgress]);

  // 获取作用域显示文本
  const getScopeLabel = () => {
    switch (preloadScope) {
      case 'major': return '大阿卡纳';
      case 'minor': return '小阿卡纳';
      case 'custom': return '自定义';
      default: return '全部塔罗牌';
    }
  };

  // 获取质量显示文本
  const getQualityLabel = (q: ImageQuality) => {
    const labels = {
      thumbnail: '缩略图',
      standard: '标准',
      high: '高清',
      ultra: '超高清'
    };
    return labels[q];
  };

  // 紧凑模式渲染
  if (compact) {
    return (
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
        {isLoading && (
          <>
            <LinearProgress
              variant="determinate"
              value={loadProgress.progress}
              sx={{ flex: 1, height: 6, borderRadius: 3 }}
            />
            <Typography variant="caption" sx={{ minWidth: 60 }}>
              {Math.round(loadProgress.progress)}%
            </Typography>
          </>
        )}

        {!isLoading && loadProgress.total > 0 && (
          <Chip
            icon={loadProgress.failed > 0 ? <ErrorIcon /> : <CheckCircleIcon />}
            label={`${loadProgress.loaded}/${loadProgress.total}`}
            color={loadProgress.failed > 0 ? 'warning' : 'success'}
            size="small"
          />
        )}
      </Box>
    );
  }

  return (
    <Card sx={{ mb: 2 }}>
      <CardContent sx={{ pb: showDetails ? 2 : '16px !important' }}>
        {/* 标题和配置信息 */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6" component="h3">
            图片预加载
          </Typography>

          <Box sx={{ display: 'flex', gap: 1 }}>
            <Chip label={getScopeLabel()} size="small" variant="outlined" />
            <Chip label={getQualityLabel(quality)} size="small" color="primary" />
          </Box>
        </Box>

        {/* 主要状态显示 */}
        <Box sx={{ mb: 2 }}>
          {!isLoading && loadProgress.total === 0 && (
            <Alert severity="info">
              点击开始预加载 {cardIds.length} 张塔罗牌图片
            </Alert>
          )}

          {isLoading && (
            <Box>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                <Typography variant="body2">
                  正在加载图片... ({loadProgress.loaded + loadProgress.failed}/{loadProgress.total})
                </Typography>
                <Typography variant="caption">
                  {Math.round(loadProgress.progress)}%
                </Typography>
              </Box>

              {showProgress && (
                <LinearProgress
                  variant="determinate"
                  value={loadProgress.progress}
                  sx={{ height: 8, borderRadius: 4 }}
                />
              )}
            </Box>
          )}

          {!isLoading && loadProgress.total > 0 && (
            <Box>
              <Alert
                severity={loadProgress.failed > 0 ? 'warning' : 'success'}
                icon={loadProgress.failed > 0 ? <ErrorIcon /> : <CheckCircleIcon />}
              >
                预加载完成：成功 {loadProgress.loaded} 张，失败 {loadProgress.failed} 张
              </Alert>
            </Box>
          )}
        </Box>

        {/* 操作按钮 */}
        <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
          {!isLoading && (
            <IconButton
              onClick={startPreload}
              color="primary"
              disabled={cardIds.length === 0}
            >
              <CachedIcon />
            </IconButton>
          )}

          {showDetails && (
            <IconButton
              onClick={() => setDetailsExpanded(!detailsExpanded)}
              size="small"
            >
              {detailsExpanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            </IconButton>
          )}
        </Box>

        {/* 详细信息 */}
        <Collapse in={detailsExpanded}>
          <Box sx={{ mt: 2, pt: 2, borderTop: '1px solid', borderColor: 'divider' }}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              预加载详情
            </Typography>

            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(120px, 1fr))', gap: 2 }}>
              <Box>
                <Typography variant="caption" color="text.secondary">
                  预加载范围
                </Typography>
                <Typography variant="body2">
                  {getScopeLabel()} ({cardIds.length} 张)
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" color="text.secondary">
                  图片质量
                </Typography>
                <Typography variant="body2">
                  {getQualityLabel(quality)}
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" color="text.secondary">
                  加载进度
                </Typography>
                <Typography variant="body2">
                  {loadProgress.loaded}/{loadProgress.total}
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" color="text.secondary">
                  失败数量
                </Typography>
                <Typography variant="body2" color={loadProgress.failed > 0 ? 'error.main' : 'text.primary'}>
                  {loadProgress.failed}
                </Typography>
              </Box>
            </Box>

            {/* 缓存统计 */}
            <Box sx={{ mt: 2 }}>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>
                缓存统计
              </Typography>

              <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(100px, 1fr))', gap: 2 }}>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    缓存大小
                  </Typography>
                  <Typography variant="body2">
                    {cacheStats.size.toFixed(1)} MB
                  </Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="text.secondary">
                    缓存数量
                  </Typography>
                  <Typography variant="body2">
                    {cacheStats.count} 项
                  </Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="text.secondary">
                    命中率
                  </Typography>
                  <Typography variant="body2">
                    {(cacheStats.hitRate * 100).toFixed(1)}%
                  </Typography>
                </Box>
              </Box>
            </Box>
          </Box>
        </Collapse>
      </CardContent>
    </Card>
  );
};

export default ImagePreloader;