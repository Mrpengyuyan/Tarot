import React, { useEffect, useState } from 'react';
import { Button, ButtonProps } from '@mui/material';
import { Favorite } from 'icons';
import { tarotService } from '../../services/tarotService';
import { useNotification } from '../UI/Notification';

interface ReadingFavoriteButtonProps {
  readingId: number;
  isFavorite?: boolean;
  onChanged?: (nextValue: boolean) => void;
  size?: ButtonProps['size'];
  fullWidth?: boolean;
  variant?: ButtonProps['variant'];
}

const ReadingFavoriteButton: React.FC<ReadingFavoriteButtonProps> = ({
  readingId,
  isFavorite = false,
  onChanged,
  size = 'small',
  fullWidth = false,
  variant,
}) => {
  const { showError, showSuccess } = useNotification();
  const [favorite, setFavorite] = useState(Boolean(isFavorite));
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    setFavorite(Boolean(isFavorite));
  }, [isFavorite]);

  const handleToggle = async (event: React.MouseEvent<HTMLButtonElement>) => {
    event.stopPropagation();
    if (submitting) {
      return;
    }

    const nextValue = !favorite;
    setSubmitting(true);

    try {
      await tarotService.toggleFavorite(readingId, nextValue);
      setFavorite(nextValue);
      onChanged?.(nextValue);
      showSuccess(nextValue ? '已加入收藏' : '已取消收藏');
    } catch (error) {
      const message = error instanceof Error ? error.message : '收藏状态更新失败';
      showError(message);
    } finally {
      setSubmitting(false);
    }
  };

  const activeVariant = variant || (favorite ? 'contained' : 'outlined');

  return (
    <Button
      size={size}
      fullWidth={fullWidth}
      variant={activeVariant}
      startIcon={<Favorite fontSize="small" />}
      disabled={submitting}
      onClick={handleToggle}
      sx={{
        minWidth: 120,
        borderColor: 'rgba(212, 175, 55, 0.35)',
        color: favorite ? '#120818' : 'primary.main',
        background: favorite ? 'linear-gradient(135deg, #D4AF37 0%, #F6D365 100%)' : 'transparent',
        boxShadow: favorite ? '0 10px 24px rgba(212, 175, 55, 0.25)' : 'none',
        '&:hover': {
          borderColor: 'primary.main',
          background: favorite
            ? 'linear-gradient(135deg, #C79C18 0%, #EAC051 100%)'
            : 'rgba(212, 175, 55, 0.08)',
        },
      }}
    >
      {submitting ? '处理中...' : favorite ? '已收藏' : '加入收藏'}
    </Button>
  );
};

export default ReadingFavoriteButton;
