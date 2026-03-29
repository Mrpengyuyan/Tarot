import React, { useMemo } from 'react';
import {
  Box,
  Chip,
  FormControl,
  FormHelperText,
  InputLabel,
  ListSubheader,
  MenuItem,
  Select as MuiSelect,
  SelectChangeEvent,
} from '@mui/material';
import { AutoAwesome } from 'icons';

export interface SelectOption {
  value: string | number;
  label: string;
  disabled?: boolean;
  icon?: React.ReactNode;
  description?: string;
}

export interface SelectOptionGroup {
  label: string;
  options: SelectOption[];
}

export interface SelectProps {
  name: string;
  label?: string;
  value?: string | number | string[] | number[];
  options?: SelectOption[] | SelectOptionGroup[];
  placeholder?: string;
  multiple?: boolean;
  required?: boolean;
  disabled?: boolean;
  error?: boolean;
  helperText?: string;
  size?: 'small' | 'medium';
  variant?: 'outlined' | 'filled' | 'standard';
  style?: 'default' | 'mystical';
  fullWidth?: boolean;
  autoFocus?: boolean;
  className?: string;
  renderValue?: (selected: unknown) => React.ReactNode;
  onChange?: (event: SelectChangeEvent<any>) => void;
  onBlur?: (event: React.FocusEvent<HTMLInputElement>) => void;
  onFocus?: (event: React.FocusEvent<HTMLInputElement>) => void;
}

const isOptionGroup = (option: SelectOption | SelectOptionGroup): option is SelectOptionGroup =>
  'options' in option;

const flattenOptions = (options: SelectProps['options'] = []): SelectOption[] =>
  options.flatMap((item) => (isOptionGroup(item) ? item.options : [item]));

const Select: React.FC<SelectProps> = ({
  name,
  label,
  value,
  options = [],
  placeholder,
  multiple = false,
  required = false,
  disabled = false,
  error = false,
  helperText,
  size = 'medium',
  variant = 'outlined',
  style = 'default',
  fullWidth = true,
  autoFocus = false,
  className,
  renderValue,
  onChange,
  onBlur,
  onFocus,
}) => {
  const resolvedValue = value ?? (multiple ? [] : '');
  const optionList = useMemo(() => flattenOptions(options), [options]);

  const defaultRenderValue = (selected: unknown) => {
    if (multiple && Array.isArray(selected)) {
      if (selected.length === 0) {
        return placeholder || '请选择';
      }

      return (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
          {selected.map((item) => {
            const option = optionList.find((entry) => String(entry.value) === String(item));
            return <Chip key={String(item)} label={option?.label ?? String(item)} size="small" />;
          })}
        </Box>
      );
    }

    if (selected === '' || selected === undefined || selected === null) {
      return placeholder || '请选择';
    }

    const option = optionList.find((entry) => String(entry.value) === String(selected));
    return option?.label ?? String(selected);
  };

  return (
    <FormControl
      fullWidth={fullWidth}
      required={required}
      disabled={disabled}
      error={error}
      size={size}
      variant={variant}
      className={className}
      sx={{
        '& .MuiOutlinedInput-root': {
          backgroundColor: style === 'mystical' ? 'rgba(16, 8, 32, 0.7)' : undefined,
        },
      }}
    >
      {label && (
        <InputLabel id={`${name}-label`} sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
          {style === 'mystical' && <AutoAwesome sx={{ fontSize: 16 }} />}
          {label}
        </InputLabel>
      )}

      <MuiSelect
        id={name}
        labelId={label ? `${name}-label` : undefined}
        value={resolvedValue as any}
        label={label}
        multiple={multiple}
        autoFocus={autoFocus}
        displayEmpty={Boolean(placeholder)}
        renderValue={renderValue ?? defaultRenderValue}
        onChange={onChange}
        onBlur={onBlur}
        onFocus={onFocus}
      >
        {options.map((option, index) => {
          if (isOptionGroup(option)) {
            return [
              <ListSubheader key={`group-${option.label}-${index}`}>{option.label}</ListSubheader>,
              ...option.options.map((child) => (
                <MenuItem key={`${option.label}-${child.value}`} value={child.value} disabled={child.disabled}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    {child.icon}
                    <Box>
                      <Box component="span">{child.label}</Box>
                      {child.description && (
                        <Box component="div" sx={{ color: 'text.secondary', fontSize: 12 }}>
                          {child.description}
                        </Box>
                      )}
                    </Box>
                  </Box>
                </MenuItem>
              )),
            ];
          }

          return (
            <MenuItem key={String(option.value)} value={option.value} disabled={option.disabled}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                {option.icon}
                <Box>
                  <Box component="span">{option.label}</Box>
                  {option.description && (
                    <Box component="div" sx={{ color: 'text.secondary', fontSize: 12 }}>
                      {option.description}
                    </Box>
                  )}
                </Box>
              </Box>
            </MenuItem>
          );
        })}
      </MuiSelect>

      {helperText && <FormHelperText>{helperText}</FormHelperText>}
    </FormControl>
  );
};

export const MysticalSelect: React.FC<Omit<SelectProps, 'style'>> = (props) => (
  <Select {...props} style="mystical" />
);

export const MultiSelect: React.FC<Omit<SelectProps, 'multiple'>> = (props) => (
  <Select {...props} multiple />
);

export default Select;

