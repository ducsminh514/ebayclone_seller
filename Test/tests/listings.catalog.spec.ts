/**
 * Listings, inventory, admin category — Phần 2 nghiệp vụ (UI).
 * Phần lớn cần đăng nhập (E2E_EMAIL / E2E_PASSWORD trong Test/.env).
 */
import { test, expect } from '@playwright/test';
import { routes } from './support/config';
import { loginAsSeller } from './support/auth';

test.describe('Listings — hub & tồn kho (đã đăng nhập)', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

  test('tiêu đề, mô tả phụ và CTA tạo / tồn kho', async ({ page }) => {
    await page.goto(routes.listings);
    await expect(page.getByRole('heading', { name: /Quản Lý Sản Phẩm/i })).toBeVisible();
    await expect(page.getByText(/Xem, sửa, kích hoạt/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /Tạo Sản Phẩm Mới/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Quản lý Tồn kho/i })).toBeVisible();
  });

  test('CTA “Tạo Sản Phẩm Mới” → /listings/create', async ({ page }) => {
    await page.goto(routes.listings);
    await page.getByRole('button', { name: /Tạo Sản Phẩm Mới/i }).click();
    await expect(page).toHaveURL(/\/listings\/create/);
    await expect(page.getByText(/Complete your listing/i)).toBeVisible();
  });

  test('CTA tồn kho → /listings/inventory', async ({ page }) => {
    await page.goto(routes.listings);
    await page.getByRole('button', { name: /Quản lý Tồn kho/i }).click();
    await expect(page).toHaveURL(/\/listings\/inventory/);
  });

  test('tabs trạng thái listing hiển thị đầy đủ', async ({ page }) => {
    await page.goto(routes.listings);
    for (const tab of ['ACTIVE', 'DRAFT', 'SCHEDULED', 'HIDDEN', 'OUT OF STOCK', 'ENDED']) {
      await expect(page.locator('a.nav-link').filter({ hasText: tab })).toBeVisible();
    }
  });
});

test.describe('Inventory — restock & điều hướng (đã đăng nhập)', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

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

  test('filter kho có các option ALL/IN_STOCK/LOW_STOCK/OUT_OF_STOCK', async ({ page }) => {
    await page.goto(routes.listingsInventory);
    const select = page.locator('select.form-select').first();
    await expect(select).toBeVisible();
    await expect(select).toContainText('Tất cả');
    await expect(select).toContainText('Còn hàng');
    await expect(select).toContainText('Sắp hết');
    await expect(select).toContainText('Hết hàng');
  });
});

test.describe('Start / Create listing — chưa đăng nhập', () => {
  test('/listings/start chuyển về login khi chưa session', async ({ page }) => {
    await page.goto(routes.listingsStart);
    await expect(page).toHaveURL(/\/login/);
  });

  test('/listings/create chuyển về login', async ({ page }) => {
    await page.goto(routes.listingsCreate);
    await expect(page).toHaveURL(/\/login/);
  });
});

test.describe('Admin — Category Manager (đã đăng nhập)', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

  test('form thêm danh mục + bảng', async ({ page }) => {
    await page.goto(routes.adminCategories);
    await expect(page.getByRole('heading', { name: /Quản Lý Danh Mục \(Admin\)/i })).toBeVisible();
    await expect(page.getByText(/Thêm Danh Mục Mới|Cập Nhật Danh Mục/)).toBeVisible();
    await expect(page.getByRole('button', { name: /Lưu Dữ Liệu/i })).toBeVisible();
  });
});

test.describe('Create Listing — deep form behavior (đã đăng nhập)', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

  test('các section chính hiển thị', async ({ page }) => {
    await page.goto(routes.listingsCreate);
    for (const section of ['Photos & Video', 'Title', 'Item Category', 'Variations', 'Item Specifics', 'Condition', 'Description', 'Pricing', 'Shipping', 'Payment']) {
      await expect(page.getByText(section, { exact: true })).toBeVisible();
    }
    await expect(page.getByRole('button', { name: /Save as Draft/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Publish Listing/i })).toBeVisible();
  });

  test('Pricing: bật Allow offers hiện Minimum offer + Auto accept', async ({ page }) => {
    await page.goto(routes.listingsCreate);
    await page.locator('#allowOffersSwitch').click();
    await expect(page.getByText(/Minimum offer/i)).toBeVisible();
    await expect(page.getByText(/Auto accept/i)).toBeVisible();
  });

  test('Pricing: bật Schedule thì có nút Schedule Listing', async ({ page }) => {
    await page.goto(routes.listingsCreate);
    await page.locator('#scheduleSwitch').click();
    await page.waitForTimeout(1000);
    await expect(page.locator('input[type="datetime-local"].form-control')).toBeVisible();
  });
});
