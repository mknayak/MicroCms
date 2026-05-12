/**
 * Imports Bootstrap CSS as a raw string at build time (Vite ?raw query).
 * Inject this into iframe <style> tags instead of loading from CDN so the
 * page's Content-Security-Policy is never violated.
 */
// @ts-expect-error — Vite ?raw import; no type declaration needed
import bootstrapCssRaw from 'bootstrap/dist/css/bootstrap.min.css?raw';

export const bootstrapCss: string = bootstrapCssRaw as string;
