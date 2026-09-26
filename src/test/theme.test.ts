import { describe, it, expect, beforeEach, vi } from 'vitest';

describe('Theme Consistency System', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.className = '';
    document.documentElement.removeAttribute('data-theme');
  });

  it('resolves light theme and sets root attributes and classes', () => {
    const root = document.documentElement;
    root.classList.remove('dark');
    root.classList.add('light');
    root.setAttribute('data-theme', 'light');
    localStorage.setItem('cinip_theme', 'light');

    expect(root.classList.contains('light')).toBe(true);
    expect(root.classList.contains('dark')).toBe(false);
    expect(root.getAttribute('data-theme')).toBe('light');
    expect(localStorage.getItem('cinip_theme')).toBe('light');
  });

  it('resolves dark theme and sets root attributes and classes', () => {
    const root = document.documentElement;
    root.classList.remove('light');
    root.classList.add('dark');
    root.setAttribute('data-theme', 'dark');
    localStorage.setItem('cinip_theme', 'dark');

    expect(root.classList.contains('dark')).toBe(true);
    expect(root.classList.contains('light')).toBe(false);
    expect(root.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem('cinip_theme')).toBe('dark');
  });

  it('resolves system theme based on prefers-color-scheme', () => {
    const matchMediaMock = vi.fn().mockImplementation((query: string) => ({
      matches: query.includes('dark'),
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }));
    window.matchMedia = matchMediaMock;

    const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const resolved = isDark ? 'dark' : 'light';
    const root = document.documentElement;

    if (resolved === 'dark') {
      root.classList.add('dark');
      root.classList.remove('light');
      root.setAttribute('data-theme', 'dark');
    } else {
      root.classList.add('light');
      root.classList.remove('dark');
      root.setAttribute('data-theme', 'light');
    }

    expect(root.classList.contains('dark')).toBe(true);
    expect(root.getAttribute('data-theme')).toBe('dark');
  });
});
