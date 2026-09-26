import { apiClient } from '../api/client';

export interface TranslationRequestDto {
  text: string;
  sourceLanguage?: string;
  targetLanguage?: string;
  evidenceId?: string;
}

export interface TranslationResponseDto {
  originalText: string;
  translatedText: string;
  detectedSourceLanguage: string;
  targetLanguage: string;
  provider: string;
  isMachineTranslation: boolean;
  disclaimer: string;
  translatedAtUtc: string;
}

export interface TranslationProviderStatusDto {
  configuredProvider: string;
  isGoogleCloudAvailable: boolean;
  fallbackAvailable: boolean;
  supportedLanguages: string[];
}

export const translationService = {
  async translate(request: TranslationRequestDto): Promise<TranslationResponseDto> {
    return await apiClient.post<TranslationResponseDto>('/translation/translate', request);
  },

  async getProviderStatus(): Promise<TranslationProviderStatusDto> {
    return await apiClient.get<TranslationProviderStatusDto>('/translation/provider-status');
  }
};
