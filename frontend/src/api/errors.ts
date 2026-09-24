import { isAxiosError } from 'axios'

const UNEXPECTED_ERROR = 'Ocorreu um erro inesperado. Tente novamente.'

// Messages for statuses whose meaning is fixed by the endpoint's contract
// (e.g. 401 on login always means invalid credentials). They take precedence
// over the backend text, which is in English.
type StatusMessages = Partial<Record<number, string>>

// Turns an API failure into a message that is safe to show to the user.
// Understands the backend's { message } bodies and ASP.NET ProblemDetails.
// Anything else (HTML error pages, stack traces) is never displayed.
export function getErrorMessage(
  error: unknown,
  statusMessages: StatusMessages = {},
): string {
  if (!isAxiosError(error)) {
    return UNEXPECTED_ERROR
  }

  if (error.code === 'ECONNABORTED' || error.code === 'ETIMEDOUT') {
    return 'O servidor demorou demais para responder. Tente novamente.'
  }

  if (!error.response) {
    return 'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.'
  }

  const statusMessage = statusMessages[error.response.status]

  if (statusMessage) {
    return statusMessage
  }

  // The Vite proxy answers 502 when the backend is down.
  if ([502, 503, 504].includes(error.response.status)) {
    return 'Servidor indisponível no momento. Tente novamente em instantes.'
  }

  // 5xx bodies are generic ProblemDetails (English title, no useful detail);
  // the controllers only send { message } with 4xx.
  if (error.response.status >= 500) {
    return 'Erro inesperado no servidor. Tente novamente mais tarde.'
  }

  return readBackendMessage(error.response.data) ?? UNEXPECTED_ERROR
}

function readBackendMessage(data: unknown): string | null {
  if (typeof data !== 'object' || data === null) {
    return null
  }

  // { message } returned by the controllers
  if ('message' in data && typeof data.message === 'string') {
    return data.message
  }

  // ProblemDetails: { title }. Its "errors" map is deliberately not shown:
  // this API only produces it for malformed payloads (model binding), and
  // those entries are serializer internals (JSON paths, byte positions).
  // Field-level rules come back as { message } from the services instead.
  if ('title' in data && typeof data.title === 'string') {
    return data.title
  }

  return null
}
