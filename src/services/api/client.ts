import { env } from '../../config/env';
import { ApiError, normalizeHttpError } from './errors';

export interface RequestOptions extends RequestInit {
  timeoutMs?: number;
  params?: Record<string, string | number | boolean | undefined>;
}

class ApiClient {
  private baseUrl: string;
  private token: string | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  public setToken(token: string | null) {
    this.token = token;
  }

  public getToken(): string | null {
    return this.token;
  }

  private buildUrl(path: string, params?: Record<string, string | number | boolean | undefined>): string {
    let cleanPath = path.startsWith('/') ? path : `/${path}`;

    // Normalize routes to the ASP.NET Core 8 API endpoints
    if (!cleanPath.startsWith('/health')) {
      if (cleanPath.startsWith('/api/v1/')) {
        // already v1 versioned
      } else if (cleanPath.startsWith('/api/')) {
        cleanPath = '/api/v1' + cleanPath.substring(4);
      } else {
        cleanPath = '/api/v1' + cleanPath;
      }
    }

    const fullUrl = this.baseUrl ? `${this.baseUrl}${cleanPath}` : cleanPath;

    if (!params) return fullUrl;

    const query = new URLSearchParams();
    Object.entries(params).forEach(([key, val]) => {
      if (val !== undefined && val !== null) {
        query.append(key, String(val));
      }
    });

    const queryString = query.toString();
    return queryString ? `${fullUrl}?${queryString}` : fullUrl;
  }

  public async request<T>(path: string, options: RequestOptions = {}): Promise<T> {
    const { timeoutMs = 15000, params, signal: callerSignal, ...fetchOptions } = options;

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

    // If caller provided an abort signal, connect it
    if (callerSignal) {
      callerSignal.addEventListener('abort', () => controller.abort());
    }

    const headers = new Headers(fetchOptions.headers || {});
    if (!headers.has('Accept')) {
      headers.set('Accept', 'application/json');
    }
    if (!headers.has('Content-Type') && fetchOptions.body && !(fetchOptions.body instanceof FormData)) {
      headers.set('Content-Type', 'application/json');
    }
    if (this.token && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${this.token}`);
    }

    const targetUrl = this.buildUrl(path, params);

    try {
      const response = await fetch(targetUrl, {
        ...fetchOptions,
        headers,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      // Check content type
      const contentType = response.headers.get('content-type') || '';
      const isJson = contentType.includes('application/json');

      let responseData: unknown = null;
      if (isJson) {
        try {
          responseData = await response.json();
        } catch {
          responseData = null;
        }
      } else {
        const text = await response.text();
        responseData = text;
      }

      // Handle HTTP 503 specifically - in system status, 503 includes a valid degraded status body!
      if (response.status === 503) {
        // If the body is a valid status response payload, we attach it to the ApiError or return it
        const error = normalizeHttpError(503, responseData);
        (error as unknown as { responseData: unknown }).responseData = responseData;
        throw error;
      }

      if (!response.ok) {
        throw normalizeHttpError(response.status, responseData);
      }

      return responseData as T;
    } catch (err: unknown) {
      clearTimeout(timeoutId);

      if (err instanceof ApiError) {
        throw err;
      }

      if (err instanceof DOMException && err.name === 'AbortError') {
        throw new ApiError(408, 'The investigation request timed out or was cancelled.', {
          isNetworkError: true,
        });
      }

      const message = err instanceof Error ? err.message : 'Network failure';
      throw new ApiError(0, `Unable to reach investigative service: ${message}`, {
        isNetworkError: true,
      });
    }
  }

  public get<T>(path: string, options?: RequestOptions): Promise<T> {
    return this.request<T>(path, { ...options, method: 'GET' });
  }

  public post<T>(path: string, body?: unknown, options?: RequestOptions): Promise<T> {
    const formattedBody = body instanceof FormData ? body : (body !== undefined ? JSON.stringify(body) : undefined);
    return this.request<T>(path, {
      ...options,
      method: 'POST',
      body: formattedBody as BodyInit | undefined,
    });
  }

  public put<T>(path: string, body?: unknown, options?: RequestOptions): Promise<T> {
    const formattedBody = body instanceof FormData ? body : (body !== undefined ? JSON.stringify(body) : undefined);
    return this.request<T>(path, {
      ...options,
      method: 'PUT',
      body: formattedBody as BodyInit | undefined,
    });
  }

  public delete<T>(path: string, options?: RequestOptions): Promise<T> {
    return this.request<T>(path, { ...options, method: 'DELETE' });
  }

  public async getBlob(path: string, options: RequestOptions = {}): Promise<Blob> {
    const { timeoutMs = 30000, params, signal: callerSignal, ...fetchOptions } = options;
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

    if (callerSignal) {
      callerSignal.addEventListener('abort', () => controller.abort());
    }

    const headers = new Headers(fetchOptions.headers || {});
    if (this.token && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${this.token}`);
    }

    const targetUrl = this.buildUrl(path, params);

    try {
      const response = await fetch(targetUrl, {
        ...fetchOptions,
        headers,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        throw new ApiError(response.status, `Download failed with HTTP ${response.status}`);
      }

      return await response.blob();
    } catch (err: unknown) {
      clearTimeout(timeoutId);
      if (err instanceof ApiError) throw err;
      throw new ApiError(0, err instanceof Error ? err.message : 'File download failed');
    }
  }
}

export const apiClient = new ApiClient(env.apiBaseUrl);
