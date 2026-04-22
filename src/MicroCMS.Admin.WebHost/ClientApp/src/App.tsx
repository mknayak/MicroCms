import { Suspense, lazy } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from '@/contexts/AuthContext';
import { AppShell } from '@/components/layout/AppShell';
import { ProtectedRoute } from '@/routes/ProtectedRoute';
import { ErrorBoundary, PageLoader } from '@/components/ui/ErrorBoundary';
import LoginPage from '@/pages/auth/LoginPage';

// ─── Lazy-loaded pages ────────────────────────────────────────────────────────

const DashboardPage = lazy(() => import('@/pages/dashboard/DashboardPage'));
const ContentTypesPage = lazy(() => import('@/pages/content-types/ContentTypesPage'));
const ContentTypeEditPage = lazy(() => import('@/pages/content-types/ContentTypeEditPage'));
const EntriesPage = lazy(() => import('@/pages/entries/EntriesPage'));
const EntryEditorPage = lazy(() => import('@/pages/entries/EntryEditorPage'));
const MediaPage = lazy(() => import('@/pages/media/MediaPage'));
const TaxonomyPage = lazy(() => import('@/pages/taxonomy/TaxonomyPage'));
const UsersPage = lazy(() => import('@/pages/users/UsersPage'));
const SettingsPage = lazy(() => import('@/pages/settings/SettingsPage'));

// ─── App ──────────────────────────────────────────────────────────────────────

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ErrorBoundary>
          <Routes>
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
            </Route>

            {/* Fallback */}
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </ErrorBoundary>
      </AuthProvider>
    </BrowserRouter>
  );
}
