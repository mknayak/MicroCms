import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { render } from '@/test/utils';
import ContentTypesPage from '@/pages/content-types/ContentTypesPage';

describe('ContentTypesPage', () => {
  it('renders the page heading', async () => {
    render(<ContentTypesPage />);
    expect(screen.getByRole('heading', { name: /content types/i })).toBeInTheDocument();
  });

  it('shows content types from API', async () => {
    render(<ContentTypesPage />);
    await waitFor(() => {
      expect(screen.getByText('Blog Post')).toBeInTheDocument();
      expect(screen.getByText('blog_post')).toBeInTheDocument();
    });
  });

  it('shows Collection badge for collection types', async () => {
    render(<ContentTypesPage />);
    await waitFor(() => {
      expect(screen.getByText('Collection')).toBeInTheDocument();
    });
  });

  it('has a link to create a new content type', () => {
    render(<ContentTypesPage />);
    expect(screen.getByRole('link', { name: /new content type/i })).toHaveAttribute('href', '/content-types/new');
  });
});
