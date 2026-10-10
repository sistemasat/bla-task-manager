import { randomUUID } from "node:crypto";
import { expect, test, type Page } from "@playwright/test";

const password = "BrowserTestPassword123!";

async function register(page: Page, email: string) {
  await page
    .getByRole("button", { name: "Create account", exact: true })
    .click();
  await page.getByLabel("Name", { exact: true }).fill("Browser Tester");
  await page.getByLabel("Email", { exact: true }).fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page
    .getByRole("button", { name: "Create account", exact: true })
    .click();
  await expect(
    page.getByRole("heading", { name: "My tasks", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "A little room to begin", exact: true }),
  ).toBeVisible();
}

async function signIn(page: Page, email: string) {
  await page.getByLabel("Email", { exact: true }).fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "My tasks", exact: true }),
  ).toBeVisible();
}

test("registers and completes task CRUD with a persisted date", async ({
  page,
}) => {
  await page.goto("/");
  await register(page, `crud-${randomUUID()}@example.com`);
  await page.getByRole("button", { name: "New task", exact: true }).click();
  await page
    .getByLabel("Title", { exact: true })
    .fill("Prepare browser walkthrough");
  await page
    .getByRole("textbox", { name: "Description", exact: true })
    .fill("Check the complete running application.");
  await page.getByLabel("Due date", { exact: true }).fill("2026-10-10");
  await page.getByRole("button", { name: "Create task", exact: true }).click();
  await expect(
    page.getByRole("button", {
      name: "Prepare browser walkthrough",
      exact: true,
    }),
  ).toBeVisible();
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBe(true);

  await page
    .getByRole("button", {
      name: "Edit Prepare browser walkthrough",
      exact: true,
    })
    .click();
  await expect(page.getByLabel("Due date", { exact: true })).toHaveValue(
    "2026-10-10",
  );
  await expect(
    page.getByRole("textbox", { name: "Description", exact: true }),
  ).toHaveValue("Check the complete running application.");
  await page
    .getByLabel("Title", { exact: true })
    .fill("Browser walkthrough ready");
  await page
    .getByRole("combobox", { name: "Status", exact: true })
    .selectOption("completed");
  await page.getByRole("button", { name: "Save changes", exact: true }).click();
  await expect(
    page.getByRole("button", {
      name: "Browser walkthrough ready",
      exact: true,
    }),
  ).toBeVisible();
  await expect(page.getByText("Completed", { exact: true })).toBeVisible();

  await page
    .getByRole("button", {
      name: "Delete Browser walkthrough ready",
      exact: true,
    })
    .click();
  const confirmation = page.getByRole("dialog", {
    name: "Delete this task?",
    exact: true,
  });
  await expect(confirmation).toBeVisible();
  await confirmation
    .getByRole("button", { name: "Keep task", exact: true })
    .click();
  await expect(
    page.getByRole("button", {
      name: "Browser walkthrough ready",
      exact: true,
    }),
  ).toBeVisible();
  await page
    .getByRole("button", {
      name: "Delete Browser walkthrough ready",
      exact: true,
    })
    .click();
  await confirmation
    .getByRole("button", { name: "Delete task", exact: true })
    .click();
  await expect(
    page.getByRole("heading", { name: "A little room to begin", exact: true }),
  ).toBeVisible();
  await expect(page.getByRole("dialog")).toHaveCount(0);
});

test("requires login after reload and keeps each user's list private", async ({
  page,
}) => {
  const ownerEmail = `owner-${randomUUID()}@example.com`;
  await page.goto("/");
  await register(page, ownerEmail);
  await page.getByRole("button", { name: "New task", exact: true }).click();
  await page.getByLabel("Title", { exact: true }).fill("Private owner task");
  await page.getByRole("button", { name: "Create task", exact: true }).click();
  await expect(
    page.getByRole("button", { name: "Private owner task", exact: true }),
  ).toBeVisible();

  await page.reload();
  await expect(
    page.getByRole("heading", { name: "Welcome back", exact: true }),
  ).toBeVisible();
  await signIn(page, ownerEmail);
  await expect(
    page.getByRole("button", { name: "Private owner task", exact: true }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
  await register(page, `stranger-${randomUUID()}@example.com`);
  await expect(
    page.getByRole("button", { name: "Private owner task", exact: true }),
  ).toHaveCount(0);
});
