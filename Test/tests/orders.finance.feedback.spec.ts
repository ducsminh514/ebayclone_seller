/**
 * Đơn hàng, ví, feedback — Phần 3–4–6 (UI). Cần đăng nhập E2E.
 */
import { test, expect } from '@playwright/test';
import { routes } from './support/config';
import { loginAsSeller } from './support/auth';

test.describe('Orders — Seller Hub fulfillment', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

  test('header, Test Buy, và tabs trạng thái máy trạng thái', async ({ page }) => {
    await page.goto(routes.orders);
    await expect(page.getByRole('heading', { name: /Manage orders/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Test Buy/i })).toBeVisible();

    const tabs = [
      'All',
      'Awaiting payment',
      'Awaiting shipment',
      'Shipped',
      'Delivered',
      'Cancelled',
      'Returns',
      'Disputes',
      'Refunded',
    ] as const;
    for (const label of tabs) {
      await expect(page.getByRole('button', { name: label })).toBeVisible();
    }
  });

  test('khu vực tìm kiếm đơn', async ({ page }) => {
    await page.goto(routes.orders);
    await expect(page.getByText(/Search orders/i)).toBeVisible();
    await expect(page.getByPlaceholder(/Search orders/i)).toBeVisible();
  });

  test("chuyển tab → active state trên 'Awaiting shipment'", async ({ page }) => {
    await page.goto(routes.orders);
    const tab = page.getByRole('button', { name: /Awaiting shipment/i });
    await tab.click();
    await expect(tab).toHaveClass(/active/);
  });

  test('nút Test Buy mở modal và đóng được', async ({ page }) => {
    await page.goto(routes.orders);
    await page.getByRole('button', { name: /Test Buy/i }).click();
    await expect(page.getByText(/Test Buy/i)).toBeVisible();
    await expect(page.getByText(/Giả lập mua hàng/i)).toBeVisible();
    await page.locator('.modal-content .btn-close').click();
    await expect(page.getByText(/Giả lập mua hàng/i)).toHaveCount(0);
  });

  test('clear all không làm crash trang', async ({ page }) => {
    await page.goto(routes.orders);
    await page.getByPlaceholder(/Search orders/i).fill('abc');
    await page.getByRole('link', { name: /Clear all/i }).click();
    await expect(page.getByRole('heading', { name: /Manage orders/i })).toBeVisible();
  });
});

test.describe('Wallet — Seller finance dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

  test('tiêu đề ví', async ({ page }) => {
    await page.goto(routes.wallet);
    await expect(page.getByRole('heading', { name: /Ví của tôi/i })).toBeVisible();
  });

  test('nhãn số dư (Khả dụng / Escrow / …) hoặc skeleton khi đang tải', async ({ page }) => {
    await page.goto(routes.wallet);
    await expect
      .poll(
        async () =>
          (await page.getByText('Khả dụng', { exact: true }).count()) > 0 ||
          (await page.getByText(/Đang giữ \(Escrow\)/).count()) > 0 ||
          (await page.locator('.skeleton-card').count()) > 0,
        { timeout: 15_000 },
      )
      .toBeTruthy();
  });
});

test.describe('Feedback — eBay-style manager', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

  test('tiêu đề và bộ lọc POSITIVE/NEUTRAL/NEGATIVE', async ({ page }) => {
    await page.goto(routes.feedback);
    await expect(page.getByRole('heading', { name: /Feedback Manager/i })).toBeVisible();

    const tablist = page.locator('ul.nav-tabs');
    for (const label of ['ALL', 'POSITIVE', 'NEUTRAL', 'NEGATIVE']) {
      await expect(tablist.getByRole('link', { name: new RegExp(`^${label}`) })).toBeVisible();
    }
  });

  test('trạng thái rỗng thân thiện (khi chưa có feedback)', async ({ page }) => {
    await page.goto(routes.feedback);
    await expect(page.getByText(/Chưa có feedback nào|Đang tải/)).toBeVisible({ timeout: 15_000 });
  });

  test('chuyển tab POSITIVE và quay về ALL', async ({ page }) => {
    await page.goto(routes.feedback);
    const positive = page.locator('ul.nav-tabs').getByRole('link', { name: /^POSITIVE/ });
    await positive.click();
    await expect(positive).toHaveClass(/active/);
    const all = page.locator('ul.nav-tabs').getByRole('link', { name: /^ALL/ });
    await all.click();
    await expect(all).toHaveClass(/active/);
  });
});
