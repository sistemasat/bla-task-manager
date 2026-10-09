import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { App } from "./App";
import { api, ApiError } from "./api";
import type { Session, TaskItem } from "./types";

const task: TaskItem = {
  id: "task-id",
  title: "Prepare the interview",
  description: "Review the architecture",
  status: "pending",
  due_date: "2026-10-09",
  created_at: "2026-10-07T12:00:00Z",
  updated_at: "2026-10-07T12:00:00Z",
};

beforeEach(() => {
  const session: Session = {
    access_token: {
      token: "test-token",
      expires_at: new Date(Date.now() + 30 * 60 * 1000).toISOString(),
    },
    user: { id: "user-id", name: "Alex", email: "demo@example.com" },
  };
  vi.spyOn(api, "register").mockResolvedValue(undefined);
  vi.spyOn(api, "login").mockResolvedValue(session);
  vi.spyOn(api, "listTasks").mockResolvedValue([]);
  vi.spyOn(api, "createTask").mockResolvedValue(task);
  vi.spyOn(api, "updateTask").mockResolvedValue(task);
  vi.spyOn(api, "deleteTask").mockResolvedValue(undefined);
});

async function signIn() {
  const user = userEvent.setup();
  render(<App />);
  await user.click(screen.getByRole("button", { name: "Use demo account" }));
  await user.click(screen.getByRole("button", { name: "Sign in" }));
  return user;
}

describe("Application flows", () => {
  it("registers before signing in and loading the private list", async () => {
    const user = userEvent.setup();
    render(<App />);
    await user.click(screen.getByRole("button", { name: "Create account" }));
    await user.type(screen.getByLabelText("Name"), "Alex");
    await user.type(screen.getByLabelText("Email"), "alex@example.com");
    await user.type(screen.getByLabelText("Password"), "StrongPassword!");
    await user.click(screen.getByRole("button", { name: "Create account" }));
    await screen.findByRole("heading", { name: "My tasks" });
    const credentials = {
      name: "Alex",
      email: "alex@example.com",
      password: "StrongPassword!",
    };
    expect(api.register).toHaveBeenCalledWith(credentials);
    expect(api.login).toHaveBeenCalledWith(credentials);
    expect(vi.mocked(api.register).mock.invocationCallOrder[0]).toBeLessThan(
      vi.mocked(api.login).mock.invocationCallOrder[0],
    );
    expect(api.listTasks).toHaveBeenCalledWith(
      "test-token",
      0,
      expect.any(AbortSignal),
    );
  });

  it("creates edits and deletes a task through the connected screens", async () => {
    const updated = {
      ...task,
      title: "Interview ready",
      status: "completed" as const,
    };
    vi.mocked(api.listTasks)
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([task])
      .mockResolvedValueOnce([updated])
      .mockResolvedValueOnce([]);
    vi.mocked(api.updateTask).mockResolvedValue(updated);
    const user = await signIn();
    await screen.findByRole("heading", { name: "A little room to begin" });
    await user.click(screen.getByRole("button", { name: "New task" }));
    await user.type(screen.getByLabelText("Title"), task.title);
    await user.type(screen.getByLabelText("Description"), task.description!);
    await user.type(screen.getByLabelText("Due date"), task.due_date!);
    await user.click(screen.getByRole("button", { name: "Create task" }));
    await screen.findByRole("button", { name: task.title });
    expect(api.createTask).toHaveBeenCalledWith("test-token", {
      title: task.title,
      description: task.description,
      due_date: task.due_date,
    });

    await user.click(
      screen.getByRole("button", { name: `Edit ${task.title}` }),
    );
    await user.clear(screen.getByLabelText("Title"));
    await user.type(screen.getByLabelText("Title"), updated.title);
    await user.selectOptions(screen.getByLabelText("Status"), "completed");
    await user.click(screen.getByRole("button", { name: "Save changes" }));
    await screen.findByRole("button", { name: updated.title });
    expect(api.updateTask).toHaveBeenCalledWith(
      "test-token",
      task.id,
      {
        title: updated.title,
        description: task.description,
        due_date: task.due_date,
      },
      "completed",
    );

    await user.click(
      screen.getByRole("button", { name: `Delete ${updated.title}` }),
    );
    const confirmation = screen.getByRole("dialog", {
      name: "Delete this task?",
    });
    expect(api.deleteTask).not.toHaveBeenCalled();
    await user.click(
      within(confirmation).getByRole("button", { name: "Delete task" }),
    );
    await screen.findByRole("heading", { name: "A little room to begin" });
    expect(api.deleteTask).toHaveBeenCalledWith("test-token", task.id);
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("returns to sign-in when a protected request receives 401", async () => {
    vi.mocked(api.listTasks).mockRejectedValue(
      new ApiError("Unauthorized", 401),
    );
    await signIn();
    await screen.findByRole("heading", { name: "Welcome back" });
    expect(screen.getByRole("status")).toHaveTextContent("session has ended");
    expect(
      screen.queryByRole("heading", { name: "My tasks" }),
    ).not.toBeInTheDocument();
  });

  it("retries a failed list request without losing the session", async () => {
    vi.mocked(api.listTasks)
      .mockRejectedValueOnce(new ApiError("Connection unavailable", 0))
      .mockResolvedValueOnce([task]);
    const user = await signIn();
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Connection unavailable",
    );
    await user.click(screen.getByRole("button", { name: "Try again" }));
    await screen.findByRole("button", { name: task.title });
    expect(api.listTasks).toHaveBeenCalledTimes(2);
    expect(api.login).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("preserves task input after server validation fails and allows correction", async () => {
    vi.mocked(api.createTask)
      .mockRejectedValueOnce(new ApiError("Title is invalid.", 400))
      .mockResolvedValueOnce(task);
    const user = await signIn();
    await screen.findByRole("heading", { name: "A little room to begin" });
    await user.click(screen.getByRole("button", { name: "New task" }));
    await user.type(screen.getByLabelText("Title"), "Original title");
    await user.click(screen.getByRole("button", { name: "Create task" }));
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Title is invalid.",
    );
    expect(screen.getByLabelText("Title")).toHaveValue("Original title");
    expect(api.listTasks).toHaveBeenCalledTimes(1);
    await user.clear(screen.getByLabelText("Title"));
    await user.type(screen.getByLabelText("Title"), task.title);
    await user.click(screen.getByRole("button", { name: "Create task" }));
    await waitFor(() =>
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
    );
    expect(api.createTask).toHaveBeenLastCalledWith("test-token", {
      title: task.title,
      description: null,
      due_date: null,
    });
    expect(api.listTasks).toHaveBeenCalledTimes(2);
  });
});
