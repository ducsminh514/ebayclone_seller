/**
 * Luồng xác thực & onboarding (Phần 1 nghiệp vụ) — kiểm tra UI có sẵn.
 */
import { test, expect } from '@playwright/test';
import { routes } from './support/config';
import { loginAsSeller } from './support/auth';

test.describe('Đăng nhập — trải nghiệm chi tiết', () => {
  test('điều hướng sang đăng ký', async ({ page }) => {
    await page.goto(routes.login);
    await page.getByRole('link', { name: /Create account/i }).click();
    await expect(page).toHaveURL(/\/register/);
  });

  test('ô “Stay signed in” và nút mạng xã hội (placeholder)', async ({ page }) => {
    await page.goto(routes.login);
    await expect(page.getByLabel(/Stay signed in/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /Continue with Google/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Continue with Facebook/i })).toBeVisible();
  });
});

test.describe('Đăng ký — trải nghiệm chi tiết', () => {
  test('liên kết quay lại đăng nhập', async ({ page }) => {
    await page.goto(routes.register);
    await page.getByRole('link', { name: /Sign in/i }).click();
    await expect(page).toHaveURL(/\/login/);
  });
});

test.describe('Xác minh email — trang tĩnh', () => {
  test('hiển thị form mã OTP khi có query email', async ({ page }) => {
    await page.goto(`${routes.verifyEmail}?email=test%40example.com`);
    await expect(page.getByRole('heading', { name: /Verify your email/i })).toBeVisible();
    await expect(page.getByPlaceholder(/Enter code/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /Verify address/i })).toBeVisible();
  });
});

