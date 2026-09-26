import React, { createContext, useContext, useState, useEffect, useMemo } from 'react';

// English bundles
import enCommon from './locales/en/common.json';
import enDashboard from './locales/en/dashboard.json';
import enEvidence from './locales/en/evidence.json';
import enGraph from './locales/en/graph.json';
import enAlerts from './locales/en/alerts.json';
import enTimeline from './locales/en/timeline.json';
import enCopilot from './locales/en/copilot.json';
import enIntegrity from './locales/en/integrity.json';

// Marathi bundles
import mrCommon from './locales/mr/common.json';
import mrDashboard from './locales/mr/dashboard.json';
import mrEvidence from './locales/mr/evidence.json';
import mrGraph from './locales/mr/graph.json';
import mrAlerts from './locales/mr/alerts.json';
import mrTimeline from './locales/mr/timeline.json';
import mrCopilot from './locales/mr/copilot.json';
import mrIntegrity from './locales/mr/integrity.json';

export type SupportedLanguage = 'en' | 'mr';

export interface LanguageOption {
  code: SupportedLanguage;
  label: string;
  nativeLabel: string;
}

export const SUPPORTED_LANGUAGES: LanguageOption[] = [
  { code: 'en', label: 'English', nativeLabel: 'English' },
  { code: 'mr', label: 'Marathi', nativeLabel: 'मराठी' },
];

const bundles: Record<SupportedLanguage, Record<string, any>> = {
  en: {
    common: enCommon,
    dashboard: enDashboard,
    evidence: enEvidence,
    graph: enGraph,
    alerts: enAlerts,
    timeline: enTimeline,
    copilot: enCopilot,
    integrity: enIntegrity,
  },
  mr: {
    common: mrCommon,
    dashboard: mrDashboard,
    evidence: mrEvidence,
    graph: mrGraph,
    alerts: mrAlerts,
    timeline: mrTimeline,
    copilot: mrCopilot,
    integrity: mrIntegrity,
  },
};

interface TranslationContextType {
  language: SupportedLanguage;
  setLanguage: (lang: SupportedLanguage) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
  supportedLanguages: LanguageOption[];
}

const TranslationContext = createContext<TranslationContextType | undefined>(undefined);

const STORAGE_KEY = 'cinip_language';

function resolveKey(bundle: Record<string, any>, key: string): string | undefined {
  const parts = key.split('.');
  let current: any = bundle;
  for (const part of parts) {
    if (current && typeof current === 'object' && part in current) {
      current = current[part];
    } else {
      return undefined;
    }
  }
  return typeof current === 'string' ? current : undefined;
}

export const TranslationProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [language, setLanguageState] = useState<SupportedLanguage>(() => {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved === 'en' || saved === 'mr') {
      return saved;
    }
    return 'en';
  });

  const setLanguage = (newLang: SupportedLanguage) => {
    setLanguageState(newLang);
    try {
      localStorage.setItem(STORAGE_KEY, newLang);
      document.documentElement.setAttribute('lang', newLang);
    } catch {
      // Storage unavailable
    }
  };

  useEffect(() => {
    document.documentElement.setAttribute('lang', language);
  }, [language]);

  const t = useMemo(() => {
    return (key: string, params?: Record<string, string | number>): string => {
      // 1. Try active language bundle
      let text = resolveKey(bundles[language], key);

      // 2. Fallback to English
      if (text === undefined && language !== 'en') {
        text = resolveKey(bundles.en, key);
      }

      // 3. Fallback to key itself
      if (text === undefined) {
        text = key;
      }

      // 4. Parameter substitution if any e.g. {count}
      if (params) {
        Object.entries(params).forEach(([paramKey, val]) => {
          text = text!.replace(new RegExp(`\\{${paramKey}\\}`, 'g'), String(val));
        });
      }

      return text;
    };
  }, [language]);

  return (
    <TranslationContext.Provider value={{ language, setLanguage, t, supportedLanguages: SUPPORTED_LANGUAGES }}>
      {children}
    </TranslationContext.Provider>
  );
};

export const useTranslation = (): TranslationContextType => {
  const context = useContext(TranslationContext);
  if (!context) {
    throw new Error('useTranslation must be used within a TranslationProvider');
  }
  return context;
};
