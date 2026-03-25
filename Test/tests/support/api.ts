import { type APIRequestContext, expect } from '@playwright/test';
import { API_BASE } from './config';

export type CategoryRow = {
  id: string;
  name?: string;
  slug?: string;
  parentId?: string | null;
  hasChildren?: boolean;
};

export type ItemSpecificRow = {
  id?: string;
  categoryId?: string;
  name?: string;
  requirement?: string;
  suggestedValues?: string | null;
  sortOrder?: number;
};

async function assertOk(res: Awaited<ReturnType<APIRequestContext['get']>>, bodyHint = ''): Promise<void> {
  expect(res.ok(), bodyHint || (await res.text())).toBeTruthy();
}

/** Root categories only: `?parentId=`. */
export async function fetchRootCategories(request: APIRequestContext): Promise<CategoryRow[]> {
  const res = await request.get(`${API_BASE}/api/categories?parentId=`);
  await assertOk(res);
  const data = await res.json();
  expect(Array.isArray(data)).toBeTruthy();
  return data;
}

/** Full tree (no parentId key) — backward compat API. */
export async function fetchAllCategories(request: APIRequestContext): Promise<CategoryRow[]> {
  const res = await request.get(`${API_BASE}/api/categories`);
  await assertOk(res);
  const data = await res.json();
  expect(Array.isArray(data)).toBeTruthy();
  return data;
}

/** Children of a parent GUID. */
export async function fetchCategoriesByParent(
  request: APIRequestContext,
  parentId: string,
): Promise<CategoryRow[]> {
  const res = await request.get(`${API_BASE}/api/categories?parentId=${parentId}`);
  await assertOk(res);
  const data = await res.json();
  expect(Array.isArray(data)).toBeTruthy();
  return data;
}

export async function fetchItemSpecifics(
  request: APIRequestContext,
  categoryId: string,
): Promise<ItemSpecificRow[]> {
  const res = await request.get(`${API_BASE}/api/categories/${categoryId}/item-specifics`);
  await assertOk(res);
  const data = await res.json();
  expect(Array.isArray(data)).toBeTruthy();
  return data;
}
