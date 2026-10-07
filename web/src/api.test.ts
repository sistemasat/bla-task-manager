import { afterEach, describe, expect, it, vi } from "vitest";
import { api, ApiError } from "./api";

afterEach(() => vi.unstubAllGlobals());

describe("API client", () => {
  it("sends bearer token and date-only body when creating a task", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValue(
        new Response(JSON.stringify({ id: "task-id" }), { status: 201 }),
      );
    vi.stubGlobal("fetch", fetch);
    await api.createTask("access-token", {
      title: "Task",
      description: null,
      due_date: "2026-10-09",
    });
    const [, options] = fetch.mock.calls[0];
    expect(options.headers.Authorization).toBe("Bearer access-token");
    expect(JSON.parse(options.body).due_date).toBe("2026-10-09");
    expect(options.method).toBe("POST");
  });
  it("surfaces validation details and the HTTP status", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          new Response(
            JSON.stringify({
              title: "Invalid input",
              errors: { title: ["Enter a title."] },
            }),
            { status: 400 },
          ),
        ),
    );
    await expect(
      api.createTask("token", { title: "", description: null, due_date: null }),
    ).rejects.toMatchObject({ message: "Enter a title.", status: 400 });
  });
  it("accepts an empty successful delete response", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response(null, { status: 204 })),
    );
    await expect(api.deleteTask("token", "task-id")).resolves.toBeUndefined();
  });
  it("does not attach authorization to a login request", async () => {
    const fetch = vi.fn().mockResolvedValue(new Response("{}"));
    vi.stubGlobal("fetch", fetch);
    await api.login({
      name: "",
      email: "alex@example.com",
      password: "StrongPassword!",
    });
    expect(fetch.mock.calls[0][1].headers.Authorization).toBeUndefined();
  });
  it("reports connection failures without exposing low-level errors", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockRejectedValue(new TypeError("Failed to fetch")),
    );
    await expect(api.listTasks("token", 0)).rejects.toBeInstanceOf(ApiError);
    await expect(api.listTasks("token", 0)).rejects.toMatchObject({
      status: 0,
    });
  });
});
