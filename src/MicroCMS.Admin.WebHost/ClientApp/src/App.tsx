import { Suspense, lazy } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from '@/contexts/AuthContext';
import { InstallProvider } from '@/contexts/InstallContext';
import { SiteProvider } from '@/contexts/SiteContext';
import { AppShell } from '@/components/layout/AppShell';
import { ProtectedRoute } from '@/routes/ProtectedRoute';
import { InstallGate } from '@/routes/InstallGate';
import { ErrorBoundary, PageLoader } from '@/components/ui/ErrorBoundary';
import LoginPage from '@/pages/auth/LoginPage';
import InstallPage from '@/pages/install/InstallPage';

// ─── Lazy-loaded pages ────────────────────────────────────────────────────────

const DashboardPage = lazy(() => import('@/pages/dashboard/DashboardPage'));
const ContentTypesPage = lazy(() => import('@/pages/content-types/ContentTypesPage'));
const ContentTypeEditPage = lazy(() => import('@/pages/content-types/ContentTypeEditPage'));
const ContentTypeDetailPage = lazy(() => import('@/pages/content-types/ContentTypeDetailPage'));
const EntriesPage = lazy(() => import('@/pages/entries/EntriesPage'));
const EntryEditorPage = lazy(() => import('@/pages/entries/EntryEditorPage'));
const MediaPage = lazy(() => import('@/pages/media/MediaPage'));
const TaxonomyPage = lazy(() => import('@/pages/taxonomy/TaxonomyPage'));
const UsersPage = lazy(() => import('@/pages/users/UsersPage'));
const SettingsPage = lazy(() => import('@/pages/settings/SettingsPage'));
const PagesPage = lazy(() => import('@/pages/pages/PagesPage'));
const TenantsPage = lazy(() => import('@/pages/tenants/TenantsPage'));
const TenantDetailPage = lazy(() => import('@/pages/tenants/TenantDetailPage'));
const SearchResultsPage = lazy(() => import('@/pages/search/SearchResultsPage'));
const ComponentLibraryPage = lazy(() => import('@/pages/components/ComponentLibraryPage'));
const ComponentEditorPage = lazy(() => import('@/pages/components/ComponentEditorPage'));
const ComponentItemListPage = lazy(() => import('@/pages/components/ComponentItemListPage'));
const ComponentItemEditorPage = lazy(() => import('@/pages/components/ComponentItemEditorPage'));
const LayoutsPage = lazy(() => import('@/pages/layouts/LayoutsPage'));
const PageDesignerPage = lazy(() => import('@/pages/designer/PageDesignerPage'));
const LayoutDesignerPage = lazy(() => import('@/pages/layouts/LayoutDesignerPage'));
const PageTemplatesPage = lazy(() => import('@/pages/page-templates/PageTemplatesPage'));
const PageTemplateDesignerPage = lazy(() => import('@/pages/page-templates/PageTemplateDesignerPage'));

// ─── App ──────────────────────────────────────────────────────────────────────

export default function App() {
    return (
        <BrowserRouter>
            <InstallProvider>
                <AuthProvider>
                    <SiteProvider>
                        <ErrorBoundary>
                            <InstallGate>
                                <Routes>
                                    {/* Install — anonymous, only reachable when not yet installed */}
                                    <Route path="/install" element={<InstallPage />} />

                                    {/* Public */}
                                    <Route path="/login" element={<LoginPage />} />

                                    {/* Protected */}
                                    <Route
                                        element={
                                            <ProtectedRoute>
                                                <AppShell />
                                            </ProtectedRoute>
                                        }
                                    >
                                        <Route
                                            path="/"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <DashboardPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/content-types"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <ContentTypesPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/content-types/new"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <ContentTypeEditPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/content-types/:id/edit"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <ContentTypeEditPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/content-types/:id"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <ContentTypeDetailPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/entries"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <EntriesPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/entries/new"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <EntryEditorPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/entries/:id/edit"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <EntryEditorPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/media"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <MediaPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/taxonomy"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <TaxonomyPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/users"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin', 'TenantAdmin']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <UsersPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/settings"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin', 'TenantAdmin']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <SettingsPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/pages"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin', 'TenantAdmin', 'Editor']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <PagesPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/layouts"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin', 'TenantAdmin', 'Editor']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <LayoutsPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/designer"
                                            element={<Navigate to="/pages" replace />}
                                        />
                                        <Route
                                            path="/pages/:pageId/designer"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin', 'TenantAdmin', 'SiteAdmin', 'ContentAdmin', 'Designer', 'Editor']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <PageDesignerPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/tenants"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <TenantsPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/tenants/:id"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <TenantDetailPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/search"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <SearchResultsPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/components"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <ComponentLibraryPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/components/:id/edit"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <ComponentEditorPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/components/:id/items"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <ComponentItemListPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/components/:id/items/:itemId"
                                            element={
                                                <Suspense fallback={<PageLoader />}>
                                                    <ComponentItemEditorPage />
                                                </Suspense>
                                            }
                                        />
                                        <Route
                                            path="/layouts/:id/designer"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin', 'TenantAdmin', 'SiteAdmin', 'ContentAdmin', 'Designer', 'Editor']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <LayoutDesignerPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/page-templates"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin', 'TenantAdmin', 'SiteAdmin', 'ContentAdmin', 'Designer', 'Editor']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <PageTemplatesPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/page-templates/new"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin', 'TenantAdmin', 'Designer']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <PageTemplateDesignerPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/page-templates/:id/edit"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin', 'TenantAdmin', 'Designer']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <PageTemplateDesignerPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                        <Route
                                            path="/page-templates/:id/designer"
                                            element={
                                                <ProtectedRoute requiredRoles={['SystemAdmin', 'TenantAdmin', 'SiteAdmin', 'ContentAdmin', 'Designer', 'Editor']}>
                                                    <Suspense fallback={<PageLoader />}>
                                                        <PageTemplateDesignerPage />
                                                    </Suspense>
                                                </ProtectedRoute>
                                            }
                                        />
                                    </Route>

                                    {/* Fallback */}
                                    <Route path="*" element={<Navigate to="/" replace />} />
                                </Routes>
                            </InstallGate>
                        </ErrorBoundary>
                    </SiteProvider>
                </AuthProvider>
            </InstallProvider>
        </BrowserRouter>
    );
}
