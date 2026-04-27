import { get } from './client';
import type { ItemPickerResult, ItemPickerParams, PagedResult } from '@/types';

export const itemsApi = {
  search: (params: ItemPickerParams): Promise<PagedResult<ItemPickerResult>> =>
    get<PagedResult<ItemPickerResult>>('/component-items/search', { params }),
};
