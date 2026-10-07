import type {
  Credentials,
  Session,
  TaskInput,
  TaskItem,
  TaskStatus,
} from "./types";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
  }
}

export const api = {
  login: (input: Credentials) =>
    request<Session>("/auth/login", {
      method: "POST",
      body: JSON.stringify(input),
    }),
  register: (input: Credentials) =>
    request<void>("/auth/register", {
      method: "POST",
      body: JSON.stringify(input),
    }),
  listTasks: (token: string, skip: number, signal?: AbortSignal) =>
    request<TaskItem[]>(`/tasks?skip=${skip}&take=51`, { signal }, token),
  createTask: (token: string, input: TaskInput) =>
    request<TaskItem>(
      "/tasks",
      { method: "POST", body: JSON.stringify(input) },
      token,
    ),
  updateTask: (
    token: string,
    id: string,
    input: TaskInput,
    status: TaskStatus,
  ) =>
    request<TaskItem>(
      `/tasks/${id}`,
      { method: "PUT", body: JSON.stringify({ ...input, status }) },
      token,
    ),
  deleteTask: (token: string, id: string) =>
    request<void>(`/tasks/${id}`, { method: "DELETE" }, token),
};

async function request<T>(
  path: string,
  options: RequestInit = {},
  token?: string,
): Promise<T> {
  const headers: Record<string, string> = {};
  if (options.body) headers["Content-Type"] = "application/json";
  if (token) headers.Authorization = `Bearer ${token}`;
  let response: Response;
  try {
    response = await fetch(
      `${import.meta.env.VITE_API_BASE_URL ?? ""}/api${path}`,
      { ...options, headers },
    );
  } catch (failure) {
    if (failure instanceof Error && failure.name === "AbortError")
      throw failure;
    throw new ApiError("We couldn't connect. Please try again.", 0);
  }
  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as {
      title?: string;
      errors?: Record<string, string[]>;
    } | null;
    const details = Object.values(problem?.errors ?? {})
      .flat()
      .join(" ");
    throw new ApiError(
      details ||
        problem?.title ||
        (response.status === 401
          ? "Your session has ended. Please sign in again."
          : "Unable to complete the request."),
      response.status,
    );
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}
