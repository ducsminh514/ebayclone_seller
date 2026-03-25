/**
 * Business policies & Marketing vouchers — Phần 1 + Phần 5 (UI). Cần đăng nhập E2E.
 */
import { test, expect } from '@playwright/test';
import { routes } from './support/config';
import { loginAsSeller } from './support/auth';

test.describe('Business policies — trung tâm', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

  test('/bp/manage: tiêu đề và menu tạo policy', async ({ page }) => {
    await page.goto(routes.bpManage);
    await expect(page.getByText(/^Business policies$/i).first()).toBeVisible();
    await expect(page.getByText(/shipping, return, and payment/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /Create policy/i })).toBeVisible();
  });
});

test.describe('Business policies — form tạo mới', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

  test('Payment policy: tiêu đề + Policy name', async ({ page }) => {
    await page.goto(routes.bpPaymentCreate);
    await expect(page.getByText('Create payment policy')).toBeVisible();
    await expect(page.getByText(/^Policy name$/).first()).toBeVisible();
  });

  test('Shipping policy: tiêu đề + Policy name', async ({ page }) => {
    await page.goto(routes.bpShippingCreate);
    await expect(page.getByText('Create shipping policy')).toBeVisible();
    await expect(page.getByText(/^Policy name$/).first()).toBeVisible();
  });

  test('Return policy: tiêu đề + Policy name', async ({ page }) => {
    await page.goto(routes.bpReturnCreate);
    await expect(page.getByText('Create return policy')).toBeVisible();
    await expect(page.getByText(/^Policy name$/).first()).toBeVisible();
  });
});

test.describe('Marketing — Coded coupon UI', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

  test('danh sách voucher: tabs trạng thái campaign', async ({ page }) => {
    await page.goto(routes.vouchers);
    await expect(page.getByRole('heading', { name: /Quản lý Voucher/i })).toBeVisible();
    for (const tab of ['ALL', 'DRAFT', 'ACTIVE', 'PAUSED', 'ENDED']) {
      await expect(page.getByRole('button', { name: tab })).toBeVisible();
    }
    await expect(page.getByRole('link', { name: /Tạo Voucher mới/i })).toBeVisible();
  });

  test('trang tạo: 3 khối nghiệp vụ + giới hạn mã 15 ký tự', async ({ page }) => {
    await page.goto(routes.vouchersCreate);
    await expect(page.getByRole('heading', { name: /Tạo Voucher mới/i })).toBeVisible();
    await expect(page.getByText('1. Thông tin cơ bản')).toBeVisible();
    await expect(page.getByText('2. Loại giảm giá')).toBeVisible();
    await expect(page.getByText('3. Điều kiện áp dụng')).toBeVisible();
    await expect(page.getByText(/Tối đa 15 ký tự/)).toBeVisible();
    await expect(page.getByLabel(/Giảm theo %/)).toBeVisible();
    await expect(page.getByLabel(/Giảm cố định/)).toBeVisible();
  });

  test('link quay lại danh sách voucher', async ({ page }) => {
    await page.goto(routes.vouchersCreate);
    await page.getByRole('link', { name: /Quay lại/i }).click();
    await expect(page).toHaveURL(/\/marketing\/vouchers$/);
  });
});
