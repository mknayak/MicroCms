// ─── Pagination ──────────────────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface PaginationParams {
  pageNumber?: number;
  pageSize?: number;
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
}

/** Matches MicroCMS.Application.Features.Auth.Dtos.AuthTokenResponse */
export interface AuthTokenResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;   // ISO 8601
  refreshTokenExpiry: string;  // ISO 8601
  user: AuthUserDto;
}

/** Matches MicroCMS.Application.Features.Auth.Dtos.AuthUserDto */
export interface AuthUserDto {
  userId: string;
  email: string;
  displayName: string;
  roles: string[];
}

/** Alias kept for backwards compatibility within the SPA */
export type LoginResponse = AuthTokenResponse;

export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  tenantId: string;
  avatarUrl?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

// ─── Tenant ───────────────────────────────────────────────────────────────────

export interface Tenant {
  id: string;
  slug: string;
  /** matches TenantDto.DisplayName */
  displayName: string;
  defaultLocale: string;
  /** matches TenantDto.TimeZoneId */
  timeZoneId: string;
  aiEnabled: boolean;
  logoUrl?: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  sites: Site[];
}

export interface UpdateTenantRequest {
  displayName: string;
  defaultLocale: string;
  timeZoneId: string;
  aiEnabled: boolean;
  logoUrl?: string;
}

// ─── Content Types ────────────────────────────────────────────────────────────

/**
 * Discriminates what a ContentType represents.
 *  - Content  : standard headless content (blog post, product, etc.)
 *  - Page     : page-typed content; creation triggers the page wizard
 *  - Component: auto-created backing type for a Component; not user-visible in the type list
 */
export type ContentTypeKind = 'Content' | 'Page' | 'Component';

/**
 * Matches the backend FieldType enum exactly (case-sensitive).
 */
export type FieldType =
  | 'ShortText'
  | 'LongText'
  | 'RichText'
  | 'Markdown'
  | 'Integer'
  | 'Decimal'
  | 'Boolean'
  | 'DateTime'
  | 'Enum'
  | 'Reference'
  | 'AssetReference'
  | 'Json'
  | 'Component'
  | 'Location'
  | 'Color';

export interface FieldDefinition {
  id: string;
  name: string;
  apiKey: string;
  type: FieldType;
  required: boolean;
  localized: boolean;
  validations?: Record<string, unknown>;
  defaultValue?: unknown;
  options?: string[];
}

/**
 * Matches ContentTypeListItemDto — returned by GET /content-types (list).
 * Uses fieldCount (number) instead of a full fields array to keep payloads small.
 */
export interface ContentTypeListItem {
  id: string;
  /** Machine-readable handle, e.g. "blog_post" */
  handle: string;
  /** Human-readable display name */
  displayName: string;
  status: string;
  localizationMode: string;
  /** Count of non-archived entries for this content type. */
  entryCount: number;
  /** Count of distinct locales used by entries of this type. */
  localeCount: number;
  /** Number of fields defined on this content type */
  fieldCount: number;
  updatedAt: string;
}

/**
 * Matches ContentTypeDto — returned by GET /content-types/{id} (single).
 * Includes the full fields array.
 */
export interface ContentType {
  id: string;
  tenantId: string;
  siteId: string;
  handle: string;
  displayName: string;
  description?: string;
  localizationMode: string;
  status: string;
  /** Discriminates what this content type represents. */
  kind: ContentTypeKind;
  /** Only set when kind === 'Page'. The layout applied to pages of this type. */
  layoutId?: string;
  fields: FieldDefinitionDto[];
  createdAt: string;
  updatedAt: string;
}

/**
 * Matches FieldDefinitionDto from the backend.
 */
export interface FieldDefinitionDto {
  id: string;
  handle: string;
  label: string;
  fieldType: string;
  isRequired: boolean;
  isLocalized: boolean;
  isUnique: boolean;
  isIndexed: boolean;
  sortOrder: number;
  description?: string;
  /** Allowed values for Enum-type fields. Null/undefined for all other types. */
  options?: string[];
}

export interface CreateContentTypeRequest {
  name: string;
  apiKey: string;
  description?: string;
  isCollection: boolean;
  fields: Omit<FieldDefinition, 'id'>[];
}

export interface UpdateContentTypeRequest extends CreateContentTypeRequest {
  fields: FieldDefinition[];
}

// ─── Entries ──────────────────────────────────────────────────────────────────

/** Matches the backend EntryStatus enum exactly (case-sensitive). */
export type EntryStatus =
  | 'Draft'
  | 'PendingReview'
  | 'Approved'
  | 'Published'
  | 'Unpublished'
  | 'Archived'
  | 'Scheduled';

/** Matches EntryListItemDto — returned by GET /entries (paginated list). */
export interface EntryListItem {
  id: string;
  siteId: string;
  contentTypeId: string;
  /** Populated when the backend performs a join; may be null on simple list paths. */
  contentTypeName?: string;
  slug: string;
  /** Extracted from the "title" field in FieldsJson; null when absent. */
  title?: string;
  locale: string;
  authorId: string;
  /** Populated when the backend performs a join; may be null on simple list paths. */
  authorName?: string;
  status: EntryStatus;
  currentVersionNumber: number;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
  scheduledPublishAt?: string;
}

/** Matches EntryDto — returned by GET /entries/{id} (single entry). */
export interface Entry {
  id: string;
  tenantId: string;
  siteId: string;
  contentTypeId: string;
  slug: string;
  locale: string;
  authorId: string;
  status: EntryStatus;
  currentVersionNumber: number;
  fields: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
  scheduledPublishAt?: string;
  scheduledUnpublishAt?: string;
  folderId?: string;
  seo?: SeoMetadata;
  /** All locale codes for which a variant of this entry exists. */
  localeVariants?: string[];
}

export interface SeoMetadata {
  metaTitle?: string;
  metaDescription?: string;
  canonicalUrl?: string;
  ogImage?: string;
}

export interface CreateEntryRequest {
  siteId: string;
  contentTypeId: string;
  slug: string;
  locale: string;
  fields?: Record<string, unknown>;
}

export interface UpdateEntryRequest {
  fields?: Record<string, unknown>;
  newSlug?: string;
  changeNote?: string;
}

export interface PublishEntryRequest {
  scheduledAt?: string;
}

/** Matches EntryVersionDto — returned by GET /entries/{id}/versions. */
export interface EntryVersion {
  id: string;
  entryId: string;
  versionNumber: number;
  fields: Record<string, unknown>;
  authorId: string;
  changeNote?: string;
  createdAt: string;
}

export interface EntryListParams extends PaginationParams {
  siteId?: string;
  contentTypeId?: string;
  status?: EntryStatus;
  locale?: string;
  search?: string;
  folderId?: string;
}

// ─── Media ────────────────────────────────────────────────────────────────────

export type MediaType = 'image' | 'video' | 'audio' | 'document' | 'other';

export type MediaAssetStatus = 'Uploading' | 'PendingScan' | 'Available' | 'Quarantined' | 'Deleted';

export interface MediaAsset {
  id: string;
  fileName: string;
  contentType: string;
  mediaType: MediaType;
  status?: MediaAssetStatus;
  url: string;
  signedUrl?: string;
  thumbnailUrl?: string;
  fileSize: number;
  width?: number;
  height?: number;
  altText?: string;
  tags: string[];
  folderId?: string;
  uploadedById: string;
  uploadedByName: string;
  createdAt: string;
}

export interface UpdateMediaAssetRequest {
  altText?: string;
  tags?: string[];
  folderId?: string;
}

export interface MediaFolder {
  id: string;
  siteId: string;
  name: string;
  parentFolderId?: string;
  assetCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateMediaFolderRequest {
  siteId: string;
  name: string;
  parentFolderId?: string;
}

export interface RenameMediaFolderRequest {
  newName: string;
}

export interface MediaListParams extends PaginationParams {
  search?: string;
  mediaType?: MediaType;
  folderId?: string;
  siteId?: string;
}

// ─── Taxonomy ─────────────────────────────────────────────────────────────────

export interface Category {
  id: string;
  name: string;
  slug: string;
  parentId?: string;
  children?: Category[];
  entryCount: number;
}

export interface Tag {
  id: string;
  name: string;
  slug: string;
  entryCount: number;
}

export interface CreateCategoryRequest {
  siteId: string;
  name: string;
  slug: string;
  parentId?: string;
}

export interface CreateTagRequest {
  siteId: string;
  name: string;
  slug: string;
}

// ─── Users ────────────────────────────────────────────────────────────────────

export type UserRole = 'SystemAdmin' | 'TenantAdmin' | 'Editor' | 'Author' | 'Viewer';

export interface User {
  id: string;
  email: string;
  displayName: string;
  /** Matches UserListItemDto.Roles — array of role name strings e.g. "TenantAdmin" */
  roles: string[];
  isActive: boolean;
  avatarUrl?: string;
  lastLoginAt?: string;
  createdAt: string;
}

export interface InviteUserRequest {
  email: string;
  displayName: string;
}

export interface UpdateUserRolesRequest {
  roles: string[];
}

// ─── Dashboard ────────────────────────────────────────────────────────────────

export interface DashboardStats {
  totalEntries: number;
  publishedEntries: number;
  draftEntries: number;
  totalAssets: number;
  totalUsers: number;
  contentTypes: number;
}

export interface ActivityItem {
  id: string;
  type: string;
  description: string;
  actorName: string;
  actorAvatarUrl?: string;
  entityId: string;
  entityType: string;
  entityTitle: string;
  createdAt: string;
}

// ─── Problem Details ─────────────────────────────────────────────────────────

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

// ─── Install ──────────────────────────────────────────────────────────────────

export interface InstallStatusResponse {
  isInstalled: boolean;
  message: string;
}

export interface InstallRequest {
  tenantSlug: string;
  tenantDisplayName: string;
  defaultLocale: string;
  timeZoneId: string;
  defaultSiteName: string;
  adminEmail: string;
  adminDisplayName: string;
  adminPassword: string;
}

export interface InstallResult {
  tenantId: string;
  siteId: string;
  adminUserId: string;
  adminEmail: string;
  message: string;
}

// ─── Pages ────────────────────────────────────────────────────────────────────

export type PageType = 'Static' | 'Collection';

export interface PageTreeNode {
  id: string;
  title: string;
  slug: string;
  pageType: PageType;
  parentId?: string;
  depth: number;
  layoutId?: string;
  children: PageTreeNode[];
}

export interface PageSeoDto {
  metaTitle?: string;
  metaDescription?: string;
  canonicalUrl?: string;
  ogImage?: string;
}

export interface PageDto {
  id: string;
  siteId: string;
  title: string;
  slug: string;
  pageType: PageType;
  parentId?: string;
  linkedEntryId?: string;
  collectionContentTypeId?: string;
  routePattern?: string;
  depth: number;
  layoutId?: string;
  /** Page-level SEO metadata; null/undefined when none have been set. */
  seo?: PageSeoDto;
}

/** Nested placement node — either a component leaf or a grid-row branch. */
export interface SavePlacementNode {
  type: 'component' | 'grid-row';
  zone: string;
  sortOrder: number;
  // component-only:
  componentId?: string;
  boundItemId?: string;
  isLayoutDefault?: boolean;
  // grid-row-only:
  columns?: Array<{
    span: number;
    zoneName: string;
    placements: SavePlacementNode[];
  }>;
}

export interface SavePageTemplateRequest {
  placements: SavePlacementNode[];
}

export interface MovePageRequest {
  newParentId?: string;
}

export interface SetPageLayoutRequest {
  layoutId: string | null;
}

export interface PageTemplatePlacementDto {
  id: string;
  componentId: string;
  zone: string;
  sortOrder: number;
}

export interface PageTemplateDto {
  id: string;
  pageId: string;
  placements: PageTemplatePlacementDto[];
  updatedAt: string;
}

export interface PageTemplatePlacementInput {
  componentId: string;
  zone: string;
  sortOrder: number;
}

export interface SetPageSeoRequest {
  metaTitle: string | null;
  metaDescription: string | null;
  canonicalUrl: string | null;
  ogImage: string | null;
}

export interface SetPageLinkedEntryRequest {
  entryId: string | null;
}

export interface CreateStaticPageRequest {
  siteId: string;
  title: string;
  slug: string;
  parentId?: string;
  layoutId?: string;
}

export interface CreateCollectionPageRequest {
  siteId: string;
  title: string;
  slug: string;
  parentId?: string;
  layoutId?: string;
  collectionContentTypeId?: string;
}

// ─── Layouts ──────────────────────────────────────────────────────────────────

export type LayoutTemplateType = 'Handlebars' | 'Html';

export interface LayoutListItem {
  id: string;
  siteId: string;
  name: string;
  key: string;
  templateType: LayoutTemplateType;
  isDefault: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface LayoutColumnDef {
  span: number;      // 1–12; all columns must sum to 12
  zoneName: string;
}

/** A node in the layout's zone tree — either a simple zone or a grid row of column-zones. */
export interface LayoutZoneNode {
  id: string;
  type: 'zone' | 'grid-row';
  name: string; // machine name, used as token in shell template
  label: string;       // display label in layout designer
sortOrder: number;
  columns?: LayoutColumnDef[];
}

/** A default component placement defined on the layout, inherited by all pages. */
export interface LayoutDefaultPlacement {
  componentId: string;
  componentName: string;
  zone: string;
  sortOrder: number;
  isLocked: boolean;
}

export interface LayoutDto {
  id: string;
  tenantId: string;
  siteId: string;
  name: string;
  key: string;
  templateType: LayoutTemplateType;
  /** Auto-generated from zones[]. Not editable directly via UI. */
  shellTemplate?: string;
  isDefault: boolean;
  zones: LayoutZoneNode[];
  defaultPlacements: LayoutDefaultPlacement[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateLayoutRequest {
  siteId: string;
  name: string;
  key: string;
  templateType: LayoutTemplateType;
}

export interface UpdateLayoutRequest {
  name: string;
  templateType: LayoutTemplateType;
}

export interface UpdateLayoutZonesRequest {
  zones: LayoutZoneNode[];
}

export interface UpdateLayoutDefaultPlacementsRequest {
  placements: LayoutDefaultPlacement[];
}

// ─── Sites ────────────────────────────────────────────────────────────────────

export interface Site {
  id: string;
  name: string;
  handle: string;
  defaultLocale: string;
  isActive: boolean;
  customDomain?: string;
}

export interface CreateSiteRequest {
  name: string;
  handle: string;
  defaultLocale: string;
}

// ─── Tenant Admin (system-level) ──────────────────────────────────────────────

export interface TenantListItem {
  id: string;
  slug: string;
  displayName: string;
  status: string;
  createdAt: string;
}

export interface TenantDetail {
  id: string;
  slug: string;
  displayName: string;
  defaultLocale: string;
  timeZoneId: string;
  aiEnabled: boolean;
  logoUrl?: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  sites: Site[];
}

export interface OnboardTenantRequest {
  slug: string;
displayName: string;
  defaultLocale: string;
  timeZoneId: string;
  adminEmail: string;
  adminDisplayName: string;
  defaultSiteName?: string;
}

export interface TenantOnboardingResult {
  tenantId: string;
  siteId: string;
  adminUserId: string;
  adminEmail: string;
  message: string;
}

export interface UpdateTenantSettingsRequest {
  displayName: string;
  defaultLocale: string;
  timeZoneId: string;
  aiEnabled: boolean;
  logoUrl?: string;
}

// ─── Search ───────────────────────────────────────────────────────────────────

export interface SearchHit {
  entryId: string;
  siteId: string;
  contentTypeId: string;
  slug: string;
  locale: string;
  status: string;
  title?: string;
  excerpt?: string;
  score: number;
  publishedAt?: string;
}

export interface SearchResults {
  hits: SearchHit[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SearchParams {
query: string;
  siteId?: string;
  contentTypeId?: string;
  locale?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

// ─── Component System ─────────────────────────────────────────────────────────

export type ComponentCategory =
  | 'Layout'
  | 'Content'
  | 'Media'
  | 'Navigation'
  | 'Interactive'
  | 'Commerce';

export type ComponentFieldType =
  | 'ShortText'
  | 'LongText'
  | 'RichText'
  | 'Number'
  | 'Boolean'
  | 'DateTime'
  | 'URL'
  | 'AssetRef'
  | 'EntryRef'
  | 'JSON'
  | 'ComponentRef';

export type RenderingTemplateType =
  | 'RazorPartial'
  | 'Handlebars'
  | 'React'
  | 'WebComponent';

export interface ComponentFieldDefinition {
  id: string;
  handle: string;
  label: string;
  fieldType: ComponentFieldType;
  isRequired: boolean;
  isLocalized: boolean;
  isIndexed: boolean;
  sortOrder: number;
  description?: string;
}

export interface ComponentDto {
  id: string;
  tenantId: string;
  siteId: string;
  name: string;
  key: string;
  description?: string;
  category: ComponentCategory;
  zones: string[];
  usageCount: number;
  itemCount: number;
  templateType: RenderingTemplateType;
  templateContent?: string;
  fields: ComponentFieldDefinition[];
  createdAt: string;
  updatedAt: string;
}

export interface ComponentListItem {
  id: string;
  name: string;
  key: string;
  description?: string;
  category: ComponentCategory;
  zones: string[];
  usageCount: number;
  itemCount: number;
  fieldCount: number;
  templateType: RenderingTemplateType;
  createdAt: string;
  updatedAt: string;
}

export interface CreateComponentRequest {
  siteId: string;
  name: string;
  key: string;
  description?: string;
  category: ComponentCategory;
  zones: string[];
  fields?: Omit<ComponentFieldDefinition, 'id'>[];
}

export interface UpdateComponentRequest {
  name: string;
  description?: string;
  category: ComponentCategory;
  zones: string[];
  fields: ComponentFieldDefinition[];
}

export interface UpdateComponentTemplateRequest {
  templateType: RenderingTemplateType;
  templateContent?: string;
}

export interface ComponentItemDto {
  id: string;
  componentId: string;
  componentName: string;
  componentKey: string;
  tenantId: string;
  siteId: string;
  title: string;
  status: 'Draft' | 'Published' | 'Archived';
  fieldsJson: Record<string, unknown>;
  usedOnPages: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateComponentItemRequest {
  title: string;
  fieldsJson: Record<string, unknown>;
}

export interface UpdateComponentItemRequest {
  title: string;
  fieldsJson: Record<string, unknown>;
}

export interface ComponentListParams extends PaginationParams {
  siteId?: string;
  category?: ComponentCategory;
  search?: string;
}

export interface ComponentItemListParams extends PaginationParams {
  status?: 'Draft' | 'Published' | 'Archived';
  search?: string;
}

// ─── API Clients ──────────────────────────────────────────────────────────────

export type ApiKeyType = 'Delivery' | 'Management' | 'Preview';

export interface ApiClientDto {
  id: string;
  siteId: string;
  name: string;
  keyType: ApiKeyType;
  isActive: boolean;
  scopes: string[];
  expiresAt?: string;
  createdAt: string;
}

export interface ApiClientCreatedDto {
  client: ApiClientDto;
  /** Raw key shown exactly once — store it immediately. */
  rawKey: string;
}

export interface CreateApiClientRequest {
  siteId: string;
  name: string;
  keyType: ApiKeyType;
  scopes?: string[];
  expiresAt?: string;
}

/** Entity type being edited, for locking. */
export type LockEntityType = 'entry' | 'page-template' | 'layout';

/** Edit lock held by a user on an entity. */
export interface EditLock {
  entityId: string;
  entityType: LockEntityType;
  lockedByUserId: string;
  lockedByDisplayName: string;
  lockedAt: string;
  expiresAt: string;
}

/** Acquire an edit lock on an entity. */
export interface AcquireLockRequest {
  entityId: string;
  entityType: LockEntityType;
}

// ─── Item Picker ──────────────────────────────────────────────────────────────

export interface ItemPickerResult {
  id: string;
  title: string;
  status: 'Draft' | 'Published' | 'Archived';
  updatedAt: string;
  contentTypeId: string;
}

export interface ItemPickerParams {
  contentTypeId: string;
  search?: string;
  status?: 'Draft' | 'Published' | 'Archived';
  page?: number;
  pageSize?: number;
}

// ─── Site Templates ───────────────────────────────────────────────────────────

/** A reusable template: defines common component placements for a layout. */
export interface SiteTemplateListItem {
  id: string;
  name: string;
  description?: string;
  layoutId: string;
  layoutName: string;
  pageCount: number;
  updatedAt: string;
}

export interface SiteTemplateDto {
  id: string;
  tenantId: string;
  siteId: string;
  layoutId: string;
  layoutName?: string;
  name: string;
  description?: string;
  placementsJson: string;
  updatedAt: string;
}

export interface CreateSiteTemplateRequest {
  siteId: string;
  layoutId: string;
  name: string;
  description?: string;
}

export interface UpdateSiteTemplateRequest {
  layoutId: string;
  name: string;
  description?: string;
}

export interface SaveSiteTemplateRequest {
  placements: SavePlacementNode[];
}
