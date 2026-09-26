import React from 'react';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost' | 'outline';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
  icon?: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'secondary',
  size = 'md',
  isLoading = false,
  icon,
  className = '',
  disabled,
  id,
  ...props
}) => {
  let variantStyles = 'bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] hover:bg-[var(--color-border)] border border-[var(--color-border)]';

  switch (variant) {
    case 'primary':
      variantStyles =
        'bg-[var(--color-accent)] text-white hover:bg-[var(--color-accent-hover)] border border-[var(--color-accent)] shadow-xs focus-visible:ring-2 focus-visible:ring-[var(--color-accent)]/40';
      break;
    case 'secondary':
      variantStyles =
        'bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] hover:bg-[var(--color-border-subtle)] border border-[var(--color-border)] shadow-2xs focus-visible:ring-2 focus-visible:ring-[var(--color-accent)]/30';
      break;
    case 'danger':
      variantStyles =
        'bg-red-500/15 text-red-500 hover:bg-red-500/25 border border-red-500/30 focus-visible:ring-2 focus-visible:ring-red-400';
      break;
    case 'outline':
      variantStyles =
        'bg-transparent text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] border border-[var(--color-border)] focus-visible:ring-2 focus-visible:ring-[var(--color-accent)]/30';
      break;
    case 'ghost':
      variantStyles =
        'bg-transparent text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)] hover:text-[var(--color-text-primary)] border border-transparent focus-visible:ring-2 focus-visible:ring-[var(--color-accent)]/30';
      break;
  }

  // Horizontal padding is 2x vertical padding
  let sizeStyles = 'py-2 px-4 text-xs tracking-wide min-h-[38px]';
  if (size === 'sm') {
    sizeStyles = 'py-1.5 px-3 text-xs tracking-wide min-h-[32px]';
  } else if (size === 'lg') {
    sizeStyles = 'py-2.5 px-5 text-sm tracking-wide min-h-[44px]';
  }

  return (
    <button
      id={id}
      disabled={disabled || isLoading}
      className={`inline-flex items-center justify-center gap-2 font-medium rounded-md transition-colors whitespace-nowrap cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed outline-none ${sizeStyles} ${variantStyles} ${className}`}
      {...props}
    >
      {isLoading ? (
        <span className="w-3.5 h-3.5 border-2 border-current border-t-transparent rounded-full animate-spin" />
      ) : (
        icon
      )}
      <span>{children}</span>
    </button>
  );
};
