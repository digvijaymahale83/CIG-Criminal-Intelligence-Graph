/**
 * Centralized Normalized API Errors
 * Never leaks raw infrastructure stack traces or database errors.
 */

export class ApiError extends Error {
  public statusCode: number;
  public userMessage: string;
  public correlationId?: string;
  public isNetworkError: boolean;
  public isDegradedService: boolean;

  constructor(
    statusCode: number,
    userMessage: string,
    options?: { correlationId?: string; isNetworkError?: boolean; isDegradedService?: boolean }
  ) {
    super(userMessage);
    this.name = 'ApiError';
    this.statusCode = statusCode;
    this.userMessage = userMessage;
    this.correlationId = options?.correlationId;
    this.isNetworkError = options?.isNetworkError ?? false;
    this.isDegradedService = options?.isDegradedService ?? (statusCode === 503);
  }
}

export function normalizeHttpError(status: number, responseBody?: unknown): ApiError {
  let userMessage: string;

  switch (status) {
    case 400:
      userMessage = 'Bad Request. Please verify the submitted parameters.';
      break;
    case 401:
      userMessage = 'Session expired. Please re-authenticate to continue.';
      break;
    case 403:
      userMessage = 'You do not have authorization to perform this investigative action.';
      break;
    case 404:
      userMessage = 'The requested investigation resource was not found.';
      break;
    case 409:
      userMessage = 'The resource was updated concurrently. Please refresh before continuing.';
      break;
    case 422:
      userMessage = 'Validation error. Submitted data does not satisfy schema requirements.';
      break;
    case 429:
      userMessage = 'Rate limit exceeded. Too many requests; try again shortly.';
      break;
    case 500:
      userMessage = 'The intelligence backend encountered an unexpected internal error.';
      break;
    case 502:
      userMessage = 'Bad gateway. Upstream investigation service is unreachable.';
      break;
    case 503:
      userMessage = 'Service temporarily degraded or unavailable.';
      break;
    case 504:
      userMessage = 'Gateway timeout. Analytical query exceeded execution time limit.';
      break;
    default:
      userMessage = `Unexpected response code ${status} from investigation API.`;
  }

  // Sanitize any potential object details
  let correlationId: string | undefined;
  if (responseBody && typeof responseBody === 'object' && 'correlationId' in responseBody) {
    correlationId = String((responseBody as { correlationId: unknown }).correlationId);
  }

  return new ApiError(status, userMessage, {
    correlationId,
    isDegradedService: status === 503,
  });
}
