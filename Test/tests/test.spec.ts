/**
 * @file Smoke tests — hồi quy nhanh khi API + Blazor đã chạy.
 * Kịch bản sâu hơn nằm trong các file *.spec.ts cùng thư mục.
 */
import { test, expect } from '@playwright/test';
import { API_BASE, routes } from './support/config';
import { fetchRootCategories } from './support/api';

test.describe('@smoke Seller Hub', () => {
  test('đăng nhập: form đầy đủ', async ({ page }) => {
    await page.goto(routes.login);
    await expect(page.getByRole('heading', { name: /Sign in to your account/i })).toBeVisible();
    await expect(page.getByPlaceholder('Email or username')).toBeVisible();
    await expect(page.getByPlaceholder('Password', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: /^Continue$/ })).toBeVisible();
  });

  test('đăng ký + liên kết đăng nhập', async ({ page }) => {
    await page.goto(routes.register);
    await expect(page.getByRole('heading', { name: /Create an account/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /Sign in/i })).toBeVisible();
  });

  test('trang bảo vệ: / và /listings/create → /login', async ({ page }) => {
    await page.goto(routes.home);
    await expect(page).toHaveURL(/\/login/);
    await page.goto(routes.listingsCreate);
    await expect(page).toHaveURL(/\/login/);
  });

  test('Swagger UI hoạt động', async ({ request }) => {
    const res = await request.get(`${API_BASE}/swagger/index.html`);
    expect(res.ok(), await res.text()).toBeTruthy();
  });

  test('API categories (root) trả về dữ liệu seed', async ({ request }) => {
    const roots = await fetchRootCategories(request);
    expect(roots.length).toBeGreaterThanOrEqual(6);
    expect(roots.some((c) => c.slug === 'electronics')).toBeTruthy();
  });
});
