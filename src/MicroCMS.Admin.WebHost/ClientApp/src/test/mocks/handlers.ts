import { http, HttpResponse } from 'msw';
import type {
  AuthTokenResponse,
  PagedResult,
  ContentType,
  EntryListItem,
  DashboardStats,
  SearchResults,
  MediaFolder,
} from '@/types';

const BASE = '/api/v1';

// ─── Auth ─────────────────────────────────────────────────────────────────────

const mockAuthUser = {
  userId: 'user-1',
  email: 'admin@example.com',
  displayName: 'Test Admin',
  roles: ['TenantAdmin'],
};

const futureDate = new Date(Date.now() + 15 * 60 * 1000).toISOString();
const refreshFutureDate = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString();

const mockAuthResponse: AuthTokenResponse = {
  accessToken: 'mock.jwt.eyJzdWIiOiJ1c2VyLTEiLCJlbWFpbCI6ImFkbWluQGV4YW1wbGUuY29tIiwidGVuYW50X2lkIjoidGVuYW50LTEiLCJyb2xlIjoiVGVuYW50QWRtaW4iLCJleHAiOjk5OTk5OTk5OTl9.signature',
  refreshToken: 'mock-refresh-token',
  accessTokenExpiry: futureDate,
  refreshTokenExpiry: refreshFutureDate,
  user: mockAuthUser,
};

// ─── Content Types ────────────────────────────────────────────────────────────

const mockContentTypes: ContentType[] = [
  {
    id: 'ct-1',
    name: 'Blog Post',
    apiKey: 'blog_post',
    description: 'A blog post',
    fields: [
      { id: 'f-1', name: 'Title', apiKey: 'title', type: 'text', required: true, localized: false },
      { id: 'f-2', name: 'Body', apiKey: 'body', type: 'richtext', required: true, localized: true },
    ],
    isCollection: true,
    tenantId: 'tenant-1',
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-15T00:00:00Z',
  },
];

// ─── Entries ──────────────────────────────────────────────────────────────────

const mockEntries: EntryListItem[] = [
  {
    id: 'entry-1',
    title: 'Hello World',
    slug: 'hello-world',
    status: 'Draft',
    locale: 'en',
    contentTypeId: 'ct-1',
    contentTypeName: 'Blog Post',
    authorId: 'user-1',
    authorName: 'Test Admin',
    createdAt: '2025-01-10T00:00:00Z',
    updatedAt: '2025-01-10T00:00:00Z',
  },
];

// ─── Dashboard ────────────────────────────────────────────────────────────────

const mockStats: DashboardStats = {
  totalEntries: 42,
  publishedEntries: 30,
  draftEntries: 12,
  totalAssets: 150,
  totalUsers: 8,
  contentTypes: 5,
};

// ─── Search ───────────────────────────────────────────────────────────────────

const mockSearchResults: SearchResults = {
  hits: [
    {
      entryId: 'entry-1',
      siteId: 'site-1',
      contentTypeId: 'ct-1',
      slug: 'hello-world',
      locale: 'en',
      status: 'Published',
      title: 'Hello World',
      excerpt: 'This is a test entry excerpt.',
      score: 0.987,
      publishedAt: '2025-01-10T00:00:00Z',
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 8,
};

// ─── Media Folders ────────────────────────────────────────────────────────────

const mockMediaFolders: MediaFolder[] = [
  {
    id: 'folder-1',
    siteId: 'site-1',
    name: 'Images',
    assetCount: 4,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  },
  {
    id: 'folder-2',
    siteId: 'site-1',
    name: 'Documents',
    assetCount: 2,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  },
];

// ─── Handlers ─────────────────────────────────────────────────────────────────

export const handlers = [
  // Search
  http.get(`${BASE}/search`, ({ request }) => {
    const q = new URL(request.url).searchParams.get('query') ?? '';
    if (!q || q.trim().length < 2) {
      return HttpResponse.json<SearchResults>({ hits: [], totalCount: 0, page: 1, pageSize: 8 });
    }
    return HttpResponse.json(mockSearchResults);
  }),

  // Auth
  http.post(`${BASE}/auth/login`, async ({ request }) => {
    const body = await request.json() as { email: string; password: string };
    if (body.email === 'admin@example.com' && body.password === 'password') {
      return HttpResponse.json(mockAuthResponse);
    }
    return HttpResponse.json(
      { title: 'Unauthorized', detail: 'Invalid credentials.', status: 401 },
      { status: 401 },
    );
  }),
  http.post(`${BASE}/auth/refresh`, async ({ request }) => {
    const body = await request.json() as { refreshToken: string };
    if (body.refreshToken === 'mock-refresh-token') {
      return HttpResponse.json(mockAuthResponse);
    }
    return HttpResponse.json({ title: 'Unauthorized', status: 401 }, { status: 401 });
  }),
  http.post(`${BASE}/auth/logout`, () => new Response(null, { status: 204 })),
  http.post(`${BASE}/auth/logout-all`, () => new Response(null, { status: 204 })),

  // Dashboard
  http.get(`${BASE}/admin/dashboard/stats`, () => HttpResponse.json(mockStats)),
  http.get(`${BASE}/admin/dashboard/activity`, () =>
    HttpResponse.json<PagedResult<never>>({
      items: [],
      totalCount: 0,
      pageNumber: 1,
      pageSize: 10,
      totalPages: 0,
    }),
  ),

  // Content Types
  http.get(`${BASE}/content-types`, () =>
    HttpResponse.json<PagedResult<ContentType>>({
      items: mockContentTypes,
      totalCount: 1,
      pageNumber: 1,
      pageSize: 100,
      totalPages: 1,
    }),
  ),
  http.get(`${BASE}/content-types/:id`, ({ params }) => {
    const ct = mockContentTypes.find((c) => c.id === params.id);
    return ct ? HttpResponse.json(ct) : HttpResponse.json({ status: 404 }, { status: 404 });
  }),
  http.post(`${BASE}/content-types`, async ({ request }) => {
    const body = await request.json() as Partial<ContentType>;
    const newCt: ContentType = {
      id: 'ct-new',
      name: body.name ?? 'New Type',
      apiKey: body.apiKey ?? 'new_type',
      description: body.description,
      fields: [],
      isCollection: body.isCollection ?? true,
      tenantId: 'tenant-1',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    return HttpResponse.json(newCt, { status: 201 });
  }),
  http.delete(`${BASE}/content-types/:id`, () => new HttpResponse(null, { status: 204 })),

  // Entries
  http.get(`${BASE}/entries`, () =>
    HttpResponse.json<PagedResult<EntryListItem>>({
      items: mockEntries,
      totalCount: 1,
      pageNumber: 1,
      pageSize: 20,
      totalPages: 1,
    }),
  ),
  http.post(`${BASE}/entries/:id/publish`, () => new HttpResponse(null, { status: 204 })),
  http.delete(`${BASE}/entries/:id`, () => new HttpResponse(null, { status: 204 })),

  // Media
  http.get(`${BASE}/media`, () =>
    HttpResponse.json({ items: [], totalCount: 0, pageNumber: 1, pageSize: 24, totalPages: 0 }),
  ),

  // Users
  http.get(`${BASE}/admin/users`, () =>
    HttpResponse.json({ items: [{ id: mockAuthUser.userId, email: mockAuthUser.email, displayName: mockAuthUser.displayName, isActive: true, roles: ['TenantAdmin'], createdAt: '2025-01-01T00:00:00Z' }], totalCount: 1, pageNumber: 1, pageSize: 20, totalPages: 1 }),
  ),

  // Tenant
  http.get(`${BASE}/admin/tenants/current`, () =>
    HttpResponse.json({
      id: 'tenant-1',
      name: 'Acme Corp',
      slug: 'acme',
      subdomain: 'acme',
      timezone: 'UTC',
      defaultLocale: 'en',
      locales: ['en', 'de'],
      aiEnabled: false,
      plan: 'pro',
      createdAt: '2025-01-01T00:00:00Z',
    }),
  ),

  // Media Folders
  http.get(`${BASE}/media/folders`, () => HttpResponse.json(mockMediaFolders)),
  http.post(`${BASE}/media/folders`, async ({ request }) => {
    const body = await request.json() as { name: string; siteId: string };
    const newFolder: MediaFolder = {
      id: `folder-${Date.now()}`,
      siteId: body.siteId,
      name: body.name,
      assetCount: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    return HttpResponse.json(newFolder, { status: 201 });
  }),
  http.patch(`${BASE}/media/folders/:id/rename`, async ({ request }) => {
    const body = await request.json() as { newName: string };
    return HttpResponse.json({ ...mockMediaFolders[0], name: body.newName });
  }),
  http.delete(`${BASE}/media/folders/:id`, () => new HttpResponse(null, { status: 204 })),
];
