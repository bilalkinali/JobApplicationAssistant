export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5108";

export async function apiGet<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`);
  return readResponse<T>(response);
}

export async function apiSend<T>(path: string, method: "POST" | "PUT", body: unknown): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(body)
  });
  return readResponse<T>(response);
}

export async function apiSendForm<T>(path: string, body: FormData): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: "POST",
    body
  });
  return readResponse<T>(response);
}

export async function apiDelete(path: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}${path}`, { method: "DELETE" });
  if (!response.ok) {
    throw await response.json();
  }
}

export async function readResponse<T>(response: Response): Promise<T> {
  const body = await response.json();
  if (!response.ok) {
    throw body;
  }
  return body;
}
