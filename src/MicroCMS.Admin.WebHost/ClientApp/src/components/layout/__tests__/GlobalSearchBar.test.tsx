import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { render } from '@/test/utils';
import { server } from '@/test/mocks/server';
import { GlobalSearchBar } from '../GlobalSearchBar';
import type { SearchResults } from '@/types';

// ─── Helpers ──────────────────────────────────────────────────────────────────

const BASE = '/api/v1';

const mockResults: SearchResults = {
  hits: [
    {
      entryId: 'entry-1',
      siteId: 'site-1',
      contentTypeId: 'ct-1',
      slug: 'hello-world',
      locale: 'en',
      status: 'Published',
      title: 'Hello World',
      excerpt: 'A great post about hello.',
      score: 0.95,
    publishedAt: '2025-01-10T00:00:00Z',
    },
    {
      entryId: 'entry-2',
      siteId: 'site-1',
      contentTypeId: 'ct-1',
      slug: 'second-post',
      locale: 'en',
      status: 'Draft',
      title: 'Second Post',
      score: 0.7,
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 8,
};

function renderBar() {
  const user = userEvent.setup();
  render(<GlobalSearchBar />);
  const input = screen.getByRole('combobox', { name: /global search/i });
  return { user, input };
}

// ─── Tests ────────────────────────────────────────────────────────────────────

describe('GlobalSearchBar', () => {
  it('renders the search input', () => {
    const { input } = renderBar();
    expect(input).toBeInTheDocument();
    expect(input).toHaveAttribute('placeholder');
  });

  it('does not show popover for short query (< 2 chars)', async () => {
    const { user, input } = renderBar();
    await user.type(input, 'h');
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('shows results after typing ≥ 2 characters', async () => {
    server.use(
      http.get(`${BASE}/search`, () => HttpResponse.json(mockResults)),
    );

    const { user, input } = renderBar();
    await user.type(input, 'he');

    await waitFor(() => {
      expect(screen.getByRole('listbox')).toBeInTheDocument();
  });

    expect(await screen.findByText('Hello World')).toBeInTheDocument();
    expect(await screen.findByText('Second Post')).toBeInTheDocument();
  });

  it('shows "No results" message when API returns empty hits', async () => {
    server.use(
   http.get(`${BASE}/search`, () =>
        HttpResponse.json<SearchResults>({ hits: [], totalCount: 0, page: 1, pageSize: 8 }),
      ),
    );

    const { user, input } = renderBar();
    await user.type(input, 'xyz');

 await waitFor(() => {
      expect(screen.getByRole('listbox')).toBeInTheDocument();
    });

    expect(screen.getByText(/no results/i)).toBeInTheDocument();
  });

  it('closes popover when Escape is pressed', async () => {
    server.use(
      http.get(`${BASE}/search`, () => HttpResponse.json(mockResults)),
    );

    const { user, input } = renderBar();
    await user.type(input, 'he');

    await waitFor(() => {
      expect(screen.getByRole('listbox')).toBeInTheDocument();
    });

    await user.keyboard('{Escape}');
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('clears input when the clear button is clicked', async () => {
    const { user, input } = renderBar();
  await user.type(input, 'hello');
    expect(input).toHaveValue('hello');

    const clearBtn = screen.getByRole('button', { name: /clear search/i });
    await user.click(clearBtn);

    expect(input).toHaveValue('');
  });

  it('shows Published badge correctly', async () => {
    server.use(
      http.get(`${BASE}/search`, () => HttpResponse.json(mockResults)),
    );

    const { user, input } = renderBar();
    await user.type(input, 'he');

    await waitFor(() => {
      expect(screen.getByRole('listbox')).toBeInTheDocument();
    });

    expect(screen.getByText('Published')).toBeInTheDocument();
    expect(screen.getByText('Draft')).toBeInTheDocument();
  });

  it('shows "See all results" footer when there are hits', async () => {
    server.use(
 http.get(`${BASE}/search`, () => HttpResponse.json(mockResults)),
 );

    const { user, input } = renderBar();
    await user.type(input, 'he');

    await waitFor(() => {
      expect(screen.getByText(/see all results/i)).toBeInTheDocument();
  });
  });

  it('navigates using ArrowDown / ArrowUp keys', async () => {
    server.use(
      http.get(`${BASE}/search`, () => HttpResponse.json(mockResults)),
    );

    const { user, input } = renderBar();
    await user.type(input, 'he');

    await waitFor(() => {
      expect(screen.getByRole('listbox')).toBeInTheDocument();
    });

    await user.keyboard('{ArrowDown}');
    const firstOption = screen.getByRole('option', { name: /hello world/i });
  expect(firstOption).toHaveAttribute('aria-selected', 'true');

    await user.keyboard('{ArrowDown}');
    const secondOption = screen.getByRole('option', { name: /second post/i });
    expect(secondOption).toHaveAttribute('aria-selected', 'true');

    await user.keyboard('{ArrowUp}');
    expect(firstOption).toHaveAttribute('aria-selected', 'true');
  });
});
