/**
 * Hợp đồng API danh mục & Item Specifics — không gắn cứng slug seed (DB có thể khác bản CategorySeeder trong repo).
 */
import { test, expect } from '@playwright/test';
import { API_BASE } from './support/config';
import {
  fetchAllCategories,
  fetchCategoriesByParent,
  fetchItemSpecifics,
  fetchRootCategories,
  type CategoryRow,
  type ItemSpecificRow,
} from './support/api';

function bySlug(rows: CategoryRow[], slug: string): CategoryRow | undefined {
  return rows.find((c) => c.slug === slug);
}

test.describe('API: cây danh mục', () => {
  test('root: đủ danh mục gốc, mọi dòng có parentId rỗng', async ({ request }) => {
    const roots = await fetchRootCategories(request);
    expect(roots.length).toBeGreaterThanOrEqual(6);
    for (const row of roots) {
      expect(row.id?.length).toBeGreaterThan(0);
      expect(row.slug?.length).toBeGreaterThan(0);
      expect(row.parentId ?? null).toBeNull();
    }
  });

  test('ít nhất một root có con: hasChildren, con trỏ đúng parentId', async ({ request }) => {
    const roots = await fetchRootCategories(request);
    const parents = roots.filter((r) => r.hasChildren === true);
    expect(parents.length).toBeGreaterThan(0);

    const parent = parents[0]!;
    const subs = await fetchCategoriesByParent(request, parent.id);
    expect(subs.length).toBeGreaterThan(0);
    for (const sub of subs) {
      expect(sub.parentId).toBe(parent.id);
      expect(sub.slug?.length).toBeGreaterThan(0);
    }
  });

  test('bản đồ đầy đủ: mỗi slug unique', async ({ request }) => {
    const all = await fetchAllCategories(request);
    const slugs = all.map((c) => c.slug).filter(Boolean) as string[];
    const set = new Set(slugs);
    expect(set.size).toBe(slugs.length);
  });

  test('nếu có root slug "electronics": con có parentId khớp', async ({ request }) => {
    const roots = await fetchRootCategories(request);
    const electronics = bySlug(roots, 'electronics');
    test.skip(!electronics?.id, 'DB không có root slug "electronics" — bỏ qua');

    const subs = await fetchCategoriesByParent(request, electronics!.id);
    expect(subs.length).toBeGreaterThan(0);
    for (const sub of subs) {
      expect(sub.parentId).toBe(electronics!.id);
    }
  });
});

test.describe('API: Item specifics theo category', () => {
  test('một danh mục có specifics: REQUIRED + name/requirement', async ({ request }) => {
    const roots = await fetchRootCategories(request);
    let specifics: ItemSpecificRow[] = [];
    const tryIds = [
      bySlug(roots, 'electronics')?.id,
      ...roots.map((r) => r.id),
    ].filter(Boolean) as string[];

    for (const id of tryIds) {
      specifics = await fetchItemSpecifics(request, id);
      if (specifics.length > 0) break;
    }
    expect(specifics.length).toBeGreaterThan(0);

    const requirements = specifics.map((s) => (s.requirement ?? '').toUpperCase());
    expect(requirements.some((r) => r.includes('REQUIRED'))).toBeTruthy();

    for (const s of specifics) {
      expect(s.name?.length).toBeGreaterThan(0);
      expect(s.requirement?.length).toBeGreaterThan(0);
    }
  });

  test('danh mục con của electronics (ưu tiên phone/cell): có specifics, thường có Brand', async ({
    request,
  }) => {
    const roots = await fetchRootCategories(request);
    const electronics = bySlug(roots, 'electronics');
    test.skip(!electronics?.id, 'Không có root electronics');

    const subs = await fetchCategoriesByParent(request, electronics!.id);
    const leaf =
      subs.find((s) => /phone|cell|mobile|smartphone/i.test(s.slug ?? '')) ?? subs[0];
    expect(leaf?.id).toBeTruthy();

    const specifics = await fetchItemSpecifics(request, leaf!.id);
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
