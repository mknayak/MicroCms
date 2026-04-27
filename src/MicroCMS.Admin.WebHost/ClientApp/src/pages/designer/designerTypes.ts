import type { ComponentCategory } from '@/types';

export type ViewportSize = 'desktop' | 'tablet' | 'mobile';

export interface DesignerPlacement {
  /** Local UUID for React key + DnD identification */
  localId: string;
  componentId: string;
  componentName: string;
  componentKey: string;
  componentCategory: ComponentCategory;
  zone: string;
  sortOrder: number;
}
