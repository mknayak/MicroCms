import { http, HttpResponse } from 'msw';
import type {
  AuthTokenResponse,
  PagedResult,
  ContentTypeListItem,
  EntryListItem,
  DashboardStats,
  SearchResults,
  MediaFolder,
  ComponentListItem,
  ComponentDto,
  ComponentItemDto,
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

const mockContentTypes: ContentTypeListItem[] = [
  {
    id: 'ct-1',
    handle: 'blog_post',
    displayName: 'Blog Post',
    status: 'Active',
    fieldCount: 2,
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

// ─── Components ───────────────────────────────────────────────────────────────

const mockComponents: ComponentListItem[] = [
  {
    id: 'comp-1',
    name: 'HeroBanner',
    key: 'hero-banner',
    description: 'Full-width hero with heading, subheading, CTA button and optional background image.',
    category: 'Layout',
    zones: ['hero-zone'],
    usageCount: 7,
    itemCount: 8,
    fieldCount: 6,
    templateType: 'RazorPartial',
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-15T00:00:00Z',
  },
  {
    id: 'comp-2',
    name: 'CardGrid',
    key: 'card-grid',
    description: 'Responsive grid of feature cards with icon, heading, and short description.',
    category: 'Content',
    zones: ['features-zone', 'content-zone'],
    usageCount: 9,
    itemCount: 5,
    fieldCount: 5,
    templateType: 'RazorPartial',
    createdAt: '2025-01-02T00:00:00Z',
    updatedAt: '2025-01-16T00:00:00Z',
  },
  {
    id: 'comp-3',
    name: 'CTABanner',
    key: 'cta-banner',
    description: 'Full-width call-to-action banner with heading, subtext and brand background.',
    category: 'Content',
    zones: ['cta-zone', 'hero-zone'],
    usageCount: 6,
    itemCount: 3,
    fieldCount: 6,
    templateType: 'RazorPartial',
    createdAt: '2025-01-03T00:00:00Z',
    updatedAt: '2025-01-17T00:00:00Z',
  },
];

const mockComponentDetail: ComponentDto = {
  ...mockComponents[0],
  tenantId: 'tenant-1',
  siteId: 'site-1',
  templateType: 'RazorPartial',
  templateContent: '@* HeroBanner Component *@\n@model MicroCms.Components.HeroBannerModel\n\n<section class="hero-banner"\n  style="background-image:url(@Model.BackgroundImage)">\n  <h1>@Model.Heading</h1>\n  <p>@Model.Subheading</p>\n  <a href="@Model.CtaUrl" class="btn">@Model.CtaLabel</a>\n</section>',
  fields: [
    { id: 'f-1', handle: 'heading',          label: 'Heading',           fieldType: 'ShortText', isRequired: true,  isLocalized: true,  isIndexed: false, sortOrder: 0 },
    { id: 'f-2', handle: 'subheading',        label: 'Subheading',      fieldType: 'ShortText', isRequired: false, isLocalized: true,  isIndexed: false, sortOrder: 1 },
    { id: 'f-3', handle: 'backgroundImage',   label: 'Background Image',  fieldType: 'AssetRef',  isRequired: false, isLocalized: false, isIndexed: false, sortOrder: 2 },
    { id: 'f-4', handle: 'ctaLabel',  label: 'CTA Label',         fieldType: 'ShortText', isRequired: true,  isLocalized: true,  isIndexed: false, sortOrder: 3 },
    { id: 'f-5', handle: 'ctaUrl',            label: 'CTA URL',  fieldType: 'URL',       isRequired: true,  isLocalized: false, isIndexed: false, sortOrder: 4 },
    { id: 'f-6', handle: 'overlayOpacity',    label: 'Overlay Opacity',   fieldType: 'Number',    isRequired: false, isLocalized: false, isIndexed: false, sortOrder: 5 },
  ],
};

const mockComponentItems: ComponentItemDto[] = [
  {
    id: 'ci-1',
    componentId: 'comp-1',
    componentName: 'HeroBanner',
    componentKey: 'hero-banner',
    tenantId: 'tenant-1',
    siteId: 'site-1',
    title: 'Summer Campaign Hero',
    status: 'Published',
    fieldsJson: { heading: 'Discover Your Summer Style', ctaLabel: 'Shop Now', ctaUrl: '/shop/summer' },
    usedOnPages: 3,
    createdAt: '2025-01-10T00:00:00Z',
    updatedAt: '2025-01-10T00:00:00Z',
  },
  {
    id: 'ci-2',
componentId: 'comp-1',
    componentName: 'HeroBanner',
    componentKey: 'hero-banner',
    tenantId: 'tenant-1',
    siteId: 'site-1',
    title: 'Product Launch Hero',
    status: 'Draft',
    fieldsJson: { heading: 'Introducing Pro 2.0', ctaLabel: 'Learn More', ctaUrl: '/product' },
    usedOnPages: 0,
    createdAt: '2025-01-11T00:00:00Z',
    updatedAt: '2025-01-11T00:00:00Z',
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
    HttpResponse.json<PagedResult<ContentTypeListItem>>({
      items: mockContentTypes,
      totalCount: 1,
  pageNumber: 1,
      pageSize: 100,
      totalPages: 1,
    }),
  ),
  http.get(`${BASE}/content-types/:id`, ({ params }) => {
    const ct = mockContentTypes.find((c) => c.id === params.id);
    if (!ct) return HttpResponse.json({ status: 404 }, { status: 404 });
    // Return a full ContentType shape for the detail endpoint
    return HttpResponse.json({
      id: ct.id,
      tenantId: 'tenant-1',
      siteId: 'site-1',
      handle: ct.handle,
      displayName: ct.displayName,
      status: ct.status,
      fields: [
        { id: 'f-1', handle: 'title', label: 'Title', fieldType: 'ShortText', isRequired: true, isLocalized: false, isUnique: false, sortOrder: 0 },
        { id: 'f-2', handle: 'body', label: 'Body', fieldType: 'RichText', isRequired: true, isLocalized: true, isUnique: false, sortOrder: 1 },
      ],
      createdAt: '2025-01-01T00:00:00Z',
      updatedAt: ct.updatedAt,
    });
  }),
  http.post(`${BASE}/content-types`, async ({ request }) => {
  const body = await request.json() as { handle?: string; displayName?: string; description?: string; siteId?: string };
    const newCt = {
      id: 'ct-new',
      tenantId: 'tenant-1',
      siteId: body.siteId ?? 'site-1',
      handle: body.handle ?? 'new_type',
      displayName: body.displayName ?? 'New Type',
      description: body.description,
      status: 'Draft',
      fields: [],
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

  // Components
  http.get(`${BASE}/components`, () =>
    HttpResponse.json<PagedResult<ComponentListItem>>({
      items: mockComponents,
      totalCount: mockComponents.length,
    pageNumber: 1,
      pageSize: 100,
      totalPages: 1,
    }),
  ),
  http.get(`${BASE}/components/:id`, ({ params }) => {
    if (params.id === 'comp-1') return HttpResponse.json(mockComponentDetail);
    const comp = mockComponents.find((c) => c.id === params.id);
    if (!comp) return HttpResponse.json({ status: 404 }, { status: 404 });
    return HttpResponse.json({ ...comp, tenantId: 'tenant-1', siteId: 'site-1', fields: [] } as ComponentDto);
  }),
  http.post(`${BASE}/components`, async ({ request }) => {
    const body = await request.json() as Partial<ComponentDto>;
    const newComp: ComponentDto = {
      id: `comp-${Date.now()}`,
    tenantId: 'tenant-1',
   siteId: body.siteId ?? 'site-1',
      name: body.name ?? 'New Component',
      key: body.key ?? 'new-component',
      description: body.description,
      category: body.category ?? 'Content',
      zones: body.zones ?? [],
      usageCount: 0,
      itemCount: 0,
      templateType: 'RazorPartial',
      templateContent: undefined,
  fields: [],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    return HttpResponse.json(newComp, { status: 201 });
  }),
  http.put(`${BASE}/components/:id`, async ({ request }) => {
    const body = await request.json() as Partial<ComponentDto>;
    return HttpResponse.json({ ...mockComponentDetail, ...body, updatedAt: new Date().toISOString() });
  }),
  http.put(`${BASE}/components/:id/template`, async ({ request }) => {
    const body = await request.json() as { templateType: string; templateContent?: string };
    return HttpResponse.json({
      ...mockComponentDetail,
      templateType: body.templateType,
      templateContent: body.templateContent ?? '',
      updatedAt: new Date().toISOString(),
});
  }),
  http.delete(`${BASE}/components/:id`, () => new HttpResponse(null, { status: 204 })),

  // Component Items
  http.get(`${BASE}/components/:id/items`, () =>
    HttpResponse.json<PagedResult<ComponentItemDto>>({
   items: mockComponentItems,
  totalCount: mockComponentItems.length,
    pageNumber: 1,
   pageSize: 50,
      totalPages: 1,
  }),
  ),
  http.get(`${BASE}/components/:id/items/:itemId`, ({ params }) => {
    const item = mockComponentItems.find((i) => i.id === params.itemId);
    return item ? HttpResponse.json(item) : HttpResponse.json({ status: 404 }, { status: 404 });
  }),
  http.post(`${BASE}/components/:id/items`, async ({ request, params }) => {
    const body = await request.json() as { title: string; fieldsJson: Record<string, unknown> };
    const newItem: ComponentItemDto = {
      id: `ci-${Date.now()}`,
componentId: params.id as string,
      componentName: 'HeroBanner',
      componentKey: 'hero-banner',
      tenantId: 'tenant-1',
      siteId: 'site-1',
      title: body.title,
      status: 'Draft',
      fieldsJson: body.fieldsJson,
      usedOnPages: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    return HttpResponse.json(newItem, { status: 201 });
  }),
  http.put(`${BASE}/components/:id/items/:itemId`, async ({ request, params }) => {
    const body = await request.json() as { title: string; fieldsJson: Record<string, unknown> };
    const existing = mockComponentItems.find((i) => i.id === params.itemId);
    return HttpResponse.json({ ...(existing ?? mockComponentItems[0]), ...body, updatedAt: new Date().toISOString() });
  }),
  http.post(`${BASE}/components/:id/items/:itemId/publish`, () => new HttpResponse(null, { status: 204 })),
  http.post(`${BASE}/components/:id/items/:itemId/archive`, () => new HttpResponse(null, { status: 204 })),
  http.delete(`${BASE}/components/:id/items/:itemId`, () => new HttpResponse(null, { status: 204 })),
];
