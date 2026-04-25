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

export type FieldType =
  | 'text'
  | 'richtext'
  | 'number'
  | 'boolean'
  | 'date'
  | 'media'
  | 'reference'
  | 'json'
  | 'select';

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

export interface ContentType {
  id: string;
  name: string;
  apiKey: string;
  description?: string;
  fields: FieldDefinition[];
  isCollection: boolean;
  tenantId: string;
  createdAt: string;
  updatedAt: string;
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

export type EntryStatus = 'Draft' | 'Review' | 'Scheduled' | 'Published' | 'Archived';

export interface EntryListItem {
  id: string;
  title: string;
  slug: string;
  status: EntryStatus;
  locale: string;
  contentTypeId: string;
  contentTypeName: string;
  authorId: string;
  authorName: string;
  publishedAt?: string;
  scheduledAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Entry extends EntryListItem {
  fields: Record<string, unknown>;
  localeVariants: string[];
}

export interface CreateEntryRequest {
  contentTypeId: string;
  slug: string;
  locale: string;
  fields: Record<string, unknown>;
}

export interface UpdateEntryRequest {
  slug: string;
  fields: Record<string, unknown>;
}

export interface PublishEntryRequest {
  scheduledAt?: string;
}

export interface EntryVersion {
  id: string;
  entryId: string;
  version: number;
  fields: Record<string, unknown>;
  createdBy: string;
  createdAt: string;
  changeNote?: string;
}

export interface EntryListParams extends PaginationParams {
  siteId?: string;
  contentTypeId?: string;
  status?: EntryStatus;
  locale?: string;
  search?: string;
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
  children: PageTreeNode[];
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
}

export interface CreateStaticPageRequest {
  siteId: string;
  title: string;
  slug: string;
  parentId?: string;
  linkedEntryId?: string;
}

export interface CreateCollectionPageRequest {
  siteId: string;
  title: string;
  slug: string;
  contentTypeId: string;
  routePattern: string;
  parentId?: string;
}

export interface MovePageRequest {
  newParentId?: string;
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
