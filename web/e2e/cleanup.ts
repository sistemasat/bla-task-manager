import { rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { basename, dirname, resolve } from "node:path";
import type { FullConfig } from "@playwright/test";

export default async function cleanup(config: FullConfig) {
  const databasePath = resolve(config.metadata.databasePath as string);
  if (
    dirname(databasePath) !== resolve(tmpdir()) ||
    !/^task-manager-e2e-[\da-f-]+\.db$/.test(basename(databasePath))
  ) {
    throw new Error(
      "Refusing to remove a database outside the E2E temporary path.",
    );
  }
  for (const suffix of ["", "-shm", "-wal"]) {
    await rm(databasePath + suffix, { force: true });
  }
}
