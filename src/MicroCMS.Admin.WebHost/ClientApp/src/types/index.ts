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
  name: string;
  slug: string;
  subdomain: string;
  logoUrl?: string;
  timezone: string;
  defaultLocale: string;
  locales: string[];
  aiEnabled: boolean;
  plan: string;
  createdAt: string;
}

export interface UpdateTenantRequest {
  name: string;
  timezone: string;
  defaultLocale: string;
  locales: string[];
  aiEnabled: boolean;
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
  contentTypeId?: string;
  status?: EntryStatus;
  locale?: string;
  search?: string;
}

// ─── Media ────────────────────────────────────────────────────────────────────

export type MediaType = 'image' | 'video' | 'audio' | 'document' | 'other';

export interface MediaAsset {
  id: string;
  fileName: string;
  contentType: string;
  mediaType: MediaType;
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

export interface MediaListParams extends PaginationParams {
  search?: string;
  mediaType?: MediaType;
  folderId?: string;
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
  name: string;
  slug: string;
  parentId?: string;
}

export interface CreateTagRequest {
  name: string;
  slug: string;
}

// ─── Users ────────────────────────────────────────────────────────────────────

export type UserRole = 'SystemAdmin' | 'TenantAdmin' | 'Editor' | 'Author' | 'Viewer';

export interface User {
  id: string;
  email: string;
  displayName: string;
  roles: UserRole[];
  isActive: boolean;
  avatarUrl?: string;
  lastLoginAt?: string;
  createdAt: string;
}

export interface InviteUserRequest {
  email: string;
  displayName: string;
  roles: UserRole[];
}

export interface UpdateUserRolesRequest {
  roles: UserRole[];
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
