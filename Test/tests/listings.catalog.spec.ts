/**
 * Listings, inventory, admin category — Phần 2 nghiệp vụ (UI).
 */
import { test, expect } from '@playwright/test';
import { routes } from './support/config';

test.describe('Listings — hub sản phẩm', () => {
  test('tiêu đề, mô tả phụ và CTA tạo / tồn kho', async ({ page }) => {
    await page.goto(routes.listings);
    await expect(page.getByRole('heading', { name: /Quản Lý Sản Phẩm/i })).toBeVisible();
    await expect(page.getByText(/Xem, sửa, kích hoạt/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /Tạo Sản Phẩm Mới/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Quản lý Tồn kho/i })).toBeVisible();
  });

  test('CTA “Tạo Sản Phẩm Mới” dẫn tới trang tạo (yêu cầu đăng nhập)', async ({ page }) => {
    await page.goto(routes.listings);
    await page.getByRole('button', { name: /Tạo Sản Phẩm Mới/i }).click();
    await expect(page).toHaveURL(/\/listings\/create|\/login/);
  });

  test('CTA tồn kho → /listings/inventory', async ({ page }) => {
    await page.goto(routes.listings);
    await page.getByRole('button', { name: /Quản lý Tồn kho/i }).click();
    await expect(page).toHaveURL(/\/listings\/inventory/);
  });
});

test.describe('Inventory — restock & điều hướng', () => {
  test('copy nghiệp vụ Restock và nút quay lại', async ({ page }) => {
    await page.goto(routes.listingsInventory);
    await expect(page.getByRole('heading', { name: /Quản Lý Tồn Kho/i })).toBeVisible();
    await expect(page.getByText(/nhập thêm hàng \(Restock\)/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /Quay lại Danh sách/i })).toBeVisible();
  });

  test('nút quay lại → /listings', async ({ page }) => {
    await page.goto(routes.listingsInventory);
    await page.getByRole('button', { name: /Quay lại Danh sách/i }).click();
    await expect(page).toHaveURL(/\/listings/);
  });
});

test.describe('Start / Create listing — bảo vệ route', () => {
  test('/listings/start chuyển về login khi chưa session', async ({ page }) => {
    await page.goto(routes.listingsStart);
    await expect(page).toHaveURL(/\/login/);
  });

  test('/listings/create chuyển về login', async ({ page }) => {
    await page.goto(routes.listingsCreate);
    await expect(page).toHaveURL(/\/login/);
  });
});

test.describe('Admin — Category Manager', () => {
  test('form thêm danh mục + bảng', async ({ page }) => {
    await page.goto(routes.adminCategories);
    await expect(page.getByRole('heading', { name: /Quản Lý Danh Mục \(Admin\)/i })).toBeVisible();
    await expect(page.getByText(/Thêm Danh Mục Mới|Cập Nhật Danh Mục/)).toBeVisible();
    await expect(page.getByRole('button', { name: /Lưu Dữ Liệu/i })).toBeVisible();
  });
});
