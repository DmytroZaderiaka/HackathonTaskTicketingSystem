export interface ApiError {
  status: number;
  title: string;
  detail?: string;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`/api${path}`, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
    ...options,
  });

  if (!response.ok) {
    let title = response.statusText;
    let detail: string | undefined;
    try {
      // Errors are RFC 7807 ProblemDetails.
      const problem = (await response.json()) as { title?: string; detail?: string };
      title = problem.title ?? title;
      detail = problem.detail;
    } catch {
      // Non-JSON error body; keep the status text.
    }
    const error: ApiError = { status: response.status, title, detail };
    throw error;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body: body === undefined ? undefined : JSON.stringify(body) }),
  del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
};
