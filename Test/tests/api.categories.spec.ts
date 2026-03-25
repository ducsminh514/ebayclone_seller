/**
 * Hợp đồng API danh mục & Item Specifics (Phần 2 nghiệp vụ — CategorySeeder).
 */
import { test, expect } from '@playwright/test';
import { API_BASE } from './support/config';
import {
  fetchAllCategories,
  fetchCategoriesByParent,
  fetchItemSpecifics,
  fetchRootCategories,
  type CategoryRow,
} from './support/api';

function bySlug(rows: CategoryRow[], slug: string): CategoryRow | undefined {
  return rows.find((c) => c.slug === slug);
}

test.describe('API: cây danh mục', () => {
  test('root: đủ 6 nhánh seed và parentId null', async ({ request }) => {
    const roots = await fetchRootCategories(request);
    expect(roots.length).toBeGreaterThanOrEqual(6);
    for (const slug of ['electronics', 'clothing-accessories', 'home-garden', 'sporting-goods']) {
      const row = bySlug(roots, slug);
      expect(row, `missing root ${slug}`).toBeTruthy();
      expect(row!.parentId ?? null).toBeNull();
    }
  });

  test('Electronics có con và HasChildren = true trên root', async ({ request }) => {
    const roots = await fetchRootCategories(request);
    const electronics = bySlug(roots, 'electronics');
    expect(electronics?.id).toBeTruthy();
    expect(electronics!.hasChildren).toBe(true);

    const subs = await fetchCategoriesByParent(request, electronics!.id);
    expect(subs.length).toBeGreaterThanOrEqual(4);
    const slugs = subs.map((s) => s.slug);
    expect(slugs).toContain('phones');
    expect(slugs).toContain('laptops');
  });

  test('bản đồ đầy đủ: mỗi slug unique', async ({ request }) => {
    const all = await fetchAllCategories(request);
    const slugs = all.map((c) => c.slug).filter(Boolean) as string[];
    const set = new Set(slugs);
    expect(set.size).toBe(slugs.length);
  });

  test('điều hướng 2 cấp: con có parentId trỏ về Electronics', async ({ request }) => {
    const roots = await fetchRootCategories(request);
    const electronics = bySlug(roots, 'electronics');
    const phones = (await fetchCategoriesByParent(request, electronics!.id)).find((c) => c.slug === 'phones');
    expect(phones?.parentId).toBe(electronics!.id);
  });
});

test.describe('API: Item specifics theo category', () => {
  test('Electronics: có ít nhất 1 REQUIRED và metadata đầy đủ', async ({ request }) => {
    const roots = await fetchRootCategories(request);
    const electronics = bySlug(roots, 'electronics');
    const specifics = await fetchItemSpecifics(request, electronics!.id);
    expect(specifics.length).toBeGreaterThan(0);

    const requirements = specifics.map((s) => (s.requirement ?? '').toUpperCase());
    expect(requirements.some((r) => r.includes('REQUIRED'))).toBeTruthy();

    for (const s of specifics) {
      expect(s.name?.length).toBeGreaterThan(0);
      expect(s.requirement?.length).toBeGreaterThan(0);
    }
  });

  test('Cell phones: item specifics khớp domain điện thoại', async ({ request }) => {
    const roots = await fetchRootCategories(request);
    const electronics = bySlug(roots, 'electronics');
    const phones = (await fetchCategoriesByParent(request, electronics!.id)).find((c) => c.slug === 'phones');
    const specifics = await fetchItemSpecifics(request, phones!.id);
    expect(specifics.length).toBeGreaterThan(0);
    const names = specifics.map((s) => (s.name ?? '').toLowerCase());
    expect(names.some((n) => n.includes('brand'))).toBeTruthy();
  });
});

test.describe('API: lỗi & biên', () => {
  test('item-specifics category “trống”: không 5xx', async ({ request }) => {
    const fake = '00000000-0000-0000-0000-000000000000';
    const res = await request.get(`${API_BASE}/api/categories/${fake}/item-specifics`);
    expect(res.status()).toBeLessThan(500);
    if (res.ok()) {
      expect(Array.isArray(await res.json())).toBeTruthy();
    }
  });
});
