import { apiClient } from './client';
import type {
  ExportOptions,
  PackageAnalysisResult,
  ImportOptions,
  ImportProgress,
} from '@/types';

export const packagesApi = {
  /**
   * Exports a site package and returns the raw ZIP blob for download.
   */
  export: async (options: ExportOptions): Promise<void> => {
    const response = await apiClient.post<Blob>('/packages/export', options, {
      responseType: 'blob',
    });
    const url = URL.createObjectURL(response.data);
    const ts = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
    const a = document.createElement('a');
 a.href = url;
    a.download = `mcms-export-${options.siteId}-${ts}.zip`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  },

/**
   * Uploads a package ZIP and returns analysis stats (no changes applied).
   */
  analyse: async (
    file: File,
    targetTenantId: string,
    targetSiteId: string,
  ): Promise<PackageAnalysisResult> => {
    const form = new FormData();
  form.append('file', file);
    const response = await apiClient.post<PackageAnalysisResult>(
      `/packages/analyse?targetTenantId=${targetTenantId}&targetSiteId=${targetSiteId}`,
   form,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );
    return response.data;
  },

  /**
   * Uploads a package ZIP and applies it to the target site.
   */
  import: async (
    file: File,
    targetTenantId: string,
    targetSiteId: string,
    options: ImportOptions,
  ): Promise<ImportProgress> => {
    const form = new FormData();
    form.append('file', file);
    form.append('importContentTypes', String(options.importContentTypes));
    form.append('importEntries', String(options.importEntries));
    form.append('importPages', String(options.importPages));
    form.append('importLayouts', String(options.importLayouts));
    form.append('importMediaMetadata', String(options.importMediaMetadata));
  form.append('importComponents', String(options.importComponents));
    form.append('importUsers', String(options.importUsers));
    form.append('importSiteSettings', String(options.importSiteSettings));
    form.append('conflictResolution', options.conflictResolution);
    const response = await apiClient.post<ImportProgress>(
      `/packages/import?targetTenantId=${targetTenantId}&targetSiteId=${targetSiteId}`,
      form,
  { headers: { 'Content-Type': 'multipart/form-data' } },
    );
    return response.data;
  },
};
