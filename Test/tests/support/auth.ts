import { expect, type Page } from '@playwright/test';
import { routes } from './config';

export type SellerCredentials = { email: string; password: string };

/**
 * Đọc từ `E2E_EMAIL` / `E2E_PASSWORD` (file `Test/.env` qua dotenv trong playwright.config).
 */
export function getE2ECredentials(): SellerCredentials {
  const email = process.env.E2E_EMAIL?.trim();
  const password = process.env.E2E_PASSWORD?.trim();
  if (!email || !password) {
    throw new Error(
      'Thiếu E2E_EMAIL hoặc E2E_PASSWORD. Tạo file Test/.env (xem Test/.env.example).',
    );
  }
  return { email, password };
}

/** Đăng nhập Seller Hub; sau khi xong URL không còn /login (thường là / hoặc /onboarding). */
export async function loginAsSeller(page: Page): Promise<void> {
  const { email, password } = getE2ECredentials();
  await page.goto(routes.login);
  await page.getByPlaceholder('Email or username').fill(email);
  await page.getByPlaceholder('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: /^Continue$/ }).click();
  await expect(page).not.toHaveURL(/\/login/, { timeout: 30_000 });
}
