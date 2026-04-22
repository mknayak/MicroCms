import { get, post, put, del } from './client';
import type { Category, Tag, CreateCategoryRequest, CreateTagRequest } from '@/types';

export const taxonomyApi = {
  getCategories: (): Promise<Category[]> =>
    get<Category[]>('/taxonomy/categories'),

  createCategory: (data: CreateCategoryRequest): Promise<Category> =>
    post<Category>('/taxonomy/categories', data),

  updateCategory: (id: string, data: CreateCategoryRequest): Promise<Category> =>
    put<Category>(`/taxonomy/categories/${id}`, data),

  deleteCategory: (id: string): Promise<void> =>
    del(`/taxonomy/categories/${id}`),

  getTags: (): Promise<Tag[]> =>
    get<Tag[]>('/taxonomy/tags'),

  createTag: (data: CreateTagRequest): Promise<Tag> =>
    post<Tag>('/taxonomy/tags', data),

  updateTag: (id: string, data: CreateTagRequest): Promise<Tag> =>
    put<Tag>(`/taxonomy/tags/${id}`, data),

  deleteTag: (id: string): Promise<void> =>
    del(`/taxonomy/tags/${id}`),
};
